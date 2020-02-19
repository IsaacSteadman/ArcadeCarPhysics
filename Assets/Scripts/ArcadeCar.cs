using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;

public class ArcadeCar : MonoBehaviour
{
    bool enableFinishFlag = false;
    bool FINISH_LINE_FLAG = false;
    const int WHEEL_LEFT_INDEX = 0;
    const int WHEEL_RIGHT_INDEX = 1;

    const float wheelWidth = 0.085f;

    int gKey = 0;
    int hKey = 0;
    int resolutionMultiple = 80;
    int q_len = 1; int q_len_prev = 1;
    int fps = 30;
    int fps_var = 0;
    public String top5ScoresString;
    public bool forward_pressed = false;
    public bool reverse_pressed = false;
    private Queue<DataInputContainer> input_queue = new Queue<DataInputContainer>();
    public bool reset_pressed = false;
    public bool start_pressed = false;

    int prevLap = -1;
    StreamWriter writer;



    public class WheelData
    {
        // is wheel touched ground or not ?
        [HideInInspector]
        public bool isOnGround = false;

        // wheel ground touch point
        [HideInInspector]
        public RaycastHit touchPoint = new RaycastHit();

        // real yaw, after Ackermann steering correction
        [HideInInspector]
        public float yawRad = 0.0f;

        // visual rotation
        [HideInInspector]
        public float visualRotationRad = 0.0f;

        // suspension compression
        [HideInInspector]
        public float compression = 0.0f;

        // suspension compression on previous update
        [HideInInspector]
        public float compressionPrev = 0.0f;

        [HideInInspector]
        public string debugText = "-";
    }


    [Serializable]
    public class Axle
    {
        [Header("Debug settings")]

        [Tooltip("Debug name of axle")]
        public string debugName;

        [Tooltip("Debug color for axle")]
        public Color debugColor = Color.white;

        [Header("Axle settings")]

        [Tooltip("Axle width")]
        public float width = 0.4f;

        [Tooltip("Axle offset")]
        public Vector2 offset = Vector2.zero;

        [Tooltip("Current steering angle (in degrees)")]
        public float steerAngle = 0.0f;

        [Header("Wheel settings")]

        [Tooltip("Wheel radius in meters")]
        public float radius = 0.3f;

        [Range(0.0f, 1.0f)]
        [Tooltip("Tire laterial friction normalized to 0..1")]
        public float laterialFriction = 0.1f;

        [Range(0.0f, 1.0f)]
        [Tooltip("Rolling friction, normalized to 0..1")]
        public float rollingFriction = 0.01f;

        [HideInInspector]
        [Tooltip("Brake left")]
        public bool brakeLeft = false;

        [HideInInspector]
        [Tooltip("Brake right")]
        public bool brakeRight = false;

        [HideInInspector]
        [Tooltip("Hand brake left")]
        public bool handBrakeLeft = false;

        [HideInInspector]
        [Tooltip("Hand brake right")]
        public bool handBrakeRight = false;


        [Tooltip("Brake force magnitude")]
        public float brakeForceMag = 4.0f;

        [Header("Suspension settings")]

        [Tooltip("Suspension Stiffness (Suspension 'Power'")]
        public float stiffness = 8500.0f;

        [Tooltip("Suspension Damping (Suspension 'Bounce')")]
        public float damping = 3000.0f;

        [Tooltip("Suspension Restitution (Not used now)")]
        public float restitution = 1.0f;


        [Tooltip("Relaxed suspension length")]
        public float lengthRelaxed = 0.55f;

        [Tooltip("Stabilizer bar anti-roll force")]
        public float antiRollForce = 10000.0f;

        [HideInInspector]
        public WheelData wheelDataL = new WheelData();

        [HideInInspector]
        public WheelData wheelDataR = new WheelData();

        [Header("Visual settings")]

        [Tooltip("Visual scale for wheels")]
        public float visualScale = 0.03270531f;

        [Tooltip("Wheel actor left")]
        public GameObject wheelVisualLeft;

        [Tooltip("Wheel actor right")]
        public GameObject wheelVisualRight;

        [Tooltip("Is axle powered by engine")]
        public bool isPowered = false;


        [Tooltip("After flight slippery coefficent (0 - no friction)")]
        public float afterFlightSlipperyK = 0.02f;

        [Tooltip("Brake slippery coefficent (0 - no friction)")]
        public float brakeSlipperyK = 0.5f;

        [Tooltip("Hand brake slippery coefficent (0 - no friction)")]
        public float handBrakeSlipperyK = 0.01f;

    }

    private System.Random rand = new System.Random();
    private double NV_MAGICCONST = 4 * Math.Exp(-0.5) / Math.Sqrt(2.0);
    public double normalRandom(double mu, double sigma)
    {
        double u1, u2, z;
        while (true)
        {
            u1 = rand.NextDouble();
            u2 = 1.0 - rand.NextDouble();
            z = NV_MAGICCONST * (u1 - 0.5) / u2;
            double zz = z * z / 4.0;
            if (zz <= -Math.Log(u2))
            {
                break;
            }
        }
        return mu + z * sigma;
    }


    public Vector3 centerOfMass = Vector3.zero;

    [Header("Engine")]

    //x - time in seconds
    //y - speed in km/h
    [Tooltip("Y - Desired vehicle speed (km/h). X - Time (seconds)")]
    public AnimationCurve accelerationCurve = AnimationCurve.Linear(0.0f, 0.0f, 5.0f, 100.0f);

    [Tooltip("Y - Desired vehicle speed (km/h). X - Time (seconds)")]
    public AnimationCurve accelerationCurveReverse = AnimationCurve.Linear(0.0f, 0.0f, 5.0f, 20.0f);


    [Header("Steering")]
    // x - speed in km/h
    // y - angle in degrees
    [Tooltip("Y - Steereing angle limit (deg). X - Vehicle speed (km/h)")]
    public AnimationCurve steerAngleLimit = AnimationCurve.Linear(0.0f, 35.0f, 100.0f, 5.0f);

    // x - speed in km/h
    // y - angle in degrees (speed of returning wheels to zero position)
    [Tooltip("Y - Steereing reset speed (deg/sec). X - Vehicle speed (km/h)")]
    public AnimationCurve steeringResetSpeed = AnimationCurve.EaseInOut(0.0f, 30.0f, 100.0f, 10.0f);

    // x - speed in km/h
    // y - angle in degrees
    [Tooltip("Y - Steereing speed (deg/sec). X - Vehicle speed (km/h)")]
    public AnimationCurve steeringSpeed = AnimationCurve.Linear(0.0f, 2.0f, 100.0f, 0.5f);

    [Header("Debug")]

    public bool debugDraw = true;

    [Header("Other")]

    [Tooltip("Stabilization in flight (torque)")]
    public float flightStabilizationForce = 8.0f;

    [Tooltip("Stabilization in flight (Ang velocity damping)")]
    public float flightStabilizationDamping = 0.0f;

    [Tooltip("Hand brake slippery time in seconds")]
    public float handBrakeSlipperyTime = 2.2f;

    public bool controllable = true;

    // x - speed in km/h
    // y - Downforce percentage
    [Tooltip("Y - Downforce (percentage 0%..100%). X - Vehicle speed (km/h)")]
    public AnimationCurve downForceCurve = AnimationCurve.Linear(0.0f, 0.0f, 200.0f, 100.0f);

    [Tooltip("Downforce")]
    public float downForce = 5.0f;

    [Header("Axles")]

    public Axle[] axles = new Axle[2];

    //////////////////////////////////////////////////////////////////////////////////////////////////////

    float afterFlightSlipperyTiresTime = 0.0f;
    float brakeSlipperyTiresTime = 0.0f;
    float handBrakeSlipperyTiresTime = 0.0f;
    bool isBrake = false;
    bool isHandBrake = false;
    bool isAcceleration = false;
    bool isReverseAcceleration = false;
    float accelerationForceMagnitude = 0.0f;
    Rigidbody rb = null;

    //================code===================================================================
    bool startGame = false;
    bool startRace = false;
    bool controlsDisabled = false;
    public float time;
    List<float> scoreBoard = new List<float>();


    List<float> FPSList = new List<float>();
    List<float> LatencyList = new List<float>();
    List<float> StabilityList = new List<float>();
    List<string> ResolutionList = new List<string>();
    List<string> TimeList = new List<string>();
    public string resultText = "";
    //======================================================================================

    int lapCount = 0;
    int[] randomizedLapArray;// = { 0, 1, 2, 3, 4, 5, 6 };

    int[] Randomize(int[] arr)
    {
        System.Random rand = new System.Random();

        // For each spot in the array, pick
        // a random item to swap into that spot.
        for (int i = 0; i < arr.Length - 1; i++)
        {
            int j = rand.Next(i, arr.Length);
            int temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
        return arr;
    }

    void changeLapVariables()
    {
        if (lapCount < randomizedLapArray.Length)
        {
            switch (randomizedLapArray[lapCount])
            {
                //fps
                case 0:
                    fps = 15; resolutionMultiple = 80; q_len = 1; fps_var = 0;
                    break;
                case 1:
                    fps = 20; resolutionMultiple = 80; q_len = 1; fps_var = 0;
                    break;
                case 2:
                    fps = 25; resolutionMultiple = 80; q_len = 1; fps_var = 0;
                    break;
                case 3:
                    fps = 30; resolutionMultiple = 80; q_len = 1; fps_var = 0;
                    break;
                case 4:
                    fps = 45; resolutionMultiple = 80; q_len = 1; fps_var = 0;
                    break;
                case 5:
                    fps = 60; resolutionMultiple = 80; q_len = 1; fps_var = 0;
                    break;

                //resolution
                case 6:
                    fps = 30; resolutionMultiple = 20; q_len = 1; fps_var = 0;
                    break;
                case 7:
                    fps = 30; resolutionMultiple = 30; q_len = 1; fps_var = 0;
                    break;
                case 8:
                    fps = 30; resolutionMultiple = 40; q_len = 1; fps_var = 0;
                    break;
                case 9:
                    fps = 30; resolutionMultiple = 60; q_len = 1; fps_var = 0;
                    break;
                case 10:
                    fps = 30; resolutionMultiple = 80; q_len = 1; fps_var = 0; // 1280x720
                    break;
                case 11:
                    fps = 30; resolutionMultiple = 120; q_len = 1; fps_var = 0; // 1920x1080
                    break;

                //latency 
                case 12:
                    fps = 30; resolutionMultiple = 120; q_len = 1; fps_var = 0;
                    break;
                case 13:
                    fps = 30; resolutionMultiple = 120; q_len = 3; fps_var = 0;
                    break;
                case 14:
                    fps = 30; resolutionMultiple = 120; q_len = 5; fps_var = 0;
                    break;
                case 15:
                    fps = 30; resolutionMultiple = 120; q_len = 7; fps_var = 0;
                    break;
                case 16:
                    fps = 30; resolutionMultiple = 120; q_len = 11; fps_var = 0;
                    break;
                case 17:
                    fps = 30; resolutionMultiple = 120; q_len = 15; fps_var = 0;
                    break;

                //stability
                case 18:
                    fps = 30; resolutionMultiple = 120; q_len = 1; fps_var = 0;
                    break;
                case 19:
                    fps = 30; resolutionMultiple = 120; q_len = 1; fps_var = 3;
                    break;
                case 20:
                    fps = 30; resolutionMultiple = 120; q_len = 1; fps_var = 7;
                    break;
                case 21:
                    fps = 30; resolutionMultiple = 120; q_len = 1; fps_var = 10;
                    break;
                case 22:
                    fps = 30; resolutionMultiple = 120; q_len = 1; fps_var = 15;
                    break;
                case 23:
                    fps = 30; resolutionMultiple = 120; q_len = 1; fps_var = 20;
                    break;


                default:
                    fps = 30; resolutionMultiple = 120; q_len = 1; fps_var = 0;
                    break;
            }
            if (lapCount != prevLap)
            {
                prevLap = lapCount;
                initLog();
                if (writer != null)
                {
                    writer.WriteLine(string.Format("Reset (LAP {4}) fps = {0}; resolutionMultiple = {1}, q_len = {2}, fps_var = {3}", fps, resolutionMultiple, q_len, fps_var, lapCount));
                    writer.Flush();
                }
                else
                {
                    Debug.Log("Could not writer to the writer");
                }
            }
        }

        Application.targetFrameRate = fps;
        Screen.SetResolution(16 * resolutionMultiple, 9 * resolutionMultiple, true);

        string resString = "";
        resString += (16 * resolutionMultiple).ToString() + " x " + (9 * resolutionMultiple).ToString();
        if (lapCount == ResolutionList.Count) ResolutionList.Add(resString);
        if (lapCount == FPSList.Count) FPSList.Add(Application.targetFrameRate);
        if (lapCount == LatencyList.Count) LatencyList.Add(q_len);
        if (lapCount == StabilityList.Count) StabilityList.Add(fps_var);


    }

    void initLog()
    {
        if (writer == null)
        {
            try
            {
                String settingsLogPath = Application.persistentDataPath + "/settingsLog.txt";
                Debug.Log("logging path: " + settingsLogPath);
                writer = new StreamWriter(settingsLogPath, true);
                writer.WriteLine("".PadLeft(80, '='));
                writer.WriteLine("Starting a new Log");
                writer.Flush();
            }
            catch (Exception exc)
            {
                Debug.Log(exc.StackTrace);
            }
        }
    }
    //void changeFPS()
    //{
    //    if (lapCount <= 6)
    //        switch (randomizedLapArray[lapCount])
    //        {
    //            //control lap
    //            case 0:
    //                fps = 10;// 30;
    //                break;
    //            case 1:
    //                fps = 10;// 15;
    //                break;
    //            case 2:
    //                fps = 10;//20;
    //                break;
    //            case 3:
    //                fps = 10;//24;
    //                break;
    //            case 4:
    //                fps = 10;//40;
    //                break;
    //            case 5:
    //                fps = 60;//50;
    //                break;
    //            case 6:
    //                fps = 60;//
    //                break;
    //            default:
    //                fps = 60;
    //                break;
    //        }
    //    Application.targetFrameRate = fps;
    // if (lapCount == FPSList.Count) FPSList.Add(Application.targetFrameRate);
    //}
    //void changeResolution()
    //{
    //    if (lapCount <= 6)
    //    {
    //        switch (randomizedLapArray[lapCount])
    //        {
    //            case 0:
    //                resolutionMultiple = 12;// 12; // 256x144
    //                break;
    //            case 1:
    //                resolutionMultiple = 12;// 40; // 640x360
    //                break;
    //            case 2:
    //                resolutionMultiple = 12;// 80; // 1280x720
    //                break;
    //            case 3:
    //                resolutionMultiple = 12;// 120; // 1920x1080
    //                break;
    //            case 4:
    //                resolutionMultiple = 12;// 12;
    //                break;
    //            case 5:
    //                resolutionMultiple = 12;// 40;
    //                break;
    //            case 6:
    //                resolutionMultiple = 12;// 80;
    //                break;
    //            default:
    //                resolutionMultiple = 120; // 1920x1080
    //                break;
    //        }
    //    }
    //string resString = "";
    //resString += (16 * resolutionMultiple).ToString() + " x " + (9 * resolutionMultiple).ToString();
    //    if (lapCount == ResolutionList.Count) ResolutionList.Add(resString);
    //    Screen.SetResolution(16 * resolutionMultiple, 9 * resolutionMultiple, true);
    //}

    //void changeLatency()
    //{
    //    if (lapCount <= 6)
    //        switch (randomizedLapArray[lapCount])
    //        {
    //            //control lap
    //            case 0:
    //                q_len = 1;
    //                break;
    //            case 1:
    //                q_len = 2;
    //                break;
    //            case 2:
    //                q_len = 4;
    //                break;
    //            case 3:
    //                q_len = 6;
    //                break;
    //            case 4:
    //                q_len = 8;
    //                break;
    //            case 5:
    //                q_len = 13;
    //                break;
    //            case 6:
    //                q_len = 30;
    //                break;
    //            default:
    //                q_len = 1;
    //                break;
    //        }
    //}

    //void changeStability()
    //{
    //    if (lapCount <= 6)
    //        switch (randomizedLapArray[lapCount])
    //        {
    //            //control lap
    //            case 0:
    //                fps_var = 60;
    //                break;
    //            case 1:
    //                fps_var = 60;
    //                break;
    //            case 2:
    //                fps_var = 60;
    //                break;
    //            case 3:
    //                fps_var = 60;
    //                break;
    //            case 4:
    //                fps_var = 60;
    //                break;
    //            case 5:
    //                fps_var = 60;
    //                break;
    //            case 6:
    //                fps_var = 60;
    //                break;
    //            default:
    //                fps_var = 0;
    //                break;
    //        }
    //}
    // UI style for debug render
    static GUIStyle style = new GUIStyle();

    //======================================================================================
    static GUIStyle timerStyle = new GUIStyle();
    static GUIStyle countdownStyle = new GUIStyle();
    //======================================================================================

    // For alloc-free raycasts
    Ray wheelRay = new Ray();
    RaycastHit[] wheelRayHits = new RaycastHit[16];

    //===================FINISH_LINE_STUFF============================================================
    void WheelDataPrint()
    {
        for (int axleIndex = 0; axleIndex < axles.Length; axleIndex++)
        {
            Axle axle = axles[axleIndex];
            //Debug.Log("LEFT WHEEL STUFF");
            //Debug.Log("Left Wheel Position X:" + axle.wheelVisualLeft.transform.position.x);
            //Debug.Log("Left Wheel Position Y:" + axle.wheelVisualLeft.transform.position.y);
            //Debug.Log("Left Wheel Position Z:" + axle.wheelVisualLeft.transform.position.z);

            //Debug.Log("=======================================================");

            //Debug.Log("RIGHT WHEEL STUFF");
            //Debug.Log("Right Wheel Position X:" + axle.wheelVisualRight.transform.position.x);
            //Debug.Log("Right Wheel Position Y:" + axle.wheelVisualRight.transform.position.y);
            //Debug.Log("Right Wheel Position Z:" + axle.wheelVisualRight.transform.position.z);
            //Debug.Log("Right Wheel Position:" + axle.wheelVisualRight.transform.position.x);

            if ((axle.wheelVisualLeft.transform.position.z > -220) && (axle.wheelVisualRight.transform.position.z < -196) &&
                (axle.wheelVisualLeft.transform.position.x < 357) && (axle.wheelVisualRight.transform.position.x < 357))
            {
                //Debug.Log("PLACE MESSAGE HERE");
                if (enableFinishFlag) FINISH_LINE_FLAG = true;

            }
        }
    }

    //==================================================================================================

    void Reset(Vector3 position, bool is_absolute = false)
    {
        if (is_absolute)
        {
            position = new Vector3(361, 4, -95);
        }
        else
        {
            position += new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0.0f, UnityEngine.Random.Range(-1.0f, 1.0f));
        }
        float yaw = transform.eulerAngles.y + UnityEngine.Random.Range(-10.0f, 10.0f);

        transform.position = position;
        if (is_absolute)
        {
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, 90.0f, 0.0f));
        }
        else
        {
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, yaw, 0.0f));
        }

        rb.velocity = new Vector3(0f, 0f, 0f);
        rb.angularVelocity = new Vector3(0f, 0f, 0f);

        for (int axleIndex = 0; axleIndex < axles.Length; axleIndex++)
        {
            axles[axleIndex].steerAngle = 0.0f;
        }

        Debug.Log(string.Format("Reset {0}, {1}, {2}, Rot {3}", position.x, position.y, position.z, yaw));
    }

    void Start()
    {
        Screen.SetResolution(1280, 720, true);
        int[] tempArray = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };
        randomizedLapArray = Randomize(tempArray);
        style.normal.textColor = Color.red;

        //=====================code====================================================
        timerStyle.normal.textColor = Color.yellow;

        timerStyle.fontSize = 20;
        countdownStyle.normal.textColor = Color.red;
        countdownStyle.fontSize = 50;
        //=========================================================================

        if (!Input.gyro.enabled)
        {
            Input.gyro.enabled = true;
        }
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
    }

    void OnValidate()
    {
        //HACK: to apply steering in editor
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        ApplyVisual();
        CalculateAckermannSteering();
    }

    float GetHandBrakeK()
    {
        float x = handBrakeSlipperyTiresTime / Math.Max(0.1f, handBrakeSlipperyTime);
        // smoother step
        x = x * x * x * (x * (x * 6 - 15) + 10);
        return x;
    }

    float GetSteeringHandBrakeK()
    {
        // 0.4 - pressed
        // 1.0 - not pressed
        float steeringK = Mathf.Clamp01(0.4f + (1.0f - GetHandBrakeK()) * 0.6f);
        return steeringK;
    }

    float GetAccelerationForceMagnitude(AnimationCurve accCurve, float speedMetersPerSec, float dt)
    {
        float speedKmH = speedMetersPerSec * 3.6f;

        float mass = rb.mass;

        int numKeys = accCurve.length;
        if (numKeys == 0)
        {
            return 0.0f;
        }

        if (numKeys == 1)
        {
            float desiredSpeed = accCurve.keys[0].value;
            float acc = (desiredSpeed - speedKmH);

            //to meters per sec
            acc /= 3.6f;
            float forceMag = (acc * mass);
            forceMag = Mathf.Max(forceMag, 0.0f);
            return forceMag;
        }


        //brute-force linear search
        float minTime = accCurve.keys[0].time;
        float maxTime = accCurve.keys[numKeys - 1].time;

        float step = (maxTime - minTime) / 500.0f;
        float prev_speed = Mathf.Min(accCurve.keys[0].value, speedKmH);

        float timeNow = 0.0f;
        bool isResultFound = false;

        for (float t = minTime; t <= maxTime; t += step)
        {
            float cur_speed = accCurve.Evaluate(t);

            if (speedKmH >= prev_speed && speedKmH < cur_speed)
            {
                //found the right value
                isResultFound = true;
                timeNow = t;
            }

            prev_speed = cur_speed;
        }

        if (isResultFound)
        {
            //float speed_now = accCurve.Evaluate(timeNow);
            //Debug.Log(string.Format("sptime {0}, speed {1}, speedn {2}", timeNow, speedKmH, speed_now));

            float speed_desired = accCurve.Evaluate(timeNow + dt);

            float acc = (speed_desired - speedKmH);
            //to meters per sec
            acc /= 3.6f;
            float forceMag = (acc * mass);
            forceMag = Mathf.Max(forceMag, 0.0f);
            return forceMag;

        }

        if (debugDraw)
        {
            Debug.Log("Max speed reached!");
        }

        float _desiredSpeed = accCurve.keys[numKeys - 1].value;
        float _acc = (_desiredSpeed - speedKmH);
        //to meters per sec
        _acc /= 3.6f;
        float _forceMag = (_acc * mass);
        _forceMag = Mathf.Max(_forceMag, 0.0f);
        return _forceMag;

    }

    public float GetSpeed()
    {
        Vector3 velocity = rb.velocity;

        Vector3 wsForward = rb.transform.rotation * Vector3.forward;
        float vProj = Vector3.Dot(velocity, wsForward);
        Vector3 projVelocity = vProj * wsForward;
        float speed = projVelocity.magnitude * Mathf.Sign(vProj);
        return speed;
    }

    //TODO: Refactor (remove this func, GetAccelerationForceMagnitude is enough)
    float CalcAccelerationForceMagnitude()
    {
        if (!isAcceleration && !isReverseAcceleration)
        {
            return 0.0f;
        }

        float speed = GetSpeed();
        float dt = Time.fixedDeltaTime;

        if (isAcceleration)
        {
            float forceMag = GetAccelerationForceMagnitude(accelerationCurve, speed, dt);
            return forceMag;
        }
        else
        {
            float forceMag = GetAccelerationForceMagnitude(accelerationCurveReverse, -speed, dt);
            return -forceMag;
        }

    }

    float GetSteerAngleLimitInDeg(float speedMetersPerSec)
    {
        float speedKmH = speedMetersPerSec * 3.6f;

        // maximum angle limit when hand brake is pressed
        speedKmH *= GetSteeringHandBrakeK();

        float limitDegrees = steerAngleLimit.Evaluate(speedKmH);

        return limitDegrees;
    }

    //======================code=============================================
    float bestScore()
    {
        float dummy = 9999999999;
        //make sure there is at least one score
        if (scoreBoard.Count != 0)
        {
            foreach (var x in scoreBoard)
            {
                if (x < dummy && x != 0) dummy = x;
            }
            return dummy;
        }
        else return -1;
    }

    float bestScore0()
    {
        if (scoreBoard.Count != 0) return scoreBoard[0];

        else return -1;
    }

    List<float> bestScores()
    {
        //float dummy = 9999999999;
        List<float> scoreList = new List<float>();

        //make sure there is at least one score
        if (scoreBoard.Count != 0)
        {
            int scoreNum = 3;
            if (scoreNum >= scoreBoard.Count) scoreNum = scoreBoard.Count;
            return scoreBoard.GetRange(0, scoreNum);
        }
        else return scoreList;
    }
    public String countdownTime = "";
    //====================================================================
    bool dataFlag = false;

    void UpdateInput()
    {
        float v = 0;// = Input.GetAxis("Vertical");
        float h = 0;//= Input.GetAxis("Horizontal");

        if (controlsDisabled == false)
        {
            // v = Input.GetAxis("Vertical");
            v = 0;
            if (forward_pressed)
            {
                v += 1.0f;
            }
            if (reverse_pressed)
            {
                v -= 1.0f;
            }
            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                h = Input.GetAxis("Horizontal");
            }
            else if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                h = Input.acceleration.x * 12;
            }
            //Implements Latency with q_len size buffer
            input_queue.Enqueue(new DataInputContainer(v, h));
            if (input_queue.Count >= q_len)
            {
                DataInputContainer input = input_queue.Dequeue();
                v = input.v;
                h = input.h;
            }

            int shakiness = Math.Max(1, Math.Min(60, (int)normalRandom(fps, fps_var)));
            Application.targetFrameRate = shakiness;            // stability shakiness
            Debug.Log("shakiness = " + shakiness);

            Touch[] myTouches = Input.touches;
        }
        if (Input.GetKey(KeyCode.G))
        {
            gKey = (gKey + 1) % Math.Max(2, Application.targetFrameRate);
            if (gKey == 1)
            {
                Application.targetFrameRate = Math.Max(1, Application.targetFrameRate - 1);
            }
        }
        else
        {
            gKey = 0;
        }
        if (Input.GetKey(KeyCode.H))
        {
            hKey = (hKey + 1) % Math.Max(2, Application.targetFrameRate);
            if (hKey == 1)
            {
                Application.targetFrameRate = Math.Min(120, Application.targetFrameRate + 1);
            }
        }
        else
        {
            hKey = 0;
        }

        if (!controllable)
        {
            v = 0.0f;
            h = 0.0f;
        }

        if (reset_pressed && controllable)
        {

            //====================code========================================
            time = 0;
            startGame = false;
            startRace = false;
            scoreBoard.Clear();
            //============================================================


            Debug.Log("Reset pressed");
            Ray resetRay = new Ray();

            // trace top-down
            resetRay.origin = transform.position + new Vector3(0.0f, 100.0f, 0.0f);
            resetRay.direction = new Vector3(0.0f, -1.0f, 0.0f);

            RaycastHit[] resetRayHits = new RaycastHit[16];

            int numHits = Physics.RaycastNonAlloc(resetRay, resetRayHits, 250.0f);

            if (numHits > 0)
            {
                float nearestDistance = float.MaxValue;
                for (int j = 0; j < numHits; j++)
                {
                    if (resetRayHits[j].collider != null && resetRayHits[j].collider.isTrigger)
                    {
                        // skip triggers
                        continue;
                    }

                    // Filter contacts with car body
                    if (resetRayHits[j].rigidbody == rb)
                    {
                        continue;
                    }

                    if (resetRayHits[j].distance < nearestDistance)
                    {
                        nearestDistance = resetRayHits[j].distance;
                    }
                }

                // -4 meters from surface
                nearestDistance -= 4.0f;
                Vector3 resetPos = resetRay.origin + resetRay.direction * nearestDistance;
                Reset(resetPos);
            }
            else
            {
                // Hard reset
                Reset(new Vector3(-69.48f, 5.25f, 132.71f), true);
            }
        }

        //=====================code=================================================================

        bool startPressed = start_pressed && controllable;
        bool finishPressed = FINISH_LINE_FLAG;// = Input.GetKey(KeyCode.F) && controllable;

        if (finishPressed)
        {

            FINISH_LINE_FLAG = false;

            startGame = false;
            startRace = false;
            float bestScr = bestScore0();
            //float bestScr = scoreBoard[0];
            if (bestScr == -1 || time <= bestScr) countdownTime = "HIGH SCORE!";
            else countdownTime = "Good Try!";
            if (!scoreBoard.Contains(time)) TimeList.Add(formatTime(time / 2));
            if (!scoreBoard.Contains(time)) scoreBoard.Add(time);

            scoreBoard.Sort();

            //=============================================
            if (dataFlag == false)//&& lapCount > 0)//lapCount == lapCount &&
            {

                for (int i = 0; i < lapCount + 1; i++)
                {
                    resultText += "Lap ";
                    resultText += i.ToString();
                    resultText += ": FPS = ";
                    resultText += FPSList[i].ToString();
                    resultText += ", Res = ";
                    resultText += ResolutionList[i].ToString();
                    resultText += ", Latency = ";
                    resultText += LatencyList[i].ToString();
                    resultText += ", Stability = ";
                    resultText += StabilityList[i].ToString();
                    resultText += ", LT = ";
                    resultText += TimeList[i];
                    resultText += "\n";

                }
                lapCount++;
                dataFlag = true;

            }
            //=======================================
        }
        if (startPressed)
        {
            enableFinishFlag = true;
            FINISH_LINE_FLAG = false;
            time = 0;
            startGame = true;
            startRace = false;
            Reset(new Vector3(0f, 0f, 0f), true);
            //position = new Vector3(361, 4, -95);
            //transform.rotation = Quaternion.Euler(new Vector3(0.0f, 90.0f, 0.0f));
        }
        //======================================================================================

        bool isBrakeNow = false;
        bool isHandBrakeNow = Input.GetKey(KeyCode.Space) && controllable;

        float speed = GetSpeed();
        isAcceleration = false;
        isReverseAcceleration = false;
        if (v > 0.4f)
        {
            if (speed < -0.5f)
            {
                isBrakeNow = true;
            }
            else
            {
                isAcceleration = true;
            }
        }
        else if (v < -0.4f)
        {
            if (speed > 0.5f)
            {
                isBrakeNow = true;
            }
            else
            {
                isReverseAcceleration = true;
            }
        }

        // Make tires more slippery (for 1 seconds) when player hit brakes
        if (isBrakeNow == true && isBrake == false)
        {
            brakeSlipperyTiresTime = 1.0f;
        }

        // slippery tires while handsbrakes are pressed
        if (isHandBrakeNow == true)
        {
            handBrakeSlipperyTiresTime = Math.Max(0.1f, handBrakeSlipperyTime);
        }

        isBrake = isBrakeNow;

        // hand brake + acceleration = power slide
        isHandBrake = isHandBrakeNow && !isAcceleration && !isReverseAcceleration;

        axles[0].brakeLeft = isBrake;
        axles[0].brakeRight = isBrake;
        axles[1].brakeLeft = isBrake;
        axles[1].brakeRight = isBrake;

        axles[0].handBrakeLeft = isHandBrake;
        axles[0].handBrakeRight = isHandBrake;
        axles[1].handBrakeLeft = isHandBrake;
        axles[1].handBrakeRight = isHandBrake;


        //TODO: axles[0] always used for steering

        if (Mathf.Abs(h) > 0.001f)
        {
            float speedKmH = Mathf.Abs(speed) * 3.6f;

            // maximum steer speed when hand-brake is pressed
            speedKmH *= GetSteeringHandBrakeK();

            float steerSpeed = steeringSpeed.Evaluate(speedKmH);

            float newSteerAngle = axles[0].steerAngle + (h * steerSpeed);
            float sgn = Mathf.Sign(newSteerAngle);

            float steerLimit = GetSteerAngleLimitInDeg(speed);

            newSteerAngle = Mathf.Min(Math.Abs(newSteerAngle), steerLimit) * sgn;

            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                axles[0].steerAngle = newSteerAngle;
            }
            else if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                axles[0].steerAngle = h;
            }
        }
        else
        {
            float speedKmH = Mathf.Abs(speed) * 3.6f;

            float angleReturnSpeedDegPerSec = steeringResetSpeed.Evaluate(speedKmH);

            angleReturnSpeedDegPerSec = Mathf.Lerp(0.0f, angleReturnSpeedDegPerSec, Mathf.Clamp01(speedKmH / 2.0f));


            float ang = axles[0].steerAngle;
            float sgn = Mathf.Sign(ang);

            ang = Mathf.Abs(ang);

            ang -= angleReturnSpeedDegPerSec * Time.fixedDeltaTime;
            ang = Mathf.Max(ang, 0.0f) * sgn;

            axles[0].steerAngle = ang;
        }
    }

    void Update()
    {
        ApplyVisual();
        WheelDataPrint();
    }

    void FixedUpdate()
    {
        UpdateInput();

        accelerationForceMagnitude = CalcAccelerationForceMagnitude();

        // 0.8 - pressed
        // 1.0 - not pressed
        float accelerationK = Mathf.Clamp01(0.8f + (1.0f - GetHandBrakeK()) * 0.2f);
        accelerationForceMagnitude *= accelerationK;

        CalculateAckermannSteering();

        int numberOfPoweredWheels = 0;
        for (int axleIndex = 0; axleIndex < axles.Length; axleIndex++)
        {
            if (axles[axleIndex].isPowered)
            {
                numberOfPoweredWheels += 2;
            }
        }


        int totalWheelsCount = axles.Length * 2;
        for (int axleIndex = 0; axleIndex < axles.Length; axleIndex++)
        {
            CalculateAxleForces(axles[axleIndex], totalWheelsCount, numberOfPoweredWheels);
        }

        bool allWheelIsOnAir = true;
        for (int axleIndex = 0; axleIndex < axles.Length; axleIndex++)
        {
            if (axles[axleIndex].wheelDataL.isOnGround || axles[axleIndex].wheelDataR.isOnGround)
            {
                allWheelIsOnAir = false;
                break;
            }
        }

        if (allWheelIsOnAir)
        {
            // set after flight tire slippery time (1 sec)
            afterFlightSlipperyTiresTime = 1.0f;

            // Try to keep vehicle parallel to the ground while jumping
            Vector3 carUp = transform.TransformDirection(new Vector3(0.0f, 1.0f, 0.0f));
            Vector3 worldUp = new Vector3(0.0f, 1.0f, 0.0f);

            // Flight stabilization from
            // https://github.com/supertuxkart/stk-code/blob/master/src/physics/btKart.cpp#L455

            // Length of axis depends on the angle - i.e. the further awat
            // the kart is from being upright, the larger the applied impulse
            // will be, resulting in fast changes when the kart is on its
            // side, but not overcompensating (and therefore shaking) when
            // the kart is not much away from being upright.
            Vector3 axis = Vector3.Cross(carUp, worldUp);
            //axis.Normalize ();

            float mass = rb.mass;

            // angular velocity damping
            Vector3 angVel = rb.angularVelocity;

            Vector3 angVelDamping = angVel;
            angVelDamping.y = 0.0f;
            angVelDamping = angVelDamping * Mathf.Clamp01(flightStabilizationDamping * Time.fixedDeltaTime);

            //Debug.Log(string.Format("Ang {0}, Damping {1}", angVel, angVelDamping));
            rb.angularVelocity = angVel - angVelDamping;

            // in flight roll stabilization
            rb.AddTorque(axis * flightStabilizationForce * mass);
        }
        else
        {
            // downforce
            Vector3 carDown = transform.TransformDirection(new Vector3(0.0f, -1.0f, 0.0f));

            float speed = GetSpeed();
            float speedKmH = Mathf.Abs(speed) * 3.6f;

            float downForceAmount = downForceCurve.Evaluate(speedKmH) / 100.0f;

            float mass = rb.mass;

            rb.AddForce(carDown * mass * downForceAmount * downForce);

            //Debug.Log(string.Format("{0} downforce", downForceAmount * downForce));
        }

        if (afterFlightSlipperyTiresTime > 0.0f)
        {
            afterFlightSlipperyTiresTime -= Time.fixedDeltaTime;
        }
        else
        {
            afterFlightSlipperyTiresTime = 0.0f;
        }

        if (brakeSlipperyTiresTime > 0.0f)
        {
            brakeSlipperyTiresTime -= Time.fixedDeltaTime;
        }
        else
        {
            brakeSlipperyTiresTime = 0.0f;
        }

        if (handBrakeSlipperyTiresTime > 0.0f)
        {
            handBrakeSlipperyTiresTime -= Time.fixedDeltaTime;
        }
        else
        {
            handBrakeSlipperyTiresTime = 0.0f;
        }

    }

    //===============================code===============================================================

    //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    public float getTime()
    {
        time += Time.deltaTime;
        return time / 2;
    }
    public String formatTime(float time)
    {
        if (time == -1) return "N/A";
        else
        {
            var minutes = Mathf.Floor(time / 60); //Divide the guiTime by sixty to get the minutes.
            var seconds = Mathf.Floor(time % 60);//Use the euclidean division for the seconds.
            var fraction = Mathf.Floor((time * 100) % 100);
            String stringTime = string.Format("{0:00} : {1:00} : {2:00}", minutes, seconds, fraction);
            return stringTime;
        }
    }

    String doCountdown()
    {
        int count = (int)(5 - (time / 2));
        time += Time.deltaTime;
        if (count == 1) return "Go!!";
        else return (count - 1).ToString();
    }

    //==============================================================================================

    public String formattedTime = "";
    void OnGUI()
    {
        if (FINISH_LINE_FLAG)
        {
            //GUI.Label(new Rect(200.0f, 200.0f, 500, 500), string.Format("FINISH"), style);
            controlsDisabled = true;
        }

        if (!controllable)
        {
            return;
        }

        float speed = GetSpeed();

        //======================================code=======================================================
        //String formattedTime = "";
        if (startGame & !startRace)
        {
            resultText = "";
            countdownTime = doCountdown();
            controlsDisabled = true;
            formattedTime = "";
            changeLapVariables();
            //changeResolution();
            //changeFPS();
            //changeLatency();
            //changeStability();
        }
        if (countdownTime == "-1")
        {
            controlsDisabled = false;
            time = 0;
            //changeLapVariables();
            //changeResolution();
            //lapCount++;
            //Debug.Log(randomizedLapArray[lapCount]);
            //Debug.Log(Application.targetFrameRate);
            startRace = true;
            countdownTime = "";
            dataFlag = false;


        }
        if (startRace) formattedTime = formatTime(getTime());
        //================================================================================================

        float speedKmH = speed * 3.6f;

        //=======================================code=======================================================
        //GUI.Label(new Rect(850.0f, 50.0f, 150, 130), "Lap Time: " + formattedTime, timerStyle);
        //GUI.Label(new Rect(850.0f, 500.0f, 200, 200), countdownTime, countdownStyle);

        top5ScoresString = "";
        List<float> top5Scores = bestScores();
        int count = 1;
        if (top5Scores.Count == 0) top5ScoresString = "N/A";
        else foreach (var x in top5Scores)
            {
                top5ScoresString += String.Format("#{0} == ", count);
                count++;
                top5ScoresString += formatTime(x / 2);
                top5ScoresString += "\n";
            }

        //GUI.Box(new Rect(30.0f, 150.0f, 150, 130), "====== Scoreboard ======\nBest score: " + formatTime(bestScore0()), timerStyle);
        //GUI.Box(new Rect(30.0f, 150.0f, 150, 130), "====== Scoreboard ======\n" + top5ScoresString, timerStyle);"====== Scoreboard ======\n" + top5ScoresString, timerStyle);
        float screenHeight = Screen.height;
        Debug.Log(screenHeight);
        float screenWidth = Screen.width;
        Debug.Log(screenWidth);
        //GUI.Box(new Rect(screenWidth * (0.75f), screenHeight * (0.333f), screenWidth / (6.0f), screenHeight / 3), "====== Scoreboard ======\n" + top5ScoresString, timerStyle);
        //GUI.Box(new Rect(screenWidth * (0.666f), screenHeight * (0.666f), screenWidth / (6.0f), screenHeight / 3), resultText, timerStyle);
        //====================================================================================================

        /*GUI.Label(new Rect(30.0f, 20.0f, 150, 130), string.Format("{0:F2} km/h", speedKmH), style);
        GUI.Label(new Rect(30.0f, 40.0f, 150, 130), string.Format("{0:F2} {1:F2} {2:F2}", afterFlightSlipperyTiresTime, brakeSlipperyTiresTime, handBrakeSlipperyTiresTime), style);
        float yPos = 60.0f;
        for (int axleIndex = 0; axleIndex < axles.Length; axleIndex++)
        {
            GUI.Label(new Rect(30.0f, yPos, 150, 130), string.Format("Axle {0}, steering angle {1:F2}", axleIndex, axles[axleIndex].steerAngle), style);
            yPos += 18.0f;
        }*/

        Camera cam = Camera.current;
        if (cam == null)
        {
            return;
        }

        if (debugDraw)
        {
            foreach (Axle axle in axles)
            {
                Vector3 localL = new Vector3(axle.width * -0.5f, axle.offset.y, axle.offset.x);
                Vector3 localR = new Vector3(axle.width * 0.5f, axle.offset.y, axle.offset.x);

                Vector3 wsL = transform.TransformPoint(localL);
                Vector3 wsR = transform.TransformPoint(localR);

                for (int wheelIndex = 0; wheelIndex < 2; wheelIndex++)
                {
                    WheelData wheelData = (wheelIndex == WHEEL_LEFT_INDEX) ? axle.wheelDataL : axle.wheelDataR;
                    Vector3 wsFrom = (wheelIndex == WHEEL_LEFT_INDEX) ? wsL : wsR;

                    Vector3 screenPos = cam.WorldToScreenPoint(wsFrom);
                    //GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 150, 130), wheelData.debugText, style);
                }
            }
        }

    }



    void AddForceAtPosition(Vector3 force, Vector3 position)
    {
        rb.AddForceAtPosition(force, position);
        //Debug.DrawRay(position, force, Color.magenta);
    }


#if UNITY_EDITOR
    void OnDrawGizmosAxle(Vector3 wsDownDirection, Axle axle)
    {
        Vector3 localL = new Vector3(axle.width * -0.5f, axle.offset.y, axle.offset.x);
        Vector3 localR = new Vector3(axle.width * 0.5f, axle.offset.y, axle.offset.x);

        Vector3 wsL = transform.TransformPoint(localL);
        Vector3 wsR = transform.TransformPoint(localR);

        Gizmos.color = axle.debugColor;

        //draw axle
        Gizmos.DrawLine(wsL, wsR);

        //draw line to com
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(0.0f, axle.offset.y, axle.offset.x)), transform.TransformPoint(centerOfMass));

        for (int wheelIndex = 0; wheelIndex < 2; wheelIndex++)
        {
            WheelData wheelData = (wheelIndex == WHEEL_LEFT_INDEX) ? axle.wheelDataL : axle.wheelDataR;
            Vector3 wsFrom = (wheelIndex == WHEEL_LEFT_INDEX) ? wsL : wsR;

            Gizmos.color = wheelData.isOnGround ? Color.yellow : axle.debugColor;
            UnityEditor.Handles.color = Gizmos.color;

            float suspCurrentLen = Mathf.Clamp01(1.0f - wheelData.compression) * axle.lengthRelaxed;

            Vector3 wsTo = wsFrom + wsDownDirection * suspCurrentLen;

            // Draw suspension
            Gizmos.DrawLine(wsFrom, wsTo);

            Quaternion localWheelRot = Quaternion.Euler(new Vector3(0.0f, wheelData.yawRad * Mathf.Rad2Deg, 0.0f));
            Quaternion wsWheelRot = transform.rotation * localWheelRot;

            Vector3 localAxle = (wheelIndex == WHEEL_LEFT_INDEX) ? Vector3.left : Vector3.right;
            Vector3 wsAxle = wsWheelRot * localAxle;
            Vector3 wsForward = wsWheelRot * Vector3.forward;

            // Draw wheel axle
            Gizmos.DrawLine(wsTo, wsTo + wsAxle * 0.1f);
            Gizmos.DrawLine(wsTo, wsTo + wsForward * axle.radius);

            // Draw wheel
            UnityEditor.Handles.DrawWireDisc(wsTo, wsAxle, axle.radius);

            UnityEditor.Handles.DrawWireDisc(wsTo + wsAxle * wheelWidth, wsAxle, axle.radius);
            UnityEditor.Handles.DrawWireDisc(wsTo - wsAxle * wheelWidth, wsAxle, axle.radius);


        }

    }



    void OnDrawGizmos()
    {
        Vector3 wsDownDirection = transform.TransformDirection(Vector3.down);
        wsDownDirection.Normalize();
        foreach (Axle axle in axles)
        {
            OnDrawGizmosAxle(wsDownDirection, axle);
        }
        Gizmos.DrawSphere(transform.TransformPoint(centerOfMass), 0.1f);
    }
#endif

    bool RayCast(Ray ray, float maxDistance, ref RaycastHit nearestHit)
    {
        int numHits = Physics.RaycastNonAlloc(wheelRay, wheelRayHits, maxDistance);
        if (numHits == 0)
        {
            return false;
        }

        // Find the nearest hit point and filter invalid hits
        ////////////////////////////////////////////////////////////////////////////////////////

        nearestHit.distance = float.MaxValue;
        for (int j = 0; j < numHits; j++)
        {
            if (wheelRayHits[j].collider != null && wheelRayHits[j].collider.isTrigger)
            {
                // skip triggers
                continue;
            }

            // Skip contacts with car body
            if (wheelRayHits[j].rigidbody == rb)
            {
                continue;
            }

            // Skip contacts with strange normals (walls?)
            if (Vector3.Dot(wheelRayHits[j].normal, new Vector3(0.0f, 1.0f, 0.0f)) < 0.6f)
            {
                continue;
            }

            if (wheelRayHits[j].distance < nearestHit.distance)
            {
                nearestHit = wheelRayHits[j];
            }
        }
        ////////////////////////////////////////////////////////////////////////////////////////

        return (nearestHit.distance <= maxDistance);
    }


    void CalculateWheelForces(Axle axle, Vector3 wsDownDirection, WheelData wheelData, Vector3 wsAttachPoint, int wheelIndex, int totalWheelsCount, int numberOfPoweredWheels)
    {
        float dt = Time.fixedDeltaTime;

        wheelData.debugText = "--";

        // Get wheel world space rotation and axes
        Quaternion localWheelRot = Quaternion.Euler(new Vector3(0.0f, wheelData.yawRad * Mathf.Rad2Deg, 0.0f));
        Quaternion wsWheelRot = transform.rotation * localWheelRot;

        // Wheel axle left direction
        Vector3 wsAxleLeft = wsWheelRot * Vector3.left;

        wheelData.isOnGround = false;
        wheelRay.direction = wsDownDirection;


        // Ray cast (better to use shape cast here, but Unity does not support shape casts)

        float traceLen = axle.lengthRelaxed + axle.radius;

        wheelRay.origin = wsAttachPoint + wsAxleLeft * wheelWidth;
        RaycastHit s1 = new RaycastHit();
        bool b1 = RayCast(wheelRay, traceLen, ref s1);

        wheelRay.origin = wsAttachPoint - wsAxleLeft * wheelWidth;
        RaycastHit s2 = new RaycastHit();
        bool b2 = RayCast(wheelRay, traceLen, ref s2);

        wheelRay.origin = wsAttachPoint;
        bool isCollided = RayCast(wheelRay, traceLen, ref wheelData.touchPoint);

        // No wheel contant found
        if (!isCollided || !b1 || !b2)
        {
            // wheel do not touch the ground (relaxing spring)
            float relaxSpeed = 1.0f;
            wheelData.compressionPrev = wheelData.compression;
            wheelData.compression = Mathf.Clamp01(wheelData.compression - dt * relaxSpeed);
            return;
        }

        // Consider wheel radius
        float suspLenNow = wheelData.touchPoint.distance - axle.radius;

        Debug.AssertFormat(suspLenNow <= traceLen, "Sanity check failed.");

        wheelData.isOnGround = true;


        //
        // Calculate suspension force
        //
        // Spring force - want's go to position 0
        // Damping force - want's go to velocity 0
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        float suspForceMag = 0.0f;

        // Positive value means that the spring is compressed
        // Negative value means that the spring is elongated.

        wheelData.compression = 1.0f - Mathf.Clamp01(suspLenNow / axle.lengthRelaxed);

        wheelData.debugText = wheelData.compression.ToString("F2");

        // Hooke's law (springs)
        // F = -k x

        // Spring force (try to reset compression from spring)
        float springForce = wheelData.compression * -axle.stiffness;
        suspForceMag += springForce;

        // Damping force (try to reset velocity to 0)
        float suspCompressionVelocity = (wheelData.compression - wheelData.compressionPrev) / dt;
        wheelData.compressionPrev = wheelData.compression;

        float damperForce = -suspCompressionVelocity * axle.damping;
        suspForceMag += damperForce;

        // Only consider component of force that is along the contact normal.
        float denom = Vector3.Dot(wheelData.touchPoint.normal, -wsDownDirection);
        suspForceMag *= denom;

        // Apply suspension force
        Vector3 suspForce = wsDownDirection * suspForceMag;
        AddForceAtPosition(suspForce, wheelData.touchPoint.point);


        //
        // Calculate friction forces
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        ///

        Vector3 wheelVelocity = rb.GetPointVelocity(wheelData.touchPoint.point);

        // Contact basis (can be different from wheel basis)
        Vector3 c_up = wheelData.touchPoint.normal;
        Vector3 c_left = (s1.point - s2.point).normalized;
        Vector3 c_fwd = Vector3.Cross(c_up, c_left);

        // Calculate sliding velocity (velocity without normal force)
        Vector3 lvel = Vector3.Dot(wheelVelocity, c_left) * c_left;
        Vector3 fvel = Vector3.Dot(wheelVelocity, c_fwd) * c_fwd;
        Vector3 slideVelocity = (lvel + fvel) * 0.5f;

        // Calculate current sliding force
        Vector3 slidingForce = (slideVelocity * rb.mass / dt) / (float)totalWheelsCount;

        if (debugDraw)
        {
            Debug.DrawRay(wheelData.touchPoint.point, slideVelocity, Color.red);
        }

        float laterialFriction = Mathf.Clamp01(axle.laterialFriction);


        float slipperyK = 1.0f;

        //Simulate slippery tires
        if (afterFlightSlipperyTiresTime > 0.0f)
        {
            float slippery = Mathf.Lerp(1.0f, axle.afterFlightSlipperyK, Mathf.Clamp01(afterFlightSlipperyTiresTime));
            slipperyK = Mathf.Min(slipperyK, slippery);
        }

        if (brakeSlipperyTiresTime > 0.0f)
        {
            float slippery = Mathf.Lerp(1.0f, axle.brakeSlipperyK, Mathf.Clamp01(brakeSlipperyTiresTime));
            slipperyK = Mathf.Min(slipperyK, slippery);
        }

        float handBrakeK = GetHandBrakeK();
        if (handBrakeK > 0.0f)
        {
            float slippery = Mathf.Lerp(1.0f, axle.handBrakeSlipperyK, handBrakeK);
            slipperyK = Mathf.Min(slipperyK, slippery);
        }

        /*
        if (slipperyK < 0.99f)
        {
            Debug.Log(string.Format("Slippery {0:F2}", slipperyK));
        }
        */

        laterialFriction = laterialFriction * slipperyK;


        // Simulate perfect static friction
        Vector3 frictionForce = -slidingForce * laterialFriction;

        // Remove friction along roll-direction of wheel
        Vector3 longitudinalForce = Vector3.Dot(frictionForce, c_fwd) * c_fwd;

        // Apply braking force or rolling resistance force or nothing
        bool isBrakeEnabled = (wheelIndex == WHEEL_LEFT_INDEX) ? axle.brakeLeft : axle.brakeRight;
        bool isHandBrakeEnabled = (wheelIndex == WHEEL_LEFT_INDEX) ? axle.handBrakeLeft : axle.handBrakeRight;
        if (isBrakeEnabled || isHandBrakeEnabled)
        {
            float clampedMag = Mathf.Clamp(axle.brakeForceMag * rb.mass, 0.0f, longitudinalForce.magnitude);
            Vector3 brakeForce = longitudinalForce.normalized * clampedMag;

            if (isHandBrakeEnabled)
            {
                // hand brake are not powerful enough ;)
                brakeForce = brakeForce * 0.8f;
            }

            longitudinalForce -= brakeForce;
        }
        else
        {

            // Apply rolling-friction (automatic slow-down) only if player don't press to the accelerator
            if (!isAcceleration && !isReverseAcceleration)
            {
                float rollingK = 1.0f - Mathf.Clamp01(axle.rollingFriction);
                longitudinalForce *= rollingK;
            }
        }

        frictionForce -= longitudinalForce;

        if (debugDraw)
        {
            Debug.DrawRay(wheelData.touchPoint.point, frictionForce, Color.red);
            Debug.DrawRay(wheelData.touchPoint.point, longitudinalForce, Color.white);
        }

        // Apply resulting force
        AddForceAtPosition(frictionForce, wheelData.touchPoint.point);


        // Engine force
        if (!isBrake && axle.isPowered && Mathf.Abs(accelerationForceMagnitude) > 0.01f)
        {
            Vector3 accForcePoint = wheelData.touchPoint.point - (wsDownDirection * 0.2f);
            Vector3 engineForce = c_fwd * accelerationForceMagnitude / (float)numberOfPoweredWheels / dt;
            AddForceAtPosition(engineForce, accForcePoint);

            if (debugDraw)
            {
                Debug.DrawRay(accForcePoint, engineForce, Color.green);
            }
        }
        //




    }


    void CalculateAxleForces(Axle axle, int totalWheelsCount, int numberOfPoweredWheels)
    {
        Vector3 wsDownDirection = transform.TransformDirection(Vector3.down);
        wsDownDirection.Normalize();

        Vector3 localL = new Vector3(axle.width * -0.5f, axle.offset.y, axle.offset.x);
        Vector3 localR = new Vector3(axle.width * 0.5f, axle.offset.y, axle.offset.x);

        Vector3 wsL = transform.TransformPoint(localL);
        Vector3 wsR = transform.TransformPoint(localR);

        // For each wheel
        for (int wheelIndex = 0; wheelIndex < 2; wheelIndex++)
        {
            WheelData wheelData = (wheelIndex == WHEEL_LEFT_INDEX) ? axle.wheelDataL : axle.wheelDataR;
            Vector3 wsFrom = (wheelIndex == WHEEL_LEFT_INDEX) ? wsL : wsR;

            CalculateWheelForces(axle, wsDownDirection, wheelData, wsFrom, wheelIndex, totalWheelsCount, numberOfPoweredWheels);
        }

        // http://projects.edy.es/trac/edy_vehicle-physics/wiki/TheStabilizerBars
        // Apply "stablizier bar" forces
        float travelL = 1.0f - Mathf.Clamp01(axle.wheelDataL.compression);
        float travelR = 1.0f - Mathf.Clamp01(axle.wheelDataR.compression);

        float antiRollForce = (travelL - travelR) * axle.antiRollForce;
        if (axle.wheelDataL.isOnGround)
        {
            AddForceAtPosition(wsDownDirection * antiRollForce, axle.wheelDataL.touchPoint.point);
            if (debugDraw)
            {
                Debug.DrawRay(axle.wheelDataL.touchPoint.point, wsDownDirection * antiRollForce, Color.magenta);
            }
        }

        if (axle.wheelDataR.isOnGround)
        {
            AddForceAtPosition(wsDownDirection * -antiRollForce, axle.wheelDataR.touchPoint.point);
            if (debugDraw)
            {
                Debug.DrawRay(axle.wheelDataR.touchPoint.point, wsDownDirection * -antiRollForce, Color.magenta);
            }
        }

    }


    void CalculateAckermannSteering()
    {
        // Copy desired steering
        for (int axleIndex = 0; axleIndex < axles.Length; axleIndex++)
        {
            float steerAngleRad = axles[axleIndex].steerAngle * Mathf.Deg2Rad;

            axles[axleIndex].wheelDataL.yawRad = steerAngleRad;
            axles[axleIndex].wheelDataR.yawRad = steerAngleRad;
        }

        if (axles.Length != 2)
        {
            Debug.LogWarning("Ackermann work only for 2 axle vehicles.");
            return;
        }

        Axle frontAxle = axles[0];
        Axle rearAxle = axles[1];

        if (Mathf.Abs(rearAxle.steerAngle) > 0.0001f)
        {
            Debug.LogWarning("Ackermann work only for vehicles with forward steering axle.");
            return;
        }

        // Calculate our chassis (remove scale)
        Vector3 axleDiff = transform.TransformPoint(new Vector3(0.0f, frontAxle.offset.y, frontAxle.offset.x)) - transform.TransformPoint(new Vector3(0.0f, rearAxle.offset.y, rearAxle.offset.x));
        float axleSeparation = axleDiff.magnitude;

        Vector3 wheelDiff = transform.TransformPoint(new Vector3(frontAxle.width * -0.5f, frontAxle.offset.y, frontAxle.offset.x)) - transform.TransformPoint(new Vector3(frontAxle.width * 0.5f, frontAxle.offset.y, frontAxle.offset.x));
        float wheelsSeparation = wheelDiff.magnitude;

        if (Mathf.Abs(frontAxle.steerAngle) < 0.001f)
        {
            // Sterring wheels are not turned
            return;
        }

        // Simple right-angled triangle math (find cathet if we know another cathet and angle)

        // Find cathet from cathet and angle
        //
        // axleSeparation - is first cathet
        // steerAngle - is angle between cathet and hypotenuse
        float rotationCenterOffsetL = axleSeparation / Mathf.Tan(frontAxle.steerAngle * Mathf.Deg2Rad);

        // Now we have another 2 cathet's (rotationCenterOffsetR and axleSeparation for second wheel)
        //  need to find right angle
        float rotationCenterOffsetR = rotationCenterOffsetL - wheelsSeparation;

        float rightWheelYaw = Mathf.Atan(axleSeparation / rotationCenterOffsetR);

        frontAxle.wheelDataR.yawRad = rightWheelYaw;
    }


    void CalculateWheelVisualTransform(Vector3 wsAttachPoint, Vector3 wsDownDirection, Axle axle, WheelData data, int wheelIndex, float visualRotationRad, out Vector3 pos, out Quaternion rot)
    {
        float suspCurrentLen = Mathf.Clamp01(1.0f - data.compression) * axle.lengthRelaxed;

        pos = wsAttachPoint + wsDownDirection * suspCurrentLen;

        float additionalYaw = 0.0f;
        float additionalMul = Mathf.Rad2Deg;
        if (wheelIndex == WHEEL_LEFT_INDEX)
        {
            additionalYaw = 180.0f;
            additionalMul = -Mathf.Rad2Deg;
        }

        Quaternion localWheelRot = Quaternion.Euler(new Vector3(data.visualRotationRad * additionalMul, additionalYaw + data.yawRad * Mathf.Rad2Deg, 0.0f));
        rot = transform.rotation * localWheelRot;
    }

    void CalculateWheelRotationFromSpeed(Axle axle, WheelData data, Vector3 wsPos)
    {
        if (rb == null)
        {
            data.visualRotationRad = 0.0f;
            return;
        }

        Quaternion localWheelRot = Quaternion.Euler(new Vector3(0.0f, data.yawRad * Mathf.Rad2Deg, 0.0f));
        Quaternion wsWheelRot = transform.rotation * localWheelRot;

        Vector3 wsWheelForward = wsWheelRot * Vector3.forward;
        Vector3 velocityQueryPos = data.isOnGround ? data.touchPoint.point : wsPos;
        Vector3 wheelVelocity = rb.GetPointVelocity(velocityQueryPos);
        // Longitudinal speed (meters/sec)
        float tireLongSpeed = Vector3.Dot(wheelVelocity, wsWheelForward);

        // Circle length = 2 * PI * R
        float wheelLengthMeters = 2 * Mathf.PI * axle.radius;

        // Wheel "Revolutions per second";
        float rps = tireLongSpeed / wheelLengthMeters;

        float deltaRot = Mathf.PI * 2.0f * rps * Time.deltaTime;

        data.visualRotationRad += deltaRot;
    }

    void ApplyVisual()
    {
        Vector3 wsDownDirection = transform.TransformDirection(Vector3.down);
        wsDownDirection.Normalize();

        for (int axleIndex = 0; axleIndex < axles.Length; axleIndex++)
        {
            Axle axle = axles[axleIndex];

            Vector3 localL = new Vector3(axle.width * -0.5f, axle.offset.y, axle.offset.x);
            Vector3 localR = new Vector3(axle.width * 0.5f, axle.offset.y, axle.offset.x);

            Vector3 wsL = transform.TransformPoint(localL);
            Vector3 wsR = transform.TransformPoint(localR);

            Vector3 wsPos;
            Quaternion wsRot;

            if (axle.wheelVisualLeft != null)
            {
                CalculateWheelVisualTransform(wsL, wsDownDirection, axle, axle.wheelDataL, WHEEL_LEFT_INDEX, axle.wheelDataL.visualRotationRad, out wsPos, out wsRot);
                axle.wheelVisualLeft.transform.position = wsPos;
                axle.wheelVisualLeft.transform.rotation = wsRot;
                axle.wheelVisualLeft.transform.localScale = new Vector3(axle.radius, axle.radius, axle.radius) * axle.visualScale;

                if (!isBrake)
                {
                    CalculateWheelRotationFromSpeed(axle, axle.wheelDataL, wsPos);
                }
            }

            if (axle.wheelVisualRight != null)
            {
                CalculateWheelVisualTransform(wsR, wsDownDirection, axle, axle.wheelDataR, WHEEL_RIGHT_INDEX, axle.wheelDataR.visualRotationRad, out wsPos, out wsRot);
                axle.wheelVisualRight.transform.position = wsPos;
                axle.wheelVisualRight.transform.rotation = wsRot;
                axle.wheelVisualRight.transform.localScale = new Vector3(axle.radius, axle.radius, axle.radius) * axle.visualScale;

                if (!isBrake)
                {
                    CalculateWheelRotationFromSpeed(axle, axle.wheelDataR, wsPos);
                }
            }


            //visualRotationRad

        }

    }
}
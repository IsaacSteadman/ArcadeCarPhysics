using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Gamebench.Sdk
{
    class Configuration { }

    enum MarkerMessage
    {
        start_mark = 1,
        stop_mark = 2,
        invalidStep = 1000
    }

    enum MarkerType
    {
        program = 1,
        scene = 2,
        invalidType = 100
    }
    class GamebenchSDKLifeCycle : Singleton<GamebenchSDKLifeCycle>, IGamebenchSDKLifeCycle
    {
        private Scene currentScene;
        private string currentSceneName;
        public static string DEFAULT_SCENE_NAME = "DefaultScene";
        public static string PRE_DIRECTORY = "/data/data/";
        public static string POST_DIRECTORY = "/files/sdk/";
        public static int FOLDERLENGTH = 12;
        private GamebenchSDKAuto autoBinder;
        private static bool allowCalling = false;
        public static IDictionary<MetricType, float> frequencyThreshHoldList = new Dictionary<MetricType, float>()
        {
            { MetricType.FPS, 1f },//FPS and GPU is not configurable at the moment
            { MetricType.MEM, 4f  },
            { MetricType.GPU, 1f  }
        }; //default matric's frequency
        public IDictionary<MetricType, float> frequencyRuntime = new Dictionary<MetricType, float>()
        {
            { MetricType.FPS, 0f },//runtime of each metric
            { MetricType.MEM, 0f  },
            { MetricType.GPU, 0f  }
        };

        /*
         * define Android/iOS interface 
         */
#if UNITY_ANDROID
        [DllImport("gbsdk")]
        private static extern void gbsdk_lifecycleInit(string config_packageName);

        [DllImport("gbsdk")]
        private static extern void gbsdk_lifecycleDestroy();

        [DllImport("gbsdk")]
        private static extern void gbsdk_sessionStart(string config_endpoint, string config_emailaddress, string config_packageName, string config_productName,
            int config_scrwith, int config_scrheight, string config_gpuvendor, string config_gpuversion, string config_gpuextensions,
            string config_gpurender, string config_appversion, int config_appversioncode, string config_hardware, int config_refreshrate, string config_networkoperator,
            string config_simoperator, string config_lastupdated, string ios_version, int matric_fps, int matric_gpu, int matric_mem);

        [DllImport("gbsdk")]
        private static extern void gbsdk_sessionStop();

        [DllImport("gbsdk")]
        private static extern void gbsdk_cleanup();

        [DllImport("gbsdk")]
        private static extern void gbsdk_lifecycle_on_frame(uint fps);

        [DllImport("gbsdk")]
        private static extern void gbsdk_cleanupSpecificFile(string deletingfileName, string packageName);

        [DllImport("gbsdk")]
        private static extern void gbsdk_lifecycle_punch_marker(string markername, int markerstep, int markertype);

        [DllImport("gbsdk")]
        private static extern void gbsdk_on_gpu(string gpu_handware);

        [DllImport("gbsdk")]
        private static extern void gbsdk_off_gpu();

        [DllImport("gbsdk")]
        private static extern void gbsdk_lifecycle_cleanup_metricsdata();

        [DllImport("gbsdk")]
        private static extern void gbsdk_lifecycle_on_memoryUsage(int totalUsage, int nativeUsage, int dalvikUsage, int otherUsage);

        [DllImport("gbsdk", CharSet=CharSet.Ansi)]
        private static extern void getCurrentFolderNumber(byte[] sessionPathRaw, out int pathLength);

        [DllImport("gbsdk")]
        private static extern void gbsdk_cleanup_withoutSession(string packageName);
#endif

#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void gbsdk_lifecycleInit(string config_packageName);

        [DllImport("__Internal")]
        private static extern void gbsdk_lifecycleDestroy();

        [DllImport("__Internal")]
        private static extern void gbsdk_sessionStart(string config_endpoint, string config_emailaddress, string config_packageName, string config_productName,
            int config_scrwith, int config_scrheight, string config_gpuvendor, string config_gpuversion, string config_gpuextensions,
            string config_gpurender, string config_appversion, int config_appversioncode, string config_hardware, int config_refreshrate, string config_networkoperator,
            string config_simoperator, string config_lastupdated, string ios_version, int matric_fps, int matric_gpu, int matric_mem);

        [DllImport("__Internal")]
        private static extern void gbsdk_sessionStop();

        [DllImport("__Internal")]
        private static extern void gbsdk_cleanup();

        [DllImport("__Internal")]
        private static extern void gbsdk_lifecycle_on_frame(uint fps);

        [DllImport("__Internal")]
        private static extern void gbsdk_cleanupSpecificFile(string deletingfileName, string packageName);

        [DllImport("__Internal")]
        private static extern void gbsdk_lifecycle_punch_marker(string markername, int markerstep, int markertype);

        [DllImport("__Internal")]
        private static extern void gbsdk_lifecycle_cleanup_metricsdata();

        [DllImport("__Internal")]
        private static extern void gbsdk_lifecycle_on_memoryUsage(int totalUsage, int nativeUsage, int dalvikUsage, int otherUsage);

        [DllImport("__Internal")]
        private static extern void gbsdk_cleanup_withoutSession(string packageName);
#endif

        private bool SDKInitialize = false;
        private bool SessionStarted = false;
        private SDKConfiguration config;
        private static GamebenchSDKLifeCycle instance;
        public readonly string defaultEndPointLifeCycle = "https://web.gamebench.net/v1/sessions/sync";
        private string zipFilePrefix = "gbsdkz";
        private float runningTime = 0f;
        private float deltaTime = 0.0f;
        public readonly int SESSION_FILE_LENGTH = 21;
        public readonly int ICON_FILE_LENGTH = 11;
        public readonly string ICON_FILE_NAME = "appicon.png";

        private bool sceneChangeAutoGenerateMarker = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void AutoAdded()
        {
            if (SDKConfiguration.getInstance().sdkEnable == true)
            {
                instance = GamebenchSDKLifeCycle.Instance;
                if (!Application.isEditor)
                    MetaDataHelper.getInstance().apply();
            }
        }

        void InjectMemoryDataIntoUnity(string totalMemory)
        {
            //in next version 
            Debug.Log("total memory " + totalMemory);
        }
		
		void Awake()
        {
            //to prevent JIT, will trigger in advance in mono platform
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            UnityWebRequest www = null;
            www = UnityWebRequest.Post(defaultEndPointLifeCycle, formData);
        }

        void Start()
        {
            Debug.Log("Gamebench SDK lifeCycle module started");
        }

        public void UpdateMetricRuntime(float delta)
        {
            List<MetricType> metricKeys = new List<MetricType>(frequencyRuntime.Keys);
            foreach (MetricType metricKey in metricKeys)
            {
                frequencyRuntime[metricKey] += delta;
            }
        }

        private bool APIExecutable()
        {
          //  allowCalling = true;
            if (SDKConfiguration.getInstance().sdkAPIControlEnable == false)
            {
              // if (allowCalling == false) //in auto mode, only GamebenchSDKAuto can call SDKLifeCycle
              //  {
                    Debug.LogWarning("Cannot be called in AutoMode");
                    return false;
              //  }
            }
            return true;
        }

        private GBStatus ExecuteInitSDK()
        {
            if (SDKInitialize == true)
            {
                Debug.LogWarning("Gamebench SDK has already been initialized");
                return GBStatus.GB_SDK_ALREADY_INITIALIZED;
            }
            var packageName = MetaDataHelper.getInstance().packageName;
            Debug.Log("Init SDK in packageName " + packageName);
            if (!Application.isEditor)
            {
                gbsdk_lifecycleInit(packageName);
#if UNITY_ANDROID //notice iOS and android for icon path is different
                MetaDataHelper.getInstance().applyIconSave(Application.identifier, "/data/data/" + Application.identifier + "/files/sdk/");
#endif
            }

            SDKInitialize = true;
            return GBStatus.GB_SUCCESS;
        }


        public GBStatus InitSDK()
        {
            if (!APIExecutable())
                return GBStatus.GB_UNKNOWN_ERROR;
            return ExecuteInitSDK();
        }

        private GBStatus ExecuteDestroySDK()
        {
            Debug.Log("destroy sdk via API ... ");
            if (!Application.isEditor)
            {
                gbsdk_lifecycleDestroy();
                SDKInitialize = false;
            }

            return GBStatus.GB_SUCCESS;
        }

        public GBStatus DestroySDK()
        {
            if (!APIExecutable())
                return GBStatus.GB_UNKNOWN_ERROR;

            if (SDKInitialize == false)
            {
                Debug.LogWarning("Gamebench SDK has already been destroyed");
                return GBStatus.GB_SDK_ALREADY_STOPPED;
            }
            return ExecuteDestroySDK();
        }

        public GBStatus ConfigCaptureFrequency(IDictionary<MetricType, float> metrics)
        {
            if (metrics == null)
            {
                Debug.LogError("input config metrics for capture frequency cannot be null");
                return GBStatus.GB_UNKNOWN_ERROR;
            }

            if (!APIExecutable())
                return GBStatus.GB_UNKNOWN_ERROR;
            if (SDKInitialize == false)
            {
                return GBStatus.GB_SESSION_START_BEFORE_INITITIALIZED;
            }
            foreach (KeyValuePair<MetricType, float> entry in metrics)
            {
                if (!frequencyThreshHoldList.ContainsKey(entry.Key))
                {
                    Debug.LogError("the enum of this metric doesn't exist");
                    return GBStatus.GB_UNKNOWN_ERROR;
                }
                frequencyThreshHoldList[entry.Key] = entry.Value; //only update elements which developer set
            }
            return GBStatus.GB_SUCCESS;
        }

        private GBStatus ExecutePerformanceCapture(IDictionary<MetricType, bool> metrics = null)
        {
            bool gpuEnabled = false;
            bool fpsEnabled = false;
            bool memoryEnabled = false;
            bool matricsEnable = false;

            if (metrics == null) //if no metric, by default, all of perf will trigger capturing
            {
                gpuEnabled = true;
                fpsEnabled = true;
                memoryEnabled = true;
                StartSession((fpsEnabled ? 1 : 0), (gpuEnabled ? 1 : 0), (memoryEnabled ? 1 : 0));
#if UNITY_ANDROID
                if (!Application.isEditor)
                {
                    GPUStart();
                    MemoryStart();
                }
#endif
                return GBStatus.GB_SUCCESS;
            }

            if (metrics.Count == 0) //zero element in metrics are causing returning error
            {
                Debug.LogWarning("Gamebench session cannot start because metric has no element");
                return GBStatus.GB_SESSION_WITH_NONE_METRICS;
            }

            foreach (KeyValuePair<MetricType, bool> entry in metrics)
            {
                matricsEnable &= entry.Value;
                if (entry.Key == MetricType.FPS)
                {
                    fpsEnabled = entry.Value;
                }
                else if (entry.Key == MetricType.GPU)
                {
                    gpuEnabled = entry.Value;
                }
                else if (entry.Key == MetricType.MEM)
                {
                    memoryEnabled = entry.Value;
                }
            }
            if (matricsEnable == false) //all of metrics are causing returning error
            {
                Debug.LogWarning("Gamebench session cannot start because all of elements are false");
                return GBStatus.GB_SESSION_WITH_NONE_METRICS;
            }
            else
            {
                StartSession((fpsEnabled ? 1 : 0), (gpuEnabled ? 1 : 0), (memoryEnabled ? 1 : 0));
#if UNITY_ANDROID
                if (!Application.isEditor)
                {
                    Debug.Log("start GPU capture in non-lifecycle control mode without uploading session");
                    GPUStart();
                    MemoryStart();
                }
#endif
                return GBStatus.GB_SUCCESS;
            }
        }

        public GBStatus StartPerformanceCapture(IDictionary<MetricType, bool> metrics = null)
        {
            if (!APIExecutable())
                return GBStatus.GB_UNKNOWN_ERROR;

            if (SDKInitialize == false)
            {
                return GBStatus.GB_SESSION_START_BEFORE_INITITIALIZED;
            }
            if (SessionStarted == true)
            {
                return GBStatus.GB_SESSION_ALREADY_STARTED;
            }

            return ExecutePerformanceCapture(metrics);
        }

        private GBStatus ExecuteStopPerformanceCapture()
        {
#if UNITY_ANDROID
            if (!Application.isEditor) //gpu should be stopped automatically in non-lifecycle mode
            {
                Debug.Log("stop GPU capturing in non-lifecycle control mode");
                GPUStop();
                MemoryStop();
            }
#endif
            gbsdk_sessionStop();
            SessionStarted = false;
            return GBStatus.GB_SUCCESS;
        }

        public GBStatus StopPerformanceCapture()
        {
            if (!APIExecutable())
                return GBStatus.GB_UNKNOWN_ERROR;

            if (SDKInitialize == false)
            {
                return GBStatus.GB_SESSION_STOP_AFTER_DESTROYED;
            }
            if (SessionStarted == false)
            {
                return GBStatus.GB_SESSION_ALREADY_STOPPED;
            }
            return ExecuteStopPerformanceCapture();
        }

        private void SaveIconIntoFolder()
        {
#if UNITY_ANDROID //notice iOS and android for icon path is different
            MetaDataHelper.getInstance().applyIconSave(Application.identifier, "/data/data/" + Application.identifier + "/files/sdk/");
#endif
#if UNITY_IOS
                MetaDataHelper.getInstance().applyIconSave(); //for ios the path of icon will be handled internally
#endif
        }

        private void StartSession(params int[] metricArgument)
        {
            var rawContent = Resources.Load<TextAsset>(SDKConfiguration.staticFolderName + SDKConfiguration.separator + SDKConfiguration.sdkConfigName).ToString();
            config = JsonUtility.FromJson<SDKConfiguration>(rawContent);
            gbsdk_sessionStart(config.serverendpoint,
            config.emailaddress,
            MetaDataHelper.getInstance().packageName,
            MetaDataHelper.getInstance().appname,
            MetaDataHelper.getInstance().screen_width,
            MetaDataHelper.getInstance().screen_height,
            MetaDataHelper.getInstance().gpu_vender,
            MetaDataHelper.getInstance().gpu_version,
            MetaDataHelper.getInstance().gpu_extension,
            MetaDataHelper.getInstance().gpu_render,
            MetaDataHelper.getInstance().app_version,
            MetaDataHelper.getInstance().app_versioncode,
            MetaDataHelper.getInstance().hardware,
            MetaDataHelper.getInstance().refreshrate,
            MetaDataHelper.getInstance().netOperator,
            MetaDataHelper.getInstance().simOperator,
            MetaDataHelper.getInstance().appLastUpdated,
            MetaDataHelper.getInstance().iosVersion,
            metricArgument[0],
            metricArgument[1],
            metricArgument[2]);
            SaveIconIntoFolder();
            if(metricArgument[2]==1)
            {
#if UNITY_ANDROID
                byte[] buf = new byte[500];
                string packageDirectory = MetaDataHelper.getInstance().packageName;
                int totalDataByteLength = PRE_DIRECTORY.Length + POST_DIRECTORY.Length + packageDirectory.Length + FOLDERLENGTH + 1;
                int sessionPathSize = 0;
                getCurrentFolderNumber(buf, out sessionPathSize);
                sessionPathSize = totalDataByteLength;
                byte[] result = new byte[sessionPathSize];
                for(int i=0;i<sessionPathSize;i++)
                {
                    result[i] = buf[i];
                }
                Debug.Log("package name is " + MetaDataHelper.getInstance().packageName + "/ " + MetaDataHelper.getInstance().packageName.Length);
                string sessionPathFromC = System.Text.Encoding.Default.GetString(result);
                Debug.Log("interpreted session path value from C is---: " + sessionPathFromC+" / "+sessionPathSize);
                var CopySessionPathFromC = String.Copy(sessionPathFromC);

                MetaDataHelper.getInstance().InjectSessionPathIntoJava(CopySessionPathFromC);
                MetaDataHelper.getInstance().ConfigMemoryThreadCaptureFrequency((int)frequencyThreshHoldList[MetricType.MEM]);
                MetaDataHelper.getInstance().StartMemoryThreadCapture();
#endif
            }
            SessionStarted = true;
        }

        public GBStatus CleanPerformanceData()
        {
            if (!APIExecutable())
                return GBStatus.GB_UNKNOWN_ERROR;

            if (SDKInitialize == false)
            {
                return GBStatus.GB_CLEAN_BEFORE_INITIALIZED;
            }
            if (SessionStarted == true)
            {
                return GBStatus.GB_CLEAN_DURING_SESSION;
            }
            gbsdk_cleanup();
            return GBStatus.GB_SUCCESS;
        }

        public string GetEndPointUri()
        {
            if (config == null)
            {
                Debug.LogWarning("You haven't set server configuration yet, will use default server endpoint");
                return defaultEndPointLifeCycle;
            }
            if (config.serverendpoint == null)
            {
                Debug.LogWarning("You haven't set your endpoint in configuraiton will use default server endpoint");
                return defaultEndPointLifeCycle;
            }
            if (config.serverendpoint == "")
            {
                Debug.LogWarning("You haven't set your endpoint empty, will use defaul endpoint");
                return defaultEndPointLifeCycle;
            }
            return config.serverendpoint;
        }

        public void SetEndPointUri(string serverEndpoint)
        {
            if (serverEndpoint == "" || serverEndpoint == null)
            {
                Debug.LogError("Invalid server endpoint");
                return;
            }
            config.serverendpoint = serverEndpoint;
        }

        public void SetSceneChangeAutoMarkerCreate(bool setState)
        {
            Debug.Log("set scene changed auto marker");
            sceneChangeAutoGenerateMarker = setState;

            if (sceneChangeAutoGenerateMarker)
            {
                currentScene = SceneManager.GetActiveScene();
                currentSceneName = currentScene.name;
                Debug.Log("The first scene name is " + currentSceneName);
                SceneManager.sceneLoaded += OnSceneLoaded;
                ExecuteStartMarker(currentSceneName);
            }
            else
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        //trigger unload and load scene marker
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("Unloaded Scene: " + currentSceneName + " Loaded Scene: " + scene.name);

            ExecuteStopMarker(currentSceneName);
            ExecuteStartMarker(scene.name);

            currentScene = scene;
            currentSceneName = currentScene.name;
        }

        public bool GetSceneChangeAutoMarker()
        {
            return sceneChangeAutoGenerateMarker;
        }

        public static IGamebenchSDKLifeCycle GetInstance()
        {
            return instance;
        }

        public static GamebenchSDKLifeCycle GetConcretInstance()
        {
            return instance;
        }

        private GBStatus ExecuteUploadPerforomanceData(UploadCallback callback = null)
        {
#if UNITY_ANDROID
            string path = "/data/data/" + Application.identifier + "/files/sdk/";
            Debug.Log("android folder path is: " + path);
#endif
#if UNITY_IOS
            string path = Application.temporaryCachePath + "/sdk/";
            Debug.Log("iOS folder path is: " + path);
#endif
            var zipFileList = Directory.GetFiles(path);
            List<string> zipFileUploadList = new List<string>();
            if (zipFileList.Length == 0)
                return GBStatus.GB_UPLOAD_NONE_FILES;
            for(int i=0; i<zipFileList.Length;i++)
            {
                var checkIconName = zipFileList[i].Substring(zipFileList[i].Length - ICON_FILE_LENGTH);
                if (checkIconName.Equals(ICON_FILE_NAME))
                {
                    Debug.Log("Icon file are not supposed to be handled");
                    continue;
                }
                zipFileUploadList.Add(zipFileList[i]);
            }

            StartCoroutine(BatchUpload(zipFileUploadList.ToArray(), callback));

            return GBStatus.GB_SUCCESS;
        }
         public GBStatus UploadPerformanceData(UploadCallback callback = null)
        {
            if (!APIExecutable())
                return GBStatus.GB_UNKNOWN_ERROR;

            if (SDKInitialize == false)
            {
                return GBStatus.GB_UPLOAD_BEFORE_INITIALIZED;
            }
            if (SessionStarted == true)
            {
                return GBStatus.GB_UPLOAD_DURING_SESSION;
            }
            return ExecuteUploadPerforomanceData(callback);
        }

        IEnumerator BatchUpload(string[] fileList, UploadCallback callback = null)
        {
            Debug.Log("trigger uploading all of files******************* ");
            int uploadedCount = 0;
            foreach (string filePath in fileList)
            {
                if (!File.Exists(filePath))
                    Debug.LogWarning("the session zipfile doesn't exist " + filePath);
                else if (!filePath.Contains(zipFilePrefix))
                {
                    Debug.Log("this file is not zip session file, won't be handle " + filePath);
                }
                else
                {
                    Debug.Log("the wrap up file should be " + filePath+" / "+filePath.Substring(filePath.Length - SESSION_FILE_LENGTH));
                    var metaName = filePath.Substring(filePath.Length - SESSION_FILE_LENGTH); //server use this name to pinpoint the session problem
                    var zipFile = File.ReadAllBytes(filePath);
                    List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
                    formData.Add(new MultipartFormFileSection("file", zipFile, metaName, "application/zip"));
                    UnityWebRequest www = null;
                    Debug.Log("uploading to endpoint: " + config.serverendpoint);
                    if (config.serverendpoint == "")
                        www = UnityWebRequest.Post(defaultEndPointLifeCycle, formData);
                    else
                        www = UnityWebRequest.Post(config.serverendpoint, formData);
                    yield return www.SendWebRequest();
                    if (www.isNetworkError || www.isHttpError)
                    {
                        Debug.LogWarning("error happening while uploading data to endpoint" + www.error + " / " + www.responseCode + " / " + filePath);
                        if (callback != null)
                        {
                            callback(false, www.error + " / " + www.responseCode);
                            if (www.responseCode == 500)
                            {
                                Debug.LogWarning("remove files while uploaded failed in specific error from server");
                                var fileName = Path.GetFileName(filePath);
                                gbsdk_cleanupSpecificFile(fileName, MetaDataHelper.getInstance().packageName);
                            }
                        }
                    }
                    else
                    { 
                        uploadedCount++;
                        var fileName = Path.GetFileName(filePath);
                        gbsdk_cleanupSpecificFile(fileName, MetaDataHelper.getInstance().packageName);
                        Debug.Log("Upload complete to gamebench backend and finish clean up! " + fileName+ " / "+www.isDone+" / "+www.responseCode+" / "+ uploadedCount);
                    }
                }
            }
            if (uploadedCount == fileList.Length)
            {
                if (callback != null)
                    callback(true);
            }
            else
            {
                if (callback != null)
                    callback(false, (fileList.Length - uploadedCount) + " files are uploaded failed in total " + fileList.Length + " files");
            }
        }

        void Update()
        {
            if (SDKConfiguration.getInstance().sdkAPIControlEnable == false)
                return;
            //inject matrics control over here
            UpdateMetricRuntime(Time.deltaTime);
            runningTime += Time.deltaTime;
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            if (!Application.isEditor)
            {
                if (SDKInitialize && SessionStarted)
                {
                    gbsdk_lifecycle_on_frame((uint)Math.Ceiling(fps));
#if UNITY_IOS
                    HandleiOSMemoryCapture();
#endif
                }
            }
        }

        private void HandleiOSMemoryCapture()
        {
#if UNITY_IOS
            float memoryRuntime;
            frequencyRuntime.TryGetValue(MetricType.MEM, out memoryRuntime);
            Debug.Log("memory time is: " + memoryRuntime + " / " + frequencyThreshHoldList[MetricType.MEM]);
            if (memoryRuntime > frequencyThreshHoldList[MetricType.MEM])
            {
                InjectMemoryMetric();
                frequencyRuntime[MetricType.MEM] = 0f;
            }
#endif
        }

        public void InjectMemoryMetric()
        {
#if UNITY_IOS
            var memorydata = MetaDataHelper.getInstance().getMemoryMetricFromIOS();
            if (memorydata == null)
                return;
            else
                Debug.Log("memory data is: " + memorydata[0] + " / " + memorydata[1] + " / " + memorydata[2] + " / " + memorydata[3] + " / " + System.DateTime.Now.Second);
                int totalMemory = memorydata[0];
                int nativeMemory = memorydata[1];
                int dalvikMemory = memorydata[2];
                int otherMemory = memorydata[3];
                gbsdk_lifecycle_on_memoryUsage(totalMemory, nativeMemory, dalvikMemory, otherMemory);
#endif
        }

        public void InjectFPS(uint fps) //not API, but expose to other class to use
        {
            gbsdk_lifecycle_on_frame(fps);
        }

        public void GPUStart()
        {
#if UNITY_ANDROID
            gbsdk_on_gpu("");
#endif
        }

        public void GPUStop()
        {
#if UNITY_ANDROID
            gbsdk_off_gpu();
#endif
        }

        public void MemoryStart()
        {
#if UNITY_ANDROID
            MetaDataHelper.getInstance().ResumeMemoryThreadCapture();
#endif
        }
        public void MemoryStop()
        {
#if UNITY_ANDROID
            MetaDataHelper.getInstance().PauseMemoryThreadCapture();
#endif
        }

        private GBStatus ExecuteStartMarker(string markerName)
        {
            if (markerName == "" || markerName.Length == 0)
            {
                markerName = DEFAULT_SCENE_NAME;
            }
            if (!Application.isEditor)
                gbsdk_lifecycle_punch_marker(markerName, (int)MarkerMessage.start_mark, (int)MarkerType.program);
            return GBStatus.GB_SUCCESS;
        }

        public GBStatus StartMarker(string markerName)
        {
            if (!APIExecutable())
                return GBStatus.GB_UNKNOWN_ERROR;

            if (SDKInitialize == false || SessionStarted == false)
                return GBStatus.GB_MARKER_BEFORE_SESSION;

            if (markerName == null)
            {
                Debug.LogWarning("Start marker has invalid scene name");
                return GBStatus.GB_UNKNOWN_ERROR;
            }
            return ExecuteStartMarker(markerName);
        }

        private GBStatus ExecuteStopMarker(string markerName)
        {
            if (markerName == "" || markerName.Length == 0)
            {
                markerName = DEFAULT_SCENE_NAME;
            }
            if (!Application.isEditor)
                gbsdk_lifecycle_punch_marker(markerName, (int)MarkerMessage.stop_mark, (int)MarkerType.program);
            return GBStatus.GB_SUCCESS;
        }

        public GBStatus StopMarker(string markerName)
        {
            if (!APIExecutable())
                return GBStatus.GB_UNKNOWN_ERROR;

            if (SDKInitialize == false || SessionStarted == false)
                return GBStatus.GB_MARKER_BEFORE_SESSION;

            if (markerName == null)
            {
                Debug.LogWarning("Start marker has invalid scene name");
                return GBStatus.GB_UNKNOWN_ERROR;
            }
            return ExecuteStopMarker(markerName);
        }


        public void MarkStartCurrentScene()
        {
            if (sceneChangeAutoGenerateMarker)
            {
                ExecuteStartMarker(currentScene.name);
            }
        }

        public void MarkStopCurrentScene()
        {
            if (sceneChangeAutoGenerateMarker)
            {
                ExecuteStopMarker(currentScene.name);
            }
        }

        public void CleanupSessionDataIfTimeTooShort()
        {
            Debug.Log("clean up this session since the session time is less than threshold");
            gbsdk_lifecycle_cleanup_metricsdata();
        }

        public void BindWithAutoMode(GamebenchSDKAuto binder)
        {
            autoBinder = binder;
        }

        public void UnBindWithAutoMode()
        {
            autoBinder = null;
        }

        public object GetfrequencyRunTime()
        {
            return frequencyRuntime;
        }

        public GBStatus InternalCleanupLeftOverFileInAutoMode()
        {
            var pkgName = MetaDataHelper.getInstance().packageName;
            if(pkgName != null || pkgName.Length!=0)
            {
                Debug.Log("clean up leftover file in auto mode "+pkgName);
                gbsdk_cleanup_withoutSession(pkgName);
            }
            return GBStatus.GB_SUCCESS;
        }

        public GBStatus InternalInitSDK(InternalCaller caller)
        {
            if (caller is GamebenchSDKAuto)
                return ExecuteInitSDK();
            else
                return GBStatus.GB_UNKNOWN_ERROR;
        }
        public GBStatus InternalStartPerformanceCapture(InternalCaller caller, IDictionary<MetricType, bool> metrics = null)
        {
            Debug.Log("the caller from auto is: " + caller);
            if (caller != null)
                return ExecutePerformanceCapture(metrics);
            else
                return GBStatus.GB_UNKNOWN_ERROR;
        }
        public GBStatus InternalStopPerformanceCapture(InternalCaller caller)
        {
            if (caller != null)
                return ExecuteStopPerformanceCapture();
            else
                return GBStatus.GB_UNKNOWN_ERROR;

        }
        public GBStatus InternalCleanPerformanceData(InternalCaller caller)
        {
            if (caller != null)
                return CleanPerformanceData();
            else
                return GBStatus.GB_UNKNOWN_ERROR;
        }
        public GBStatus InternalUploadPerformanceData(InternalCaller caller, UploadCallback callback = null)
        {
            if (caller != null)
                return ExecuteUploadPerforomanceData(callback);
            else
                return GBStatus.GB_UNKNOWN_ERROR;
        }
        public GBStatus InternalDestroySDK(InternalCaller caller)
        {
            if (caller != null)
                return ExecuteDestroySDK();
            else
                return GBStatus.GB_UNKNOWN_ERROR;
        }

        public GBStatus InternalConfigCaptureFrequency(IDictionary<MetricType, float> metrics, InternalCaller caller)
        {
            if (caller != null)
                return ConfigCaptureFrequency(metrics);
            else
                return GBStatus.GB_UNKNOWN_ERROR;
        }

        public void InternalSetSceneChangeAutoMarkerCreate(bool setState, InternalCaller caller)
        {
            if (caller != null)
            {
                SetSceneChangeAutoMarkerCreate(setState);
            }
        }

        //will be considered to be used in next release
         /*public GBStatus InternalStartMarker(string markerName, InternalCaller caller)
         {
             if (caller != null)
                 return ExecuteStartMarker(markerName);
             else
                 return GBStatus.GB_UNKNOWN_ERROR;
         }
         public GBStatus InternalStopMarker(string markerName, InternalCaller caller)
         {
             if (caller != null)
                 return ExecuteStopMarker(markerName);
             else
                 return GBStatus.GB_UNKNOWN_ERROR;
         }*/

        //if disable SDK at runtime, it should directly mark scene stop, otherwise, there is no pair match
        /*  private void OnDisable()
          {
              if (sceneChangeAutoGenerateMarker)
              {
                  if(currentScene!=null)
                      ExecuteStopMarker(currentScene.name);
              }
          }*/
    }
}

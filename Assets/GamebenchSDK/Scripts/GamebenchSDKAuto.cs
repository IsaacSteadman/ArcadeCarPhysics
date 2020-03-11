using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Gamebench.Sdk
{
    public class GamebenchSDKAuto : Singleton<GamebenchSDKAuto>, InternalCaller
    {
        private SDKConfiguration config;
        private Scene currentScene;
        private string currentSceneName;
        private float runningTime = 0f;
        private float deltaTime = 0.0f;
        private bool session_running = false;
        private static float THRESHOLD = 10.0f;//if timing beyond this threshold, will upload session, otherwise, the sesion will be given up while paused or killed
        private static GamebenchSDKAuto instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] //always let Lifecycle is initialization before auto module
        public static void AutoAdded()
        {
            if (SDKConfiguration.getInstance().sdkEnable == true)
            {
                if (SDKConfiguration.getInstance().sdkAPIControlEnable == false)
                {
                    Debug.Log("Auto Module is loaded");
                    instance = GamebenchSDKAuto.Instance;
                }
                else
                {
                    Debug.Log("Auto Module isn't loaded");
                    instance = null;
                }
            }
        }

        static void uninitializeSDKAPI()
        {
            Gamebench.Sdk.IGamebenchSDKLifeCycle sdk = Gamebench.Sdk.GamebenchSDKLifeCycle.GetInstance();
            if (sdk != null)
            {
                sdk.UnBindWithAutoMode();
            }
        }

        // Start is called before the first frame update
        static void initializationSDKAPI()
        {
            var sdk = Gamebench.Sdk.GamebenchSDKLifeCycle.GetConcretInstance();
            if(sdk != null)
            {
                sdk.BindWithAutoMode(instance);
                sdk.InternalInitSDK(instance);
            }
            else
            {
                Debug.LogError("The API runtime isn't intializing");
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            var sdk = Gamebench.Sdk.GamebenchSDKLifeCycle.GetConcretInstance();
        }

        void Start()
        {
            CleanUpEnvironment();
            AutoSessionStart();
            var sdk = Gamebench.Sdk.GamebenchSDKLifeCycle.GetConcretInstance();
            sdk.InternalSetSceneChangeAutoMarkerCreate(true,instance);
            session_running = true;
        }

        // Update is called once per frame
        void Update()
        {
            //auto running
            runningTime += Time.deltaTime;
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            if (!Application.isEditor)
            {
                if (session_running)
                {
                    Gamebench.Sdk.GamebenchSDKLifeCycle.GetInstance().InjectFPS((uint)Math.Ceiling(fps));
 #if UNITY_IOS
                    Gamebench.Sdk.GamebenchSDKLifeCycle.GetInstance().UpdateMetricRuntime(Time.deltaTime);
                    var memRuntime = Gamebench.Sdk.GamebenchSDKLifeCycle.GetInstance().GetfrequencyRunTime() as Dictionary<MetricType, float>;
                    if (memRuntime[MetricType.MEM] > GamebenchSDKLifeCycle.frequencyThreshHoldList[MetricType.MEM])
                    {
                        Debug.Log("memory time is in auto model: " + memRuntime + " / " + GamebenchSDKLifeCycle.frequencyThreshHoldList[MetricType.MEM]);
                        Gamebench.Sdk.GamebenchSDKLifeCycle.GetInstance().InjectMemoryMetric();
                        memRuntime[MetricType.MEM] = 0;
                    }
#endif
                }
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Debug.Log("GBSDK running on background");
                if (!Application.isEditor)
                {
                    Gamebench.Sdk.GamebenchSDKLifeCycle.GetInstance().MarkStopCurrentScene();

                    if (runningTime >= THRESHOLD)
                    {
                        Debug.Log("running time is more than threshold, upload the data "+ runningTime);
                        runningTime = 0;
                        var sdk = Gamebench.Sdk.GamebenchSDKLifeCycle.GetConcretInstance();
                        sdk.InternalStopPerformanceCapture(instance);
                        sdk.InternalUploadPerformanceData(instance,UploadCallback);
                    }
                    else
                    {
                        Debug.Log("running time is less than threshold, clean up the data " + runningTime);
                        Gamebench.Sdk.GamebenchSDKLifeCycle.GetInstance().CleanupSessionDataIfTimeTooShort(); //just give up this session if it is less than threshold
                        runningTime = 0;
                    }
                }
            }
            else
            {
                Debug.Log("GBSDK running on frontend");
            }
        }

        public void UploadCallback(bool success, string errorReason)
        {
            if (success)
            {
                Debug.Log("The session has been uploaded to endpoint successfully ");
            }
            else
            {
                Debug.LogWarning("the session is not uploaded successfully due to error " + errorReason);
            }
            if (session_running)
            {
                AutoSessionStart();
            }
        }


        private void CleanUpEnvironment()
        {
            if (!Application.isEditor)
            {
                var sdk = Gamebench.Sdk.GamebenchSDKLifeCycle.GetConcretInstance();
                sdk.InternalInitSDK(instance);
                GBStatus destroyLeftOver = sdk.InternalCleanupLeftOverFileInAutoMode();
                GBStatus destroySession = sdk.InternalDestroySDK(instance);
                Debug.Log("clean up environment in case the last session");
            }
        }

        private void AutoSessionStart()
        {
            var rawContent = Resources.Load<TextAsset>(SDKConfiguration.staticFolderName + SDKConfiguration.separator + SDKConfiguration.sdkConfigName).ToString();
            config = JsonUtility.FromJson<SDKConfiguration>(rawContent);
            if (!Application.isEditor)
            {
                var sdk = Gamebench.Sdk.GamebenchSDKLifeCycle.GetConcretInstance();
                GBStatus state = sdk.InternalStartPerformanceCapture(instance, null);
                Gamebench.Sdk.GamebenchSDKLifeCycle.GetInstance().MarkStartCurrentScene();
                Debug.Log("the performance of capturing state is: " + state);
#if UNITY_ANDROID //notice iOS and android for icon path is different
                MetaDataHelper.getInstance().applyIconSave(Application.identifier, "/data/data/" + Application.identifier + "/files/sdk/");
#endif
#if UNITY_IOS
                MetaDataHelper.getInstance().applyIconSave(); //for ios the path of icon will be handled internally
#endif
            }
            Debug.Log("Finished gbsdk session start in auto mode");
        }
    }
}

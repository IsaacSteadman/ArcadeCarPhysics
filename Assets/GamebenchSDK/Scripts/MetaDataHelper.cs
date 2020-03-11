using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_2018_OR_NEWER
using System.Threading.Tasks;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Networking;
using Gamebench.Sdk;


namespace Gamebench.Sdk
{
    //for Android and iOS meta data helper
    public class MetaDataHelper
    {
        private static MetaDataHelper dataHelper;
#if UNITY_ANDROID
        private AndroidJavaClass TelephonyInfo = null;
        private AndroidJavaClass PackageInfo = null;
        private AndroidJavaClass GPUInfo = null;
        private AndroidJavaClass MemoryInfo = null;

        private AndroidJavaObject context;
#endif

        public MetaDataHelper()
        {
            if (!Application.isEditor)
            {
#if UNITY_ANDROID
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                context = activity.Call<AndroidJavaObject>("getApplicationContext");
                PackageInfo = new AndroidJavaClass("com.gamebenchsdk.communicatelibrary.PackageInformation");
#endif
#if UNITY_IOS
            SDKMetaDataFacade.StartMetaHelper();
#endif
            }

        }

        public static MetaDataHelper getInstance()
        {
            if (null == dataHelper)
            {
                dataHelper = new MetaDataHelper();
            }
            return dataHelper;
        }

        public string packageName { get; set; }
        public string appname { get; set; }

        public int screen_width { get; set; }
        public int screen_height { get; set; }
        public int refreshrate { get; set; }

        public string gpu_vender { get; set; }
        public string gpu_version { get; set; }
        public string gpu_extension { get; set; }
        public string gpu_render { get; set; }

        public string app_version { get; set; }
        public int app_versioncode { get; set; }
        public string appLastUpdated { get; set; }

        public string hardware { get; set; }

        public string netOperator { get; set; }
        public string simOperator { get; set; }
        public string iosVersion { get; set; }

        public void applyIconSave(string packageName =null, string path=null)
        {
#if UNITY_ANDROID
            PackageInfo.CallStatic("SaveTheLogo", new object[] { packageName, path });
#endif
#if UNITY_IOS
            SDKMetaDataFacade.SaveIcon();
#endif
        }

        public void StartMemoryThreadCapture()
        {
#if UNITY_ANDROID
            if (MemoryInfo == null)
            {
                Debug.LogError("there is no memory info provider from java native");
                return;
            }
            Debug.Log("start memory thread capture");
            MemoryInfo.CallStatic("RunMemroyCaptureThread");
#endif
        }

        public void PauseMemoryThreadCapture()
        {
#if UNITY_ANDROID
        if (MemoryInfo == null)
        {
            Debug.LogError("there is no memory info provider from java native");
            return;
        }
        MemoryInfo.CallStatic("PauseMemoryCaptureThread");
#endif
        }

        public void ResumeMemoryThreadCapture()
        {
#if UNITY_ANDROID
            if (MemoryInfo == null)
            {
                Debug.LogError("there is no memory info provider from java native");
                return;
            }
            MemoryInfo.CallStatic("ResumeMemoryCaptureThread");
#endif
        }

        public void ConfigMemoryThreadCaptureFrequency(int internval)
        {
        #if UNITY_ANDROID
            MemoryInfo.CallStatic("ConfigCaptureInterval", new object[] { internval });
        #endif    
        }

        public void StopMemoryThreadCapture()
        {
#if UNITY_ANDROID
            if (MemoryInfo == null)
            {
                Debug.LogError("there is no memory info provider from java native");
                return;
            }
            MemoryInfo.CallStatic("StopMemoryCaptureThread");
#endif
        }

        public void InjectSessionPathIntoJava(string sessionPath)
        {
            Debug.Log("session path is: "+sessionPath);
        #if UNITY_ANDROID
            MemoryInfo.CallStatic("SaveSessionPath", new object[] { sessionPath });
        #endif
        }

        public int[] getMemoryMetricFromIOS()
        {
#if UNITY_IOS
            int[] memorydata = new int[4];
            float iosRSS = SDKMetaDataFacade.GetMemoryUsage();
            if (iosRSS < 0)
            {
                Debug.LogError("cannot get memory metric");
                return null;
            }
            memorydata[0] = (int)iosRSS;
            memorydata[1] = 0;
            memorydata[2] = 0;
            memorydata[3] = 0;
           return memorydata;
#endif
#if UNITY_ANDROID
            Debug.LogError("this function cannot be called in Android");
            return null;
#endif
        }

        public void apply()
        {

#if UNITY_ANDROID
            MemoryInfo = new AndroidJavaClass("com.gamebenchsdk.communicatelibrary.AndroidMemoryCapture");
            MemoryInfo.CallStatic("setContext", new object[] { context });
            TelephonyInfo = new AndroidJavaClass("com.gamebenchsdk.communicatelibrary.TelephonyInformation");
            TelephonyInfo.CallStatic("setContext", new object[] { context });
            PackageInfo.CallStatic("setContext", new object[] { context });
            GPUInfo = new AndroidJavaClass("com.gamebenchsdk.communicatelibrary.GPUInformation");
            GPUInfo.CallStatic("setContext", new object[] { context });
            gpu_extension = GPUInfo.CallStatic<string>("GetGPUExtensions");
            netOperator = TelephonyInfo.CallStatic<string>("GetNetworkOperator");
            simOperator = TelephonyInfo.CallStatic<string>("GetSimOperator");
            appLastUpdated = PackageInfo.CallStatic<string>("GetLastUpdated", new object[] { Application.identifier });
#endif

#if UNITY_IOS
            gpu_extension = SDKMetaDataFacade.GetGPUExtension();
            netOperator = SDKMetaDataFacade.GetNetWork();
            simOperator = SDKMetaDataFacade.GetCarrierName();
            var lastUpdateDate = SDKMetaDataFacade.GetLastUpdatedDate();
            if (lastUpdateDate == null)
                appLastUpdated = "";
            else
                appLastUpdated = SDKMetaDataFacade.GetLastUpdatedDate();
            iosVersion = SDKMetaDataFacade.GetIOSVersion();
#endif
            packageName = Application.identifier;
            appname = Application.productName;
            screen_width = Screen.width;
            screen_height = Screen.height;
            refreshrate = Screen.currentResolution.refreshRate;
            hardware = SystemInfo.deviceName;
            app_version = Application.version;
            app_versioncode = 1;
            // app_versioncode = PlayerSettings.Android.bundleVersionCode;
            gpu_vender = SystemInfo.graphicsDeviceVendor;
            gpu_version = SystemInfo.graphicsDeviceVersion;
            gpu_render = SystemInfo.graphicsDeviceName;//need to double confirm thi
        }
    }

}


#if UNITY_IOS
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace Gamebench.Sdk
{
    public class SDKMetaDataFacade
    {

        [DllImport("__Internal")]
        private static extern void _StartMetaHelper();

        [DllImport("__Internal")]
        private static extern string _OCCarrierName();

        [DllImport("__Internal")]
        private static extern string _OCGetIOSVersion();

        [DllImport("__Internal")]
        private static extern string _OCNetworkCode();

        [DllImport("__Internal")]
        private static extern string _OCLastUpdatedDate();

        [DllImport("__Internal")]
        private static extern string _OCGetVersion();

        [DllImport("__Internal")]
        private static extern string _OCGetGPUExtension();

        [DllImport("__Internal")]
        private static extern void _SaveIconLocally();

        [DllImport("__Internal")]
        private static extern float _OCMemoryUsage();

        public static string IN_EDITOR = "In editor running";


        public static void StartMetaHelper()
        {
            if (!Application.isEditor)
                _StartMetaHelper();
        }

        public static void SaveIcon()
        {
            _SaveIconLocally();
        }

        public static float GetMemoryUsage()
        {
            if(Application.platform!=RuntimePlatform.OSXEditor)
            {
                return _OCMemoryUsage();
            }
            return -1;
        }

        public static string GetCarrierName()
        {
            if(Application.platform!=RuntimePlatform.OSXEditor)
            {
                return _OCCarrierName();
            }
            return IN_EDITOR;
        }

        public static string GetIOSVersion()
        {
            if(Application.platform!=RuntimePlatform.OSXEditor)
            {
                return _OCGetIOSVersion();
            }
            return IN_EDITOR;
        }

        public static string GetNetWork()
        {
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                return _OCNetworkCode();
            }
            return IN_EDITOR;
        }

        public static string GetLastUpdatedDate()
        {
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                return _OCLastUpdatedDate();
            }
            return IN_EDITOR;
        }

        public static string GetVersion()
        {
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                return _OCGetVersion();
            }
            return IN_EDITOR;
        }

        public static string GetGPUExtension()
        {
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                return _OCGetGPUExtension();
            }
            return IN_EDITOR;
        }
    }
}
#endif

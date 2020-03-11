using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.IO;
using System;

namespace Gamebench.Sdk
{
    public sealed class SDKConfiguration
   {
        //if test only purpose for staging
        //public string serverendpoint = "https://web.staging.gamebench.net/v1/sessions/sync";
        public string serverendpoint = "https://web.gamebench.net/v1/sessions/sync"; //by default endpoint
        public string emailaddress = "testing@gamebench.net";
        public bool needInstallation = true;
        public bool installationPrompted = false;
        public int useFPS = 1;
        public int usePower = 0;
        public bool sdkEnable = true;
        public bool sdkAPIControlEnable = false;
        //private string dataPath = Path.Combine(Application.persistentDataPath, "SDKConfig.txt");
        private string sourceFilePath = "Assets/Resources";
        public static string staticFolderName = "GameBenchSDK";
        public static string fileName = "SDKConfig.txt";
        public static string sdkConfigName = "SDKConfig";
        public static string separator = "/";

#if UNITY_EDITOR_WIN
	    private static string sdkConfigPath = "Assets\\Resources\\GameBenchSDK\\SDKConfig.txt";
#else
        private static string sdkConfigPath = "Assets/Resources/GameBenchSDK/SDKConfig.txt";
#endif

        private static SDKConfiguration instance= null;
        public static SDKConfiguration getInstance(){
            if(instance ==null){
                Debug.Log("Get Instance New");
                TextAsset textAsset = Resources.Load<TextAsset>("GameBenchSDK/SDKConfig");
                if (textAsset != null)
                {
                    string IJson = textAsset.text;
                    Debug.Log("reading the data as following: " + IJson);
                    instance = JsonUtility.FromJson<SDKConfiguration>(IJson);
                }
                else
                {
                    Debug.Log("The SDK Configuraiton is not initialized");
                }
                //  EnableSDKInCompilation();
                //  EnableLifeCycleCompilation();
            }
            return instance;
        }

        private static void EnableSDKInCompilation()
        {
#if UNITY_EDITOR
    #if UNITY_ANDROID
            if ((instance.sdkEnable))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android) + ";gamebenchSDKEnabled");
            }
            else
            {
                string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
                defines = defines.Replace(";gamebenchSDKEnabled", "");
                defines = defines.Replace("gamebenchSDKEnabled", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defines);
            }
    #endif
    #if UNITY_IOS
            if ((instance.sdkEnable))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS) + ";gamebenchSDKEnabled");
            }
            else
            {
                string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
                defines = defines.Replace(";gamebenchSDKEnabled", "");
                defines = defines.Replace("gamebenchSDKEnabled", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, defines);
            }
    #endif
#endif
        }

        private static void EnableLifeCycleCompilation()
        {
#if UNITY_EDITOR
    #if UNITY_ANDROID
            if ((instance.sdkAPIControlEnable))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android) + ";SDKAPIEnabled");
            }
            else
            {
                string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
                defines = defines.Replace(";SDKAPIEnabled", "");
                defines = defines.Replace("SDKAPIEnabled", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defines);
            }
    #endif
    #if UNITY_IOS
            if ((instance.sdkAPIControlEnable))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS) + ";SDKAPIEnabled");
            }
            else
            {
                string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
                defines = defines.Replace(";SDKAPIEnabled", "");
                defines = defines.Replace("SDKAPIEnabled", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, defines);
            }
    #endif
#endif
        }

        public SDKConfiguration(){
        }

        public void apply()
        {
            needInstallation = false;
            try
			{
                FileInfo fi = new FileInfo(sdkConfigPath);
                if (!Directory.Exists(fi.DirectoryName))
                {
                    System.IO.Directory.CreateDirectory(fi.DirectoryName);
                }

                Debug.Log("Generate SDK Config file at " + sdkConfigPath);
                string jsonString = JsonUtility.ToJson(this);
                using (StreamWriter streamWriter = File.CreateText(sdkConfigPath))
                {
                    streamWriter.Write(jsonString);
                }
               
            }
            catch(Exception ex)
            {
                Debug.LogError("what is the error is: " + ex);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor;
using System;

//Unity UI implementation in IDE
namespace Gamebench.Sdk.UI
{
    public class GamebenchSDKEditor : EditorWindow
    {
        private Texture logo;
        private GUIStyle styleFoldout;
        private Vector2 scrollPosition;
        internal const int HEIGHT_SEPARATOR = 20;
        private bool foldoutAnalytics = true;
        private bool foldoutNotifications = true;
        internal const int WIDTH_BUTTON = 80;
        private SDKConfiguration config;


        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            SDKConfiguration conf = SDKConfiguration.getInstance();
            if (conf == null)
                return;
            if ((conf.installationPrompted == false) && (conf.needInstallation == true))
            {
                conf.installationPrompted = true;
                if (EditorUtility.DisplayDialog("Gamebench SDK", "Welcome to Gamebench SDK, Please configure your account and backend ", "open", "cancel"))
                {
                    EditorApplication.ExecuteMenuItem("GamebenchSDK/Configure");
                }

                Debug.Log("Checking scripts reloaded");
            }
        }


        void OnEnable()
        {
            titleContent = new GUIContent(
                "Configuration",
                AssetDatabase.LoadAssetAtPath<Texture>("Assets/GamebenchSDK/Editor/Resource/logo.png"));

            if (config == null)
                return;
            if ((config.installationPrompted == false) && (config.needInstallation == true))
            {
                config.installationPrompted = true;
                if (EditorUtility.DisplayDialog("Gamebench SDK", "Welcome to Gamebench SDK, Please configure your account and backend ", "open", "cancel"))
                {
                    EditorApplication.ExecuteMenuItem("GamebenchSDK/Configure");
                }
                Debug.Log("Checking on Enable");
            }
        }

        private static bool CreateFoldout(
          bool foldout,
          string content,
          bool toggleOnLabelClick,
          GUIStyle style)
        {

#if UNITY_5_5_OR_NEWER
            return EditorGUILayout.Foldout(foldout, content, toggleOnLabelClick, style);
#else
            return EditorGUILayout.Foldout(foldout, content, style);
#endif
        }

        void OnGUI()
        {
            config = SDKConfiguration.getInstance();
            if (logo == null) logo = AssetDatabase.LoadAssetAtPath<Texture>("Assets/GamebenchSDK/Editor/Resource/logo.png");
            if (styleFoldout == null) styleFoldout = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            GUILayout.Label(logo, GUILayout.ExpandWidth(false));
            GUILayout.Space(HEIGHT_SEPARATOR);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foldoutAnalytics = CreateFoldout(foldoutAnalytics, "SDK Configuration", true, styleFoldout);

            if (foldoutAnalytics)
            {
                GUILayout.Label("Version: 0.4.0", EditorStyles.boldLabel); //in future, this should be injected from CI
                GUILayout.Label("Core settings", EditorStyles.boldLabel);
                config.serverendpoint = EditorGUILayout.TextField(new GUIContent("Gamebench Backend URL", "Your developer backend Server URL"), config.serverendpoint);
                config.emailaddress = EditorGUILayout.TextField(new GUIContent("Email Address", "Your Email address"), config.emailaddress);
                GUILayout.Label("Activate", EditorStyles.boldLabel);
                config.sdkEnable = GUILayout.Toggle(config.sdkEnable, new GUIContent("Enable Gamebench SDK", "set to integrate SDK"));
                if(config.sdkEnable == true) //set to enable only if sdk enable
                {
                    //temporary close this functionality in v0.4, will be release in v0.5
                    //config.sdkAPIControlEnable = GUILayout.Toggle(config.sdkAPIControlEnable, new GUIContent("Manual control of SDK Life-cycle", "Manual configuration of the GameBench SDK allows to control life-cycle usage of the SDK via API"));
                }
            }

            GUILayout.Space(HEIGHT_SEPARATOR);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", GUILayout.Width(WIDTH_BUTTON)))
                Apply();
            GUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            GUIUtility.ExitGUI();
        }

        private void Apply()
        {
            config.apply();
            AssetDatabase.Refresh();
            Debug.Log("apply for that configuration");
        }
    }
}

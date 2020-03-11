using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace Gamebench.Sdk.UI
{
    public sealed class MenuItems : MonoBehaviour
    {
        static MenuItems()
        {
        }

        internal const string MENU_PATH = "GamebenchSDK";

        internal static string AndroidSdkLocation
        {
            get { return EditorPrefs.GetString("AndroidSdkRoot"); }
        }

        [MenuItem(MENU_PATH + "/Configure", priority = 1)]
        public static void ConfigureSdk()
        {
            WindowHelper.Show<GamebenchSDKEditor>("Configuration");
        }
    }
}

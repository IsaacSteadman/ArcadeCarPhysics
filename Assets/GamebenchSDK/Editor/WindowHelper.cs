using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gamebench.Sdk.UI
{
    internal sealed class WindowHelper
    {

        internal static void Show<T>(string title) where T : EditorWindow
        {
            System.Type inspectorType = typeof(UnityEditor.Editor)
                .Assembly
                .GetType("UnityEditor.InspectorWindow");

            var foundInspector = false;
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType() == inspectorType)
                {
                    foundInspector = true;
                    break;
                }
            }

            if (foundInspector)
            {
                EditorWindow.GetWindow<T>(
                    title,
                    true,
                    inspectorType)
                    .Show();
            }
            else
            {
                EditorWindow.GetWindow<T>(
                    title,
                    true)
                    .Show();
            }
        }
    }

}

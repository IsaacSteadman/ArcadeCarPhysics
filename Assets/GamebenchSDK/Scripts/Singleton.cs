using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//derive from DetaDNA, easier to detect singleton destory issue while playing in IDE
namespace Gamebench.Sdk
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        private static object _lock = new object();

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    Debug.Log("already destroyed on application quit");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));

                        if (FindObjectsOfType(typeof(T)).Length > 1)
                        {
                            Debug.Log("[Singleton] Something went really wrong " +
                                           " - there should never be more than 1 singleton!" +
                                           " Reopening the scene might fix it.");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = typeof(T).ToString();

#if UNITY_EDITOR
                            if (Application.isPlaying)
                            { // avoid test errors
#endif
                                DontDestroyOnLoad(singleton);
#if UNITY_EDITOR
                            }
#endif
                        }
                    }

                    return _instance;
                }
            }
        }

        private static bool applicationIsQuitting = false;
        public virtual void OnDestroy()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            { // avoid test errors
#endif
                applicationIsQuitting = true;
#if UNITY_EDITOR
            }
#endif
        }
    }

}


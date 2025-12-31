using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using UnityEngine;

namespace MegabonkTogether.Helpers
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;

        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("CoroutineRunner");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CoroutineRunner>();
                }
                return _instance;
            }
        }


        public Coroutine Run(IEnumerator routine)
        {
            return MonoBehaviourExtensions.StartCoroutine(this, routine);
        }

        public void Stop(Coroutine coroutine)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
    }
}

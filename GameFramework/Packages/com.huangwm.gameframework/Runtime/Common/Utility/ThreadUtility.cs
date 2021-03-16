using System.Threading;
using UnityEngine;

namespace GF.Common.Utility
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class ThreadUtility
    {
        private static Thread ms_MainThread = Thread.CurrentThread;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void _ForInitialize()
        {
        }

        public static bool IsMainThread()
        {
            return Thread.CurrentThread == ms_MainThread;
        }
    }
}
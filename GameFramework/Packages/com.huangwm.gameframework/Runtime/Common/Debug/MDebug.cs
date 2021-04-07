#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Profiling;


namespace GF.Common.Debug
{
    /// <summary>
    /// My Debug. 为了不跟<see cref="UnityEngine.Debug"/>冲突
    /// </summary>
    /// 
    public static class MDebug
    {
        public const string LOG_VERBOSE_EDITOR_CONDITIONAL = "GF_LOG_EDITOR_V";
        public const string LOG_EDITOR_CONDITIONAL = "GF_LOG_EDITOR_L";
        public const string LOG_WARNING_EDITOR_CONDITIONAL = "GF_LOG_EDITOR_W";
        public const string LOG_ERROR_EDITOR_CONDITIONAL = "GF_LOG_EDITOR_E";
        public const string LOG_ASSERT_EDITOR_CONDITIONAL = "GF_LOG_EDITOR_A";

        public const string LOG_VERBOSE_BUILT_CONDITIONAL = "GF_LOG_BUILT_V";
        public const string LOG_BUILT_CONDITIONAL = "GF_LOG_BUILT_L";
        public const string LOG_WARNING_BUILT_CONDITIONAL = "GF_LOG_BUILT_W";
        public const string LOG_ERROR_BUILT_CONDITIONAL = "GF_LOG_BUILT_E";
        public const string LOG_ASSERT_BUILT_CONDITIONAL = "GF_LOG_BUILT_A";

#if UNITY_EDITOR
        public const string LOG_VERBOSE_CONDITIONAL = LOG_VERBOSE_EDITOR_CONDITIONAL;
        public const string LOG_CONDITIONAL = LOG_EDITOR_CONDITIONAL;
        public const string LOG_WARNING_CONDITIONAL = LOG_WARNING_EDITOR_CONDITIONAL;
        public const string LOG_ERROR_CONDITIONAL = LOG_ERROR_EDITOR_CONDITIONAL;
        public const string LOG_ASSERT_CONDITIONAL = LOG_ASSERT_EDITOR_CONDITIONAL;
#else
		public const string LOG_VERBOSE_CONDITIONAL = LOG_VERBOSE_BUILT_CONDITIONAL;
		public const string LOG_CONDITIONAL = LOG_BUILT_CONDITIONAL;
		public const string LOG_WARNING_CONDITIONAL = LOG_WARNING_BUILT_CONDITIONAL;
		public const string LOG_ERROR_CONDITIONAL = LOG_ERROR_BUILT_CONDITIONAL;
		public const string LOG_ASSERT_CONDITIONAL = LOG_ASSERT_BUILT_CONDITIONAL;
#endif

        private const string DEFAULT_TAG = "Default";
        /// <summary>
        /// Log的分隔符
        /// {LOG_SPLIT1}Tag{LOG_SPLIT2}Text
        /// </summary>
        private const string LOG_SPLIT1 = "🐷";
        /// <summary>
        /// <see cref="LOG_SPLIT1"/>
        /// </summary>
        private const string LOG_SPLIT2 = " | ";

        private const double BYTES_TO_MBYTES = 1.0 / 1024.0 / 1024.0;

        /// <summary>
        /// 只能在这个类中使用
        /// </summary>
        private static System.Text.StringBuilder ms_LogCache;

        /// <summary>
        /// 相当于<see cref="Time.realtimeSinceStartup"/>
        /// 但是编辑器下也能使用
        /// </summary>
        private static System.Diagnostics.Stopwatch ms_Stopwatch;

        static MDebug()
        {
            ms_LogCache = new System.Text.StringBuilder();

            ms_Stopwatch = new System.Diagnostics.Stopwatch();
            ms_Stopwatch.Start();
        }

        #region Log
        public static string FormatPathToHyperLink(string path)
        {
            return $"<a path=\"{path}\">{path}</a>";
        }

        public static string FormatDirectoryToHyperLink(string directory)
        {
            return $"<a directory=\"{directory}\">{directory}</a>";
        }

        public static string FormatLog(string tag, string message)
        {
            return string.Format("{0}{1}{2}{3}", LOG_SPLIT1, tag, LOG_SPLIT2, message);
        }

        public static void ParserLog(string log, out string tag, out string message)
        {
            if (log.StartsWith(LOG_SPLIT1))
            {
                int split2Index = log.IndexOf(LOG_SPLIT2);
                if (split2Index > 0)
                {
                    tag = log.Substring(LOG_SPLIT1.Length, split2Index - LOG_SPLIT1.Length);
                    message = log.Substring(split2Index + 1);
                }
                else
                {
                    tag = DEFAULT_TAG;
                    message = log;
                }
            }
            else
            {
                tag = DEFAULT_TAG;
                message = log;
            }
        }

        [System.Diagnostics.Conditional(LOG_VERBOSE_CONDITIONAL)]
        public static void LogVerbose(string tag, string message)
        {
            string text = FormatLog(tag, message);
            UnityEngine.Debug.Log(text);
        }

        [System.Diagnostics.Conditional(LOG_VERBOSE_CONDITIONAL)]
        public static void LogVerbose(string tag, string message, Object context)
        {
            string text = FormatLog(tag, message);
            UnityEngine.Debug.Log(text, context);
        }

        [System.Diagnostics.Conditional(LOG_CONDITIONAL)]
        public static void Log(string tag, string message)
        {
            string text = FormatLog(tag, message);
            UnityEngine.Debug.Log(text);
        }

        [System.Diagnostics.Conditional(LOG_CONDITIONAL)]
        public static void Log(string tag, string message, Object context)
        {
            string text = FormatLog(tag, message);
            UnityEngine.Debug.Log(text, context);
        }

        [System.Diagnostics.Conditional(LOG_CONDITIONAL)]
        public static void LogFormat(string tag, string message, params object[] args)
        {
            message = string.Format(message, args);
            string text = FormatLog(tag, message);
            UnityEngine.Debug.Log(text);
        }

        [System.Diagnostics.Conditional(LOG_WARNING_CONDITIONAL)]
        public static void LogWarning(string tag, string message)
        {
            string text = FormatLog(tag, message);
            UnityEngine.Debug.LogWarning(text);
        }

        [System.Diagnostics.Conditional(LOG_WARNING_CONDITIONAL)]
        public static void LogWarning(string tag, string message, Object context)
        {
            string text = FormatLog(tag, message);
            UnityEngine.Debug.LogWarning(text, context);
        }

        [System.Diagnostics.Conditional(LOG_ERROR_CONDITIONAL)]
        public static void LogError(string tag, string message)
        {
            string text = FormatLog(tag, message);
            UnityEngine.Debug.LogError(text);
        }

        [System.Diagnostics.Conditional(LOG_ERROR_CONDITIONAL)]
        public static void LogError(string tag, string message, Object context)
        {
            string text = FormatLog(tag, message);
            UnityEngine.Debug.LogError(text, context);
        }

        [System.Diagnostics.Conditional(LOG_CONDITIONAL)]
        public static void LogErrorFormat(string tag, string message, params object[] args)
        {
            message = string.Format(message, args);
            string text = FormatLog(tag, message);
            UnityEngine.Debug.LogError(text);
        }

        [System.Diagnostics.Conditional(LOG_ASSERT_CONDITIONAL)]
        public static void Assert(bool condition, string message, bool displayDialog = true)
        {
            if (!condition)
            {
                UnityEngine.Debug.Assert(condition, message);
                InternalAssert(message, displayDialog);
            }
        }

        [System.Diagnostics.Conditional(LOG_ASSERT_CONDITIONAL)]
        public static void Assert(bool condition, string message, Object context, bool displayDialog = true)
        {
            if (!condition)
            {
                UnityEngine.Debug.Assert(condition, message, context);
                InternalAssert(message, displayDialog);
            }
        }

        [System.Diagnostics.Conditional(LOG_ASSERT_CONDITIONAL)]
        public static void Assert(bool condition, string tag, string message, bool displayDialog = true)
        {
            if (condition)
            {
                return;
            }

            Assert(condition, FormatLog(tag, message), displayDialog);
        }

        [System.Diagnostics.Conditional(LOG_ASSERT_CONDITIONAL)]
        public static void Assert(bool condition, string tag, string message, Object context, bool displayDialog = true)
        {
            if (condition)
            {
                return;
            }

            Assert(condition, FormatLog(tag, message), context, displayDialog);
        }

        /// <param name="forceCollection">true:强制收集，可能会导致卡顿</param>
        public static void LogMemory(string tag, string message, bool forceCollection)
        {
            Log(tag, GenerateMemoryLog(message, forceCollection));
        }

        /// <param name="forceCollection">true:强制收集，可能会导致卡顿</param>
        public static void LogVerboseMemory(string tag, string message, bool forceCollection)
        {
            LogVerbose(tag, GenerateMemoryLog(message, forceCollection));
        }

        /// <summary>
        /// 生成内存Log
        /// </summary>
        /// <param name="forceCollection">true:强制收集，可能会导致卡顿</param>
        public static string GenerateMemoryLog(string message, bool forceCollection)
        {
            return ms_LogCache.Clear()
                .Append($"TotalMemory: {System.GC.GetTotalMemory(forceCollection) * BYTES_TO_MBYTES:F2}MB, ")
                .Append($"MaxUsed: {Profiler.maxUsedMemory * BYTES_TO_MBYTES:F2}MB, ")
                .Append($"Graphics: {Profiler.GetAllocatedMemoryForGraphicsDriver() * BYTES_TO_MBYTES:F2}MB, ")
                .Append($"MonoHeap: {Profiler.GetMonoHeapSizeLong() * BYTES_TO_MBYTES:F2}MB, ")
                .Append($"MonoUsed: {Profiler.GetMonoUsedSizeLong() * BYTES_TO_MBYTES:F2}MB, ")
                .Append($"TempAlloc: {Profiler.GetTempAllocatorSize() * BYTES_TO_MBYTES:F2}MB, ")
                .Append($"TotalAlloc: {Profiler.GetTotalAllocatedMemoryLong() * BYTES_TO_MBYTES:F2}MB, ")
                .Append($"TotalUnusedReserved: {Profiler.GetTotalUnusedReservedMemoryLong() * BYTES_TO_MBYTES:F2}MB.")
                .Append(" ").Append(message)
                .ToString();
        }
        #endregion

        #region Time
        /// <summary>
        /// <see cref="Time.realtimeSinceStartup"/>
        /// </summary>
        public static long GetMillisecondsSinceStartup()
        {
            return ms_Stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// <see cref="Time.realtimeSinceStartup"/>
        /// </summary>
        public static long GetTicksSinceStartup()
        {
            return ms_Stopwatch.ElapsedTicks;
        }

        /// <summary>
        /// Tick to FPS
        /// </summary>
        public static float ConvertTicksToFPS(long ticks)
        {
            return 10000000.0f / ticks;
        }

        /// <summary>
        /// Tick to FPS
        /// </summary>
        public static float ConvertTicksToMilliseconds(long ticks)
        {
            return ticks * 0.0001f;
        }

        /// <summary>
        /// Convert to *h *m *s
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        public static string FormatMilliseconds(long milliseconds)
        {
            long totalS = (long)(milliseconds / 1000.0);
            long h = (long)(totalS / 3600.0);
            long m = (long)((totalS - h * 3600) / 60.0);
            long s = totalS - h * 3600 - m * 60;
            return $"({h}h {m}m {s}s)";
        }
        #endregion

        [System.Diagnostics.Conditional(LOG_ASSERT_CONDITIONAL)]
        private static void InternalAssert(string message, bool displayDialog)
        {
#if UNITY_EDITOR
            if (displayDialog)
            {
                EditorUtility.DisplayDialog("Assert Failed", message, "OK");
            }
#endif
            UnityEngine.Debug.Break();
        }
    }
}
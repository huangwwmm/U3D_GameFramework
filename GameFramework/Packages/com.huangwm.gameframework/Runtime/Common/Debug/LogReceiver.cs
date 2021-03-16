using GF.Common.Utility;
using System;
using System.Threading;
using UnityEngine;

namespace GF.Common.Debug
{
#if UNITY_EDITOR
	[UnityEditor.InitializeOnLoad]
#endif
	public static class LogReceiver
	{
		private static bool ms_EnableLogRecord;
		private static bool ms_EnableLogItems;
		private static int ms_LastFrameCount;
		private static object ms_LockObject;

		static LogReceiver()
		{
#if GF_DEBUG
            ms_EnableLogItems = true;
            ms_EnableLogRecord = true;
#else
            ms_EnableLogItems = false;
            ms_EnableLogRecord = false;
#endif

            // 这里是主线程。Unity的API不能在子线程里调用，所以需要提前在这里初始化LogRecord和LogItems
            if (ms_EnableLogRecord)
            {
                LogRecord._ForInItialize();
            }

            if (ms_EnableLogItems)
            {
                LogItems.GetInstance();
            }

			ms_LastFrameCount = Time.frameCount;
			ms_LockObject = new object();

			Application.logMessageReceivedThreaded -= OnLogReceived;
			if (ms_EnableLogRecord
				|| ms_EnableLogItems)
			{
                ms_LastFrameCount = Time.frameCount;
                ms_LockObject = new object();
                Application.logMessageReceivedThreaded += OnLogReceived;
			}
		}

		public static bool IsEnableLogRecord()
		{
			return ms_EnableLogRecord;
		}

		public static bool IsEnableLogItems()
		{
			return ms_EnableLogItems;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		internal static void _ForInitialize()
		{
		}

		private static void OnLogReceived(string condition, string stackTrace, LogType type)
		{
			int frameCount = ms_LastFrameCount;
			if (ThreadUtility.IsMainThread())
			{
				try
				{
					frameCount = Time.frameCount;
				}
				catch (Exception)
				{
					// get_frameCount is not allowed to be called during serialization, call it from OnEnable instead. 
					// 拿不到就不拿，不需要ErrorHandle
				}
				ms_LastFrameCount = frameCount;
			}

            MDebug.ParserLog(condition, out string tag, out string message);
			LogItem logItem = new LogItem(type, DateTime.Now, tag, message, stackTrace, frameCount);

			lock (ms_LockObject)
			{
				if (ms_EnableLogRecord)
				{
					LogRecord._OnLogReceived(logItem);
				}

				if (ms_EnableLogItems)
				{
					LogItems.GetInstance()._OnLogReceived(logItem);
				}
			}
		}
	}
}
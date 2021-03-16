using System;
using System.IO;
using UnityEngine;

namespace GF.Common.Debug
{
	public static class LogRecord
	{
		private static readonly string LOGRECORD_DIRECTORY = string.Format("{0}\\Unity_LogRecord\\", Application.temporaryCachePath);

		private static StreamWriter ms_StreamWriter;
		private static string ms_LogRecordPath;

		static LogRecord()
		{
			CreateNewLogFile();			
		}

#if UNITY_EDITOR
        [CTMenuItem("调试/打开Log文件")]
		public static void OpenLogFile()
		{
            UnityEngine.Debug.Log("LogRecord path: " + ms_LogRecordPath);
			UnityEditor.EditorUtility.RevealInFinder(ms_LogRecordPath);
			UnityEditor.EditorUtility.OpenWithDefaultApp(ms_LogRecordPath);
		}
#endif

		public static string GetLogRecordPath()
		{
			return ms_LogRecordPath;
		}

		public static void CreateNewLogFile()
        {            
            try
			{
				if (ms_StreamWriter != null)
				{
					ms_StreamWriter.Close();
					ms_StreamWriter = null;
				}

				ms_LogRecordPath = string.Format("{0}{1}.log", LOGRECORD_DIRECTORY, DateTime.Now.ToString("yyyy-M-dd--HH-mm-ss"));
                UnityEngine.Debug.Log("LogRecord path: " + ms_LogRecordPath);
				if (!Directory.Exists(LOGRECORD_DIRECTORY))
				{
					Directory.CreateDirectory(LOGRECORD_DIRECTORY);
				}
				ms_StreamWriter = new StreamWriter(ms_LogRecordPath
					, false
					, System.Text.Encoding.UTF8);
				ms_StreamWriter.AutoFlush = true;
			}
			catch (Exception e)
			{
                UnityEngine.Debug.LogError("LogRecord initialize exception:\n" + e.ToString());
			}
		}

		internal static void _OnLogReceived(LogItem logItem)
		{
			if (ms_StreamWriter != null)
			{
				bool oldPrettryPrint = LitJson.JsonMapper.GetStaticJsonWriter().PrettyPrint;
				LitJson.JsonMapper.GetStaticJsonWriter().PrettyPrint = false;
				string json = LitJson.JsonMapper.ToJson(logItem);
				LitJson.JsonMapper.GetStaticJsonWriter().PrettyPrint = oldPrettryPrint;
				ms_StreamWriter.WriteLine(json);
			}
		}

        internal static void _ForInItialize()
        {
        }
    }
}
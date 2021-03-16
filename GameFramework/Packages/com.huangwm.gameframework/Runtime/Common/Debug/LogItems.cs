using GF.Common;
using GF.Common.Collection;
using GF.Common.Data;
using System;
using UnityEngine;

namespace GF.Common.Debug
{
	public class LogItems
	{
		/// <summary>
		/// 开发用的电脑内存都比较大，可以多缓存些Log
		/// </summary>
		private const int MAX_LOG_COUNT =
#if UNITY_EDITOR
			1000000
#else
			50000
#endif
			;

		private static LogItems ms_Instance;

		private BetterQueue<LogItem> m_LogItems;
		private int m_LogCount;
		private int m_WarningLogCount;
		private int m_ErrorLogCount;
		private bool m_LogChanged;
		/// <summary>
		/// 是否显示时间
		/// </summary>
		private PrefsValue<bool> m_ShowTimestamp;
		/// <summary>
		/// 是否显示FrameCount
		/// </summary>
		private PrefsValue<bool> m_ShowFrameCount;
		/// <summary>
		/// 是否显示LogId
		/// </summary>
		private PrefsValue<bool> m_ShowLogId;

		/// <summary>
		///  Log的自增长ID
		/// </summary>
		private int m_LastLogId;
		private int m_LogIdToIndex;

		public static LogItems GetInstance()
		{
			if (ms_Instance == null)
			{
				ms_Instance = new LogItems();
			}

			return ms_Instance;
		}

		private LogItems()
		{
			m_LogItems = new BetterQueue<LogItem>(MAX_LOG_COUNT + 1);
			_Clear();

			m_ShowTimestamp = new PrefsValue<bool>("LogItems_m_ShowTimestamp", false);
			m_ShowFrameCount = new PrefsValue<bool>("LogItems_m_ShowFrameCount", false);
			m_ShowLogId = new PrefsValue<bool>("LogItems_m_ShowLogId", false);
		}

		~LogItems()
		{
		}

		public bool ShowTimestamp()
		{
			return m_ShowTimestamp;
		}

		public bool ShowFrameCount()
		{
			return m_ShowFrameCount;
		}

		public bool ShowLogId()
		{
			return m_ShowLogId;
		}

		public void SwitchShowTimestamp()
		{
			m_ShowTimestamp.Set(!m_ShowTimestamp);
			ReformatAllLogSingleLineText();

		}

		public void SwitchShowFrameCount()
		{
			m_ShowFrameCount.Set(!m_ShowFrameCount);
			ReformatAllLogSingleLineText();
		}

		public void SwitchShowLogId()
		{
			m_ShowLogId.Set(!m_ShowLogId);
			ReformatAllLogSingleLineText();
		}

		public int LogIdToIndex(int logId)
		{
			return logId + m_LogIdToIndex;
		}

#if UNITY_EDITOR
		public void LoadFromFile()
		{
			string filePath = UnityEditor.EditorUtility.OpenFilePanel("LogFile", Application.temporaryCachePath, "log");
			if (!System.IO.File.Exists(filePath))
			{
				return;
			}
			string[] logItemsJson = System.IO.File.ReadAllLines(filePath);
			LogItem[] logItems = new LogItem[logItemsJson.Length];
			bool[] successs = new bool[logItems.Length];
			System.Threading.Tasks.ParallelLoopResult parallelLoopResult = System.Threading.Tasks.Parallel.For(0, logItems.Length, iItem =>
			{
				string iterJson = logItemsJson[iItem];
				if (string.IsNullOrEmpty(iterJson))
				{
					successs[iItem] = false;
				}
				else
				{
					try
					{
						logItems[iItem] = new LogItem(LitJson.JsonMapper.ToObject<LogItem>(logItemsJson[iItem]));
						successs[iItem] = true;
					}
					catch (Exception)
					{
						successs[iItem] = false;
					}
				}
			});
			while (!parallelLoopResult.IsCompleted)
			{
			}
			for (int iLog = 0; iLog < logItems.Length; iLog++)
			{
				if (successs[iLog])
				{
					_OnLogReceived(logItems[iLog]);
				}
			}
		}
#endif

		public void _Clear()
		{
			m_LogItems.Clear();
			m_LogCount = 0;
			m_WarningLogCount = 0;
			m_ErrorLogCount = 0;
			m_LogChanged = true;
			m_LastLogId = -1;
			m_LogIdToIndex = 0;
		}

		internal void _OnLogReceived(LogItem logItem)
		{
			switch (logItem._LogType)
			{
				case LogType.Assert:
				case LogType.Error:
				case LogType.Exception:
					m_ErrorLogCount++;
					break;
				case LogType.Warning:
					m_WarningLogCount++;
					break;
				case LogType.Log:
					m_LogCount++;
					break;
			}

			if (m_LogItems.Count + 1 >= MAX_LOG_COUNT)
			{
				switch (m_LogItems[0]._LogType)
				{
					case LogType.Assert:
					case LogType.Error:
					case LogType.Exception:
						m_ErrorLogCount--;
						break;
					case LogType.Warning:
						m_WarningLogCount--;
						break;
					case LogType.Log:
						m_LogCount--;
						break;
				}
				m_LogIdToIndex--;
				m_LogItems.Dequeue();
			}

			logItem._Id = ++m_LastLogId;
			logItem.FormatSingleLineText(m_ShowTimestamp, m_ShowFrameCount, m_ShowLogId);
			m_LogItems.Enqueue(logItem);
			m_LogChanged = true;
		}

		public void _GetForGUI(out BetterQueue<LogItem> logItems, out int logCount, out int warningLogCount, out int errorLogCount)
		{
			logItems = m_LogItems;
			logCount = m_LogCount;
			warningLogCount = m_WarningLogCount;
			errorLogCount = m_ErrorLogCount;
		}

		public bool _GetLogChanged()
		{
			return m_LogChanged;
		}

		public void _ResetLogChanged()
		{
			m_LogChanged = false;
		}

		private void ReformatAllLogSingleLineText()
		{
			for (int iLog = 0; iLog < m_LogItems.Count; iLog++)
			{
				LogItem iterLog = m_LogItems[iLog];
				iterLog.FormatSingleLineText(m_ShowTimestamp, m_ShowFrameCount, m_ShowLogId);
				m_LogItems[iLog] = iterLog;
			}
		}
	}
}
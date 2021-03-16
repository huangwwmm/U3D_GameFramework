using System;
using UnityEngine;

namespace GF.Common.Debug
{
	public struct LogItem
	{
		private const int MAX_TAG_LENGTH = 12;

		/// <summary>
		/// LogType的缩写
		/// </summary>
		public readonly int LT;
		/// <summary>
		/// DataTime的缩写
		/// </summary>
		public readonly DateTime DT;
		public readonly string Tag;
		public readonly string Text;
		/// <summary>1
		/// </summary>
		public readonly string ST;
		/// <summary>
		/// FrameCount的缩写
		/// </summary>
		public readonly int FC;
        
		[LitJson.JsonIgnore]
		public readonly LogType _LogType;
		[LitJson.JsonIgnore]
		public readonly string _Hash;
		[LitJson.JsonIgnore]
		public string _SingleLineText;
		/// <summary>
		/// 自增长的ID
		/// </summary>
		[LitJson.JsonIgnore]
		public int _Id;

		public LogItem(LogItem logItem)
			: this(logItem._LogType, logItem.DT, logItem.Tag, logItem.Text, logItem.ST, logItem.FC)
		{
			LT = logItem.LT;
			switch (LT)
			{
				case 1 << (int)LogType.Error:
					_LogType = LogType.Error;
					break;
				case 1 << (int)LogType.Assert:
					_LogType = LogType.Assert;
					break;
				case 1 << (int)LogType.Warning:
					_LogType = LogType.Warning;
					break;
				case 1 << (int)LogType.Log:
					_LogType = LogType.Log;
					break;
				case 1 << (int)LogType.Exception:
					_LogType = LogType.Exception;
					break;
			}
		}

		public LogItem(LogType logType, DateTime dateTime, string tag, string text, string stackTrace, int frameCount)
		{
			LT = 1 << (int)logType;
			DT = dateTime;
			Tag = tag;
			Text = text;
			ST = stackTrace;
			FC = frameCount;

			_LogType = logType;
			_Hash = string.Format("{0}-{1}-{2}-{3}", logType, tag, text, stackTrace);
			_SingleLineText = "";
			_Id = -1;
		}

		public void FormatSingleLineText(bool showTimestamp, bool showFrameCount, bool showLogId)
		{
			int indexOfEOF = Text.IndexOf('\n');
			_SingleLineText = string.Format("{4}{0}{1} {2} : {3}"
				, showTimestamp
					? DT.ToString("[HH:mm:ss]")
					: ""
				, showFrameCount
					? FC.ToString("[0]")
					: ""
				, Tag.PadRight(MAX_TAG_LENGTH, ' ')
				, indexOfEOF > 0
					? Text.Substring(0, indexOfEOF)
					: Text
				, showLogId
					? _Id.ToString("[0]")
					: "");
		}
	}
}
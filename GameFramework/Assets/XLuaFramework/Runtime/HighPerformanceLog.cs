using GF.Common.Debug;
using GF.Common.Utility;
using GF.Core.Behaviour;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.XLuaFramework
{
    public class HighPerformanceLog : BaseBehaviour
    {
        private const int LOGTYPE_VERBOSE = 100;

        /// <summary>
        /// Lua的c中size_t的字节数
        /// </summary>
        private int m_LuaSizetLength;

        internal HighPerformanceLog()
            : base("XLuaManager.HighPerformanceLog"
                  , (int)BehaviourPriority.LuaLog
                  , BehaviourGroup.Default.ToString())
        {
            EnableFeature(FeatureFlag.TaskUpdate);
        }

        public override void OnInitialize()
        {
            m_LuaSizetLength = CallLuaAPI.lua_gfloginternal_getsizetlength();
        }

        public override object OnTaskUpdate_Thread(object input, float deltaTime)
        {
            unsafe
            {
                int bufferLength = CallLuaAPI.lua_gfloginternal_getbufferlength();
                byte* logBuffer = (byte*)CallLuaAPI.lua_gfloginternal_getbuffer().ToPointer();
                CallLuaAPI.lua_gfloginternal_swapbuffer();
                while (bufferLength > 0)
                {
                    logBuffer = ConvertToIntFromLogBuffer(logBuffer, ref bufferLength, out int type);
                    logBuffer = ConvertToStringFromLogBuffer(logBuffer, ref bufferLength, out string tag);
                    logBuffer = ConvertToStringFromLogBuffer(logBuffer, ref bufferLength, out string message);

                    switch (type)
                    {
                        case LOGTYPE_VERBOSE:
                            MDebug.LogVerbose(tag, message);
                            break;
                        case (int)LogType.Log:
                            MDebug.Log(tag, message);
                            break;
                        case (int)LogType.Warning:
                            MDebug.LogWarning(tag, message);
                            break;
                        case (int)LogType.Error:
                            MDebug.LogError(tag, message);
                            break;
                        default:
                            throw new Exception("Not handle LogType: " + type);
                    }
                }
            }
            return null;
        }

        private unsafe byte* ConvertToStringFromLogBuffer(byte* logBuffer, ref int bufferLength, out string str)
        {
            logBuffer = ConvertToIntFromLogBuffer(logBuffer, ref bufferLength, out int length);
            str = System.Text.Encoding.Default.GetString(logBuffer, length);
            logBuffer += length;
            bufferLength -= length;
            return logBuffer;
        }

        private unsafe byte* ConvertToIntFromLogBuffer(byte* logBuffer, ref int bufferLength, out int intValue)
        {
            switch (m_LuaSizetLength)
            {
                case 4:
                    intValue = MBitConverter.ToInt32(logBuffer);
                    break;
                case 8:
                    intValue = (int)MBitConverter.ToInt64(logBuffer);
                    break;
                default:
                    throw new Exception("Not handle size_t = " + m_LuaSizetLength);
            }

            logBuffer += m_LuaSizetLength;
            bufferLength -= m_LuaSizetLength;

            return logBuffer;
        }
    }
}
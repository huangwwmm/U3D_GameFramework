using GF.Common.Debug;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Common.Utility
{
    /// <summary>
    /// byte转换为其他类型
    /// 对<see cref="System.BitConverter"/>的扩展
    /// </summary>
    public static class MBitConverter
    {
        public static bool IsLittleEndian
        {
            get
            {
                return System.BitConverter.IsLittleEndian;
            }
        }

        public static unsafe int ToInt32(byte* pbyte)
        {
            if (IsLittleEndian)
            {
                return (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
            }
            else
            {
                return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
            }
        }

        public static unsafe long ToInt64(byte* pbyte)
        {
            if (IsLittleEndian)
            {
                int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                return (uint)i1 | ((long)i2 << 32);
            }
            else
            {
                int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                return (uint)i2 | ((long)i1 << 32);
            }
        }

        public static unsafe uint ToUInt32(byte* pbyte)
        {
            return (uint)ToInt32(pbyte);
        }

        public static unsafe ulong ToUInt64(byte* pbyte)
        {
            return (ulong)ToInt64(pbyte);
        }

        /// <summary>
        /// Byte转换为0~F，每两个Byte为一组，组之间用'-'分隔
        /// </summary>
        /// <param name="pbyte"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static unsafe string ToString(byte* pbyte, int length)
        {
            int chArrayLength = length * 3;
            char[] chArray = new char[chArrayLength];

            for (int iByte = 0; iByte < chArrayLength; iByte += 3)
            {
                byte b = pbyte[iByte];
                chArray[iByte] = GetHexValue(b / 16);
                chArray[iByte + 1] = GetHexValue(b % 16);
                chArray[iByte + 2] = '-';
            }

            // We don't need the last '-' character
            return new string(chArray, 0, chArrayLength - 1);
        }

        public static char GetHexValue(int value)
        {
            MDebug.Assert(value >= 0 && value < 16, "value is out of range.");
            if (value < 10)
            {
                return (char)(value + '0');
            }

            return (char)(value - 10 + 'A');
        }

        /// <summary>
        /// 往buffer的指定位置(offset)写入value
        /// </summary>
        /// <param name="offset">ref 写入后的位置</param>
        public static void WriteTo(byte[] buffer, ref int offset, byte value)
        {
            buffer[offset] = value;
            offset++;
        }

        /// <summary>
        /// <see cref="WriteTo(byte[], ref int, byte)"/>
        /// </summary>
        public static unsafe void WriteTo(byte[] buffer, ref int offset, short value)
        {
            fixed (byte* pBuffer = buffer)
            {
                *((short*)pBuffer) = value;
            }
            offset += sizeof(short);
        }

        /// <summary>
        /// <see cref="WriteTo(byte[], ref int, byte)"/>
        /// </summary>
        public static unsafe void WriteTo(byte[] buffer, ref int offset, int value)
        {
            fixed (byte* pBuffer = buffer)
            {
                *((int*)pBuffer) = value;
            }
            offset += sizeof(int);
        }

        /// <summary>
        /// <see cref="WriteTo(byte[], ref int, byte)"/>
        /// </summary>
        public static unsafe void WriteTo(byte[] buffer, ref int offset, float value)
        {
            WriteTo(buffer, ref offset, *(int*)&value);
        }

        /// <summary>
        /// <see cref="WriteTo(byte[], ref int, byte)"/>
        /// </summary>
        public static unsafe void WriteTo(byte[] buffer, ref int offset, double value)
        {
            WriteTo(buffer, ref offset, *(long *)&value);
        }
    }
}
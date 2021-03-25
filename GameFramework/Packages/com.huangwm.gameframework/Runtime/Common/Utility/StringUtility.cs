using GF.Common.Collection;
using GF.Common.Debug;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GF.Common.Utility
{
    public static class StringUtility
    {
        private static BetterQueue<StringBuilder> ms_StringBuilderPool = new BetterQueue<StringBuilder>();
        private static MD5 ms_DefaultMd5;

        public static StringBuilder AllocStringBuilder()
        {
            lock (ms_StringBuilderPool)
            {
                return ms_StringBuilderPool.Count == 0
                    ? new StringBuilder()
                    : ms_StringBuilderPool.Dequeue();
            }
        }

        public static string ReleaseStringBuilder(StringBuilder stringBuilder)
        {
            string str = stringBuilder.ToString();
            stringBuilder.Clear();
            lock (ms_StringBuilderPool)
            {
                ms_StringBuilderPool.Enqueue(stringBuilder);
            }
            return str;
        }

        /// <summary>
        /// 计算一个字符串的MD5
        /// </summary>
        public static string CalculateMD5Hash(string input)
        {
            if (ms_DefaultMd5 == null)
            {
                ms_DefaultMd5 = System.Security.Cryptography.MD5.Create();
            }

            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = ms_DefaultMd5.ComputeHash(inputBytes);

            StringBuilder stringBuilder = AllocStringBuilder();
            for (int iByte = 0; iByte < hashBytes.Length; iByte++)
            {
                stringBuilder.Append(hashBytes[iByte].ToString("X2"));
            }
            return ReleaseStringBuilder(stringBuilder);
        }

        /// <summary>
        /// 把一个字符串转换为变量名
        /// </summary>
        public static string FormatToVariableName(string value, char replace = '_')
        {
            string variableName = string.Empty;
            for (int iChar = 0; iChar < value.Length; iChar++)
            {
                char iterChar = value[iChar];
                if (iterChar == '_'
                    || char.IsLetterOrDigit(iterChar))
                {
                    variableName += iterChar;
                }
                else
                {
                    variableName += replace;
                }
            }
            if (char.IsNumber(variableName[0]))
            {
                variableName = replace + variableName;
            }
            return variableName;
        }

        public static string ConvertToDex(byte[] buffer)
        {
            return ConvertToDex(buffer, 0, buffer.Length);
        }

        public static string ConvertToDex(byte[] buffer, int length)
        {
            return ConvertToDex(buffer, 0, length);
        }

        public static string ConvertToDex(byte[] buffer, int offset, int length)
        {
            StringBuilder stringBuilder = AllocStringBuilder();
            for (int iByte = offset; iByte < length; iByte++)
            {
                stringBuilder.AppendFormat(buffer[iByte].ToString("X2"));
            }
            return ReleaseStringBuilder(stringBuilder);
        }

        public static string Format(string format, Vector3 vec3)
        {
            return string.Format(format, vec3.x, vec3.y, vec3.z);
        }

        public static string FormatToFileName(string value, char sign = '_')
        {
            char[] chars = Path.GetInvalidFileNameChars();
            for (int iChar = 0; iChar < chars.Length; iChar++)
            {
                value = value.Replace(chars[iChar], sign);
            }
            return value;
        }

        public static string FormatToFileName(System.DateTime dateTime)
        {
            return dateTime.ToString("yyyy-M-dd--HH-mm-ss");
        }

        public static string ConvertToDisplay(Vector3 vec3, string format = "F2")
        {
            return $"({vec3.x.ToString(format)}, {vec3.y.ToString(format)}, {vec3.z.ToString(format)})";
        }

        public static string ConvertToDisplay(Quaternion quaternion, string format = "F2")
        {
            return $"({quaternion.x.ToString(format)}, {quaternion.y.ToString(format)}, {quaternion.z.ToString(format)}), {quaternion.w.ToString(format)})";
        }

        /// <summary>
        /// 解析属性，例：
        ///		当attributeText = "ABC()"时：
        ///			attributeName = "ABC"
        ///			args = null
        ///		当attributeText = "ABC(D,E)"时：
        ///			attributeName = "ABC"
        ///			args = string[2] {"D", "E"}
        /// </summary>
        public static bool TryParseAttribute(string attributeText, out string attributeName, out string[] args)
        {
            attributeName = string.Empty;
            args = null;

            Match match = Regex.Match(attributeText, @"(\w+)\s*\((.*)\)");
            if (!match.Success)
            {
                return false;
            }

            attributeName = match.Groups[1].Value;
            string argText = match.Groups[2].Value.Trim();

            if (!string.IsNullOrEmpty(argText))
            {
                args = argText.Split(',');

                for (var iArg = 0; iArg < args.Length; ++iArg)
                {
                    args[iArg] = args[iArg].Trim();
                }
            }
            return true;
        }

        /// <summary> 
        /// RSA加密 
        /// </summary> 
        public static string RSAEncryption(string express, string keyContainerName = "Default")
        {
            CspParameters param = new CspParameters();
            param.KeyContainerName = keyContainerName;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
            {
                byte[] plaindata = Encoding.Default.GetBytes(express);
                byte[] encryptdata = rsa.Encrypt(plaindata, false);
                return System.Convert.ToBase64String(encryptdata);
            }
        }

        /// <summary> 
        /// RSA解密 
        /// </summary> 
        public static string RSADecrypt(string ciphertext, string keyContainerName = "Default")
        {
            CspParameters param = new CspParameters();
            param.KeyContainerName = keyContainerName;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
            {
                byte[] encryptdata = System.Convert.FromBase64String(ciphertext);
                byte[] decryptdata = rsa.Decrypt(encryptdata, false);
                return Encoding.Default.GetString(decryptdata);
            }
        }

        /// <summary>
        /// DES加密
        /// </summary>
        /// <param name="data">加密数据</param>
        /// <param name="key">8位字符的密钥字符串</param>
        /// <param name="iv">8位字符的初始化向量字符串</param>
        public static string DESEncrypt(string data, string key, string iv)
        {
            byte[] keyByte = Encoding.ASCII.GetBytes(key);
            byte[] ivByte = Encoding.ASCII.GetBytes(iv);

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateEncryptor(keyByte, ivByte), CryptoStreamMode.Write);

            StreamWriter wtreamWriter = new StreamWriter(cryptoStream);
            wtreamWriter.Write(data);
            wtreamWriter.Flush();
            cryptoStream.FlushFinalBlock();
            wtreamWriter.Flush();
            return System.Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
        }

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="data">解密数据</param>
        /// <param name="key">8位字符的密钥字符串(需要和加密时相同)</param>
        /// <param name="iv">8位字符的初始化向量字符串(需要和加密时相同)</param>
        public static string DESDecrypt(string data, string key, string iv)
        {
            byte[] keyByte = Encoding.ASCII.GetBytes(key);
            byte[] ivByte = Encoding.ASCII.GetBytes(iv);
            byte[] encryptByte = System.Convert.FromBase64String(data);

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream memoryStream = new MemoryStream(encryptByte);
            CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateDecryptor(keyByte, ivByte), CryptoStreamMode.Read);
            StreamReader streamReader = new StreamReader(cryptoStream);
            return streamReader.ReadToEnd();
        }

        /// <summary>
        /// Quote a string for passing as a single argument to Process.Start
        /// and append it to this string builder.
        /// </summary>
        /// <remarks>
        /// On Windows, quote according to the Win32 CommandLineToArgvW API scheme,
        /// used by most Windows applications (with some notable exceptions, like
        /// cmd.exe and cscript.exe). On Unix, Mono uses the entirely incompatible
        /// GLib g_shell_parse_argv function for converting the argument string to
        /// a native Unix argument list, so quote for that instead.
        ///
        /// Do not use this to quote arguments for command line shells (cmd.exe
        /// or POSIX shell), as these may use distinct quotation mechanisms.
        ///
        /// Do not append two quoted arguments without an (unquoted) separator
        /// between them: Two consecutive quotation marks triggers undocumented
        /// behavior in CommandLineToArgvW and possibly other argument processors.
        /// </remarks>
        /// <see cref="https://blogs.msdn.microsoft.com/twistylittlepassagesallalike/2011/04/23/everyone-quotes-command-line-arguments-the-wrong-way/"/> 
        public static string QuoteForProcessStart(string argument)
        {
            var sb = new StringBuilder();
            // Quote for g_shell_parse_argv when running on Unix (under Mono).
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                sb.Append('\'');
                sb.Append(argument.Replace("\\", "\\\\").Replace("'", "\\'"));
                sb.Append('\'');
                return sb.ToString();
            }


            sb.Append('"');
            for (int i = 0; i < argument.Length; ++i)
            {
                char c = argument[i];
                if (c == '"')
                {
                    for (int j = i - 1; j >= 0 && argument[j] == '\\'; --j)
                        sb.Append('\\');
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            for (int j = argument.Length - 1; j >= 0 && argument[j] == '\\'; --j)
                sb.Append('\\');
            sb.Append('"');
            return sb.ToString();
        }
    }
}
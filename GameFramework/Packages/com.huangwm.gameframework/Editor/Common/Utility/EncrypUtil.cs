using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace GFEditor.Common.Utility
{
	public static class EncrypUtil
	{
		
		/// <summary>
		/// 获取文件Md5
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static string GetFileMD5(string filePath)
		{
			using (FileStream fs = new FileStream(filePath, FileMode.Open))
			{
				MD5 md5 = new MD5CryptoServiceProvider();
				byte[] retVal = md5.ComputeHash(fs);
				fs.Close();
				StringBuilder md5Str = new StringBuilder();
				for (int i = 0; i < retVal.Length; i++)
				{
					md5Str.Append(retVal[i].ToString("x2"));
				}
				return md5Str.ToString();
			}
		}
	}
}


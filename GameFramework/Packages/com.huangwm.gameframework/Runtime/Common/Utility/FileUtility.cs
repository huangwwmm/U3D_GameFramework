using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace GF.Common.Utility
{
    public static class FileUtility
    {
        public static string SystemPathToAssetPath(string path)
        {
            path = path.Replace('\\', '/');
            path = path.Replace(Application.dataPath, "Aseets");
            return path;
        }

        public static string AssetPathToSystemPath(string path)
        {
            path = Application.dataPath + path.Substring("Assets".Length);
            return path;
        }

        /// <summary>
        /// 写入文件 需要处理异常
        /// </summary>
        /// <param name="fileFullName">文件完整路径</param>
        /// <param name="graph">需要保存的数据</param>
        public static void WriteToBinaryFile(string fileFullName, object graph)
        {
            FileStream fs = null;
            try
            {
                if (File.Exists(fileFullName))
                {
                    File.Delete(fileFullName);
                }
                fs = new FileStream(fileFullName, FileMode.OpenOrCreate, FileAccess.Write);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, graph);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }

        /// <summary>
        /// 读取文件 需要处理异常
        /// </summary>
        /// <param name="fileFullName">文件完整路径</param>
        /// <returns>读取到的数据</returns>
        public static object ReadFromBinaryFile(string fileFullName)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(fileFullName, FileMode.OpenOrCreate);
                BinaryFormatter bf = new BinaryFormatter();
                return bf.Deserialize(fs);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }

		/// <summary>
		/// 删除文件
		/// </summary>
		/// <param name="filePath"></param>
		public static void DeleteTextFile(string filePath)
		{
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}

		/// <summary>
		/// 写入文件
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="content"></param>
		public static void CreateFile(string filePath, byte[] content)
		{
			DeleteTextFile(filePath);
			string parentFolderPath = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(parentFolderPath))
			{
				Directory.CreateDirectory(parentFolderPath);
			}
			using (FileStream fs = File.Create(filePath))
			{
				fs.Write(content, 0, content.Length);
				fs.Flush();
				fs.Close();
			}
		}

		/// <summary>
		/// 获取文件Md5
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static string GetFileMD5(string filePath)
		{
			try
			{
				using (FileStream fs = new FileStream(filePath, FileMode.Open))
				{
					MD5 md5 = new MD5CryptoServiceProvider();
					byte[] retVal = md5.ComputeHash(fs);
					fs.Close();
					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < retVal.Length; i++)
					{
						sb.Append(retVal[i].ToString("x2"));
					}
					return sb.ToString();
				}

			}
			catch (Exception ex)
			{
				throw (ex);
			}
		}

		/// <summary>
		/// 写入文件 需要处理异常
		/// </summary>
		/// <param name="fileFullName">文件完整路径</param>
		/// <param name="graph">需要保存的数据</param>
		public static void WriteToJsonFile(string path, object graph)
        {
            File.WriteAllText(path, JsonUtility.ToJson(graph));
        }

        /// <summary>
        /// 读取文件 需要处理异常
        /// </summary>
        /// <param name="fileFullName">文件完整路径</param>
        /// <returns>读取到的数据</returns>
        public static T ReadFromJsonFile<T>(string path)
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }
    }
}
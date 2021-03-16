using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
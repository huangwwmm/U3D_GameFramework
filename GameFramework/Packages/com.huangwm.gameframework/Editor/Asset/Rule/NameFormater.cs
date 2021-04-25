using System;
using System.Text;
using System.IO;
using GF.Common.Debug;

namespace GFEditor.Asset.Rule
{
    public class AssetInfoHelper
    {
		private const string LOG_TAG = "AssetInfoHelper";

		public string AssetPath;
        public string RootPath;

        private string m_ParentFolderName;
        private string m_AssetFileName;
        private string m_Extension;

        private StringBuilder m_FormatedCache = new StringBuilder();
        private StringBuilder m_SignCache = new StringBuilder();
        

        public void SetAssetInfo(string assetPath, string rootPath)
        {
            AssetPath = assetPath;
            RootPath = rootPath;

            m_ParentFolderName = string.Empty;
            m_AssetFileName = string.Empty;
            m_Extension = string.Empty;
        }

        public string Format(string text)
        {
			if(string.IsNullOrEmpty(text))
			{
				MDebug.LogWarning(LOG_TAG,$"Format string IsNullOrEmpty！  AssetPath:{AssetPath},");
			}
            m_FormatedCache.Clear();
            m_SignCache.Clear();

            bool isSign = false;
            for(int iChar = 0; iChar < text.Length; iChar++)
            {
                char iterChar = text[iChar];
                switch (iterChar)
                {
                    case '{':
                        if (isSign)
                        {
                            throw new Exception("NameFormat Not Support : ‘{’ 嵌套");
                        }
                        isSign = true;
                        break;
                    case '}':
                        if (!isSign)
                        {
                            throw new Exception("'{}' Need appear in pairs");
                        }
                        isSign = false;
                        string sign = m_SignCache.ToString().ToUpper();
                        m_SignCache.Clear();
                        AppendSign(sign);
                        break;
                    default:
                        if (isSign)
                        {
                            m_SignCache.Append(iterChar);
                        }
                        else
                        {
                            m_FormatedCache.Append(iterChar);
                        }
                        break;
                }
            }

            return m_FormatedCache.ToString();
        }

        /// <param name="sign">只能用大写字母</param>
        private void AppendSign(string sign)
        {
            switch(sign)
            {
                case "AN":
                    m_FormatedCache.Append(GetAssetFileName());
                    break;
                case "P":
                    m_FormatedCache.Append(GetParentFolderName());
                    break;
                case "E":
                    m_FormatedCache.Append(GetExtension());
                    break;
                default:
                    throw new Exception("Not Support Format Sign Key :" + sign);
            }
        }

        public string GetAssetFileName()
        {
            if (string.IsNullOrEmpty(m_AssetFileName))
            {
                m_AssetFileName = Path.GetFileNameWithoutExtension(AssetPath);
            }
            return m_AssetFileName;
        }

        public string GetExtension()
        {
            if (string.IsNullOrEmpty(m_Extension) && Path.HasExtension(AssetPath))
            {

                m_Extension = Path.GetExtension(AssetPath);
                m_Extension = m_Extension.Substring(1);
            }
            return m_Extension;
        }

        public string GetParentFolderName()
        {
            if (string.IsNullOrEmpty(m_ParentFolderName))
            {
                string directory = Path.GetDirectoryName(AssetPath);
                int index = (directory.Contains("/") ? directory.LastIndexOf('/') : directory.LastIndexOf('\\'))  + 1;
                m_ParentFolderName = directory.Substring(index);
            }

            return m_ParentFolderName;
        }
    }
}
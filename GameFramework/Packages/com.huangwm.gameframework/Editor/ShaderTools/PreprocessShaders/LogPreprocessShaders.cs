using GF.Common.Debug;
using GF.Common.Utility;
using GFEditor.Common.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace GFEditor.ShaderTools.PreprocessShaders
{
    public abstract class LogPreprocessShaders : BasePreprocessShaders<LogPreprocessShadersSetting>
    {
        /// <summary>
        /// 是否包含内置Shader
        /// Shader name => Process Shader CompilerData Count
        /// </summary>
        private Dictionary<string, int> m_BuiltinShaders;
        private Dictionary<string, ShaderInfo> m_ShaderInfos;
        private HashSet<string> m_KeywordsCache;
        private int m_HandledShader;
        private string m_ReportDirectory;

#if !UNITY_2019_1_OR_NEWER
        private string[] m_KeywordNames;
#endif

        public LogPreprocessShaders()
            : base()
        {
        }

        protected override void OnBeforeInternal()
        {
            m_BuiltinShaders = new Dictionary<string, int>();
            m_ShaderInfos = new Dictionary<string, ShaderInfo>();
            m_KeywordsCache = new HashSet<string>();

            m_HandledShader = 0;

            m_ReportDirectory = Setting.FormatReportDirectory();
            if (!Directory.Exists(m_ReportDirectory))
            {
                Directory.CreateDirectory(m_ReportDirectory);
            }

#if !UNITY_2019_1_OR_NEWER
            m_KeywordNames = new string[UnityEngineReflectionUtility.ShaderKeyword.MAX_SHADER_KEYWORDS];
            for (int iKeyword = 0; iKeyword < UnityEngineReflectionUtility.ShaderKeyword.MAX_SHADER_KEYWORDS; iKeyword++)
            {
                m_KeywordNames[iKeyword] = UnityEngineReflectionUtility.ShaderKeyword.GetShaderKeywordName(iKeyword);
            }
#endif
        }

        protected override void OnAfterInternal()
        {
            SaveSummaryToFile();

            m_BuiltinShaders.Clear();
            m_BuiltinShaders = null;
            m_ShaderInfos.Clear();
            m_ShaderInfos = null;
            m_KeywordsCache.Clear();
            m_KeywordsCache = null;
        }

        protected override void OnProcessShaderInternal(Shader shader
            , ShaderSnippetData snippet
            , IList<ShaderCompilerData> data)
        {
            #region Builtin
            bool isBuiltinShader = AssetUtility.IsBuiltinOrLibraryAsset(shader);
            if (isBuiltinShader)
            {
                m_BuiltinShaders[shader.name] = data.Count
                    + (m_BuiltinShaders.TryGetValue(shader.name, out int count)
                        ? count
                        : 0);
            }
            #endregion

            #region ProcessInfo
            ProcessInfo processInfo = new ProcessInfo();
            processInfo.IsBuiltinShader = isBuiltinShader;
            processInfo.ShaderName = shader.name;
            processInfo.ShaderType = snippet.shaderType;
            processInfo.PassType = snippet.passType;
            processInfo.PassName = snippet.passName;

            processInfo.Compilers = new CompilerInfo[data.Count];
            Parallel.For(0, data.Count, (iShader) =>
            {
                processInfo.Compilers[iShader] = GenerateCompilerInfo(data[iShader]);
            });

            processInfo.Keywords = CollectionKeywords(processInfo.Compilers);
            #endregion

            #region Summary
            {
                string shaderName = (processInfo.IsBuiltinShader ? "B_" : "") + processInfo.ShaderName;
                if (!m_ShaderInfos.TryGetValue(shaderName, out ShaderInfo shaderInfo))
                {
                    shaderInfo = new ShaderInfo();
                    m_ShaderInfos.Add(shaderName, shaderInfo);
                }

                shaderInfo.ShaderTypes.Add(processInfo.ShaderType);
                shaderInfo.PassTypes.Add(processInfo.PassType);

                shaderInfo.VariantCount += processInfo.Compilers.Length;
                for (int iKeyword = 0; iKeyword < processInfo.Keywords.Length; iKeyword++)
                {
                    shaderInfo.Keywords.Add(processInfo.Keywords[iKeyword]);
                }
            }
            #endregion

            int index = ++m_HandledShader;
            Task.Run(() =>
            {
                try
                {
                    string directory = $"{m_ReportDirectory}/{(processInfo.IsBuiltinShader ? "B_" : "")}{StringUtility.FormatToFileName(processInfo.ShaderName)}/";
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string processName = $"_({processInfo.ShaderType})_({processInfo.PassType})_({processInfo.PassName})_Count(";
                    string path = $"{directory}{index}_{processName}{processInfo.Compilers.Length}).csv";
                    File.WriteAllText(path
                        , GenerateCompilersReport(processInfo.Compilers, processInfo.Keywords));
                    MDebug.LogVerbose("Shader", "Save PreprocessShaderReport to path " + path);
                }
                catch (Exception e)
                {
                    MDebug.LogError("Shader"
                        , "Save PreprocessShaderReport Exception:\n" + e.ToString());
                }
            });
        }

        private void SaveSummaryToFile()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Shader Name").Append(',')
                 .Append("Shader Types").Append(',')
                 .Append("Pass Types").Append(',')
                 .Append("Variant Count").Append(',')
                 .Append("Keywords").Append(',')
                 .Append('\n');

            int totalVariantCount = 0;
            HashSet<string> totalKeywords = new HashSet<string>();

            foreach (KeyValuePair<string, ShaderInfo> kv in m_ShaderInfos)
            {
                ShaderInfo shaderInfo = kv.Value;

                totalVariantCount += shaderInfo.VariantCount;
                foreach (string keyword in shaderInfo.Keywords)
                {
                    totalKeywords.Add(keyword);
                }

                stringBuilder.Append(kv.Key).Append(',')
                    .Append(string.Join(";", shaderInfo.ShaderTypes)).Append(',')
                    .Append(string.Join(";", shaderInfo.PassTypes)).Append(',')
                    .Append(shaderInfo.VariantCount).Append(',')
                    .Append(string.Join(";", shaderInfo.Keywords)).Append(',')
                    .Append('\n');
            }

            string reportString = stringBuilder.ToString();
            stringBuilder.Clear()
                .Append("Total Variant Count").Append(',').Append(totalVariantCount).Append('\n')
                .Append("Total Keywords").Append(',').Append(string.Join(";", totalKeywords)).Append('\n')
                .Append('\n');

            reportString = stringBuilder.ToString() + reportString;
            try
            {
                string path = m_ReportDirectory + "/Summary.csv";
                File.WriteAllText(path, reportString);
                MDebug.Log("Shader"
                    , "Save PreprocessShaderReport Summary to " + MDebug.FormatPathToHyperLink(path));
            }
            catch (Exception e)
            {
                MDebug.LogError("Shader"
                    , "Save PreprocessShaderReport Summary Exception:\n" + e.ToString());
            }

            stringBuilder.Clear();
            stringBuilder.Append("Shader Name").Append(',')
                 .Append("Variant Count").Append(',')
                 .Append("\n");
            foreach (KeyValuePair<string, int> kv in m_BuiltinShaders)
            {
                stringBuilder.Append(kv.Key).Append(",")
                    .Append(kv.Value).Append(",")
                    .Append("\n");
            }

            try
            {
                string path = m_ReportDirectory + "/Builtin.csv";
                File.WriteAllText(path, stringBuilder.ToString());
                MDebug.Log("Shader"
                    , "Save PreprocessShaderReport Builtin to " + MDebug.FormatPathToHyperLink(path));
            }
            catch (Exception e)
            {
                MDebug.LogError("Shader"
                    , "Save PreprocessShaderReport Builtin Exception:\n" + e.ToString());
            }
        }

        private string GenerateCompilersReport(CompilerInfo[] compilers, string[] keywords)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Used Keywords:").Append(',').Append(string.Join(";", keywords)).Append('\n')
                .Append('\n')
                .Append("GraphicsTier").Append(',')
                .Append("ShaderCompilerPlatform").Append(',')
                .Append("ShaderRequirements").Append(',')
                .Append("Keywords").Append(',')
                .Append("PlatformKeywords").Append('\n');

            for (int iCompiler = 0; iCompiler < compilers.Length; iCompiler++)
            {
                CompilerInfo compiler = compilers[iCompiler];
                stringBuilder.Append(compiler.GraphicsTier).Append(',')
                    .Append(compiler.ShaderCompilerPlatform).Append(',')
                    .Append(compiler.ShaderRequirements.ToString().Replace(',', ';')).Append(',')
                    .Append(string.Join(";", compiler.Keywords)).Append(',')
                    .Append(string.Join(";", compiler.PlatformKeywords)).Append(',')
                    .Append('\n');
            }

            return stringBuilder.ToString();
        }

        private string[] CollectionKeywords(CompilerInfo[] compilers)
        {
            if (compilers == null
                || compilers.Length == 0)
            {
                return new string[0];
            }

            m_KeywordsCache.Clear();
            for (int iCompiler = 0; iCompiler < compilers.Length; iCompiler++)
            {
                CompilerInfo compiler = compilers[iCompiler];
                for (int iKeyword = 0; iKeyword < compiler.Keywords.Length; iKeyword++)
                {
                    string iterKeyword = compiler.Keywords[iKeyword];
                    m_KeywordsCache.Add(iterKeyword);
                }
            }

            return m_KeywordsCache.ToArray();
        }

        /// <summary>
        /// 生成CompilerInfo
        /// </summary>
        private CompilerInfo GenerateCompilerInfo(ShaderCompilerData data)
        {
            CompilerInfo compilerInfo = new CompilerInfo();
            compilerInfo.Keywords = ConvertToKeywords(data.shaderKeywordSet);
            compilerInfo.PlatformKeywords = ConvertToPlatformKeywords(data.platformKeywordSet);
            compilerInfo.ShaderRequirements = data.shaderRequirements;
            compilerInfo.GraphicsTier = data.graphicsTier;
            compilerInfo.ShaderCompilerPlatform = data.shaderCompilerPlatform;
            return compilerInfo;
        }

        /// <summary>
        /// 转换成Keywords
        /// </summary>
        private string[] ConvertToKeywords(ShaderKeywordSet shaderKeywordSet)
        {
            ShaderKeyword[] shaderKeywords = shaderKeywordSet.GetShaderKeywords();
            string[] keywords = new string[shaderKeywords.Length];
            for (int iKeyword = 0; iKeyword < shaderKeywords.Length; iKeyword++)
            {
                ShaderKeyword iterKeyword = shaderKeywords[iKeyword];
                keywords[iKeyword] =
#if UNITY_2019_1_OR_NEWER
                    ShaderKeyword.GetGlobalKeywordName(iterKeyword)
#else
                    m_KeywordNames[UnityEngineReflectionUtility.ShaderKeyword.GetShaderKeywordIndex(iterKeyword)]
#endif
                    ;
            }
            return keywords;
        }

        /// <summary>
        /// 转换成PlatformKeywords
        /// </summary>
        private BuiltinShaderDefine[] ConvertToPlatformKeywords(PlatformKeywordSet platformKeywordSet)
        {
            // 这里是被多线程使用的，所以不能把临时变量Cahce下来
            List<BuiltinShaderDefine> platformKeywords = new List<BuiltinShaderDefine>();
            foreach (BuiltinShaderDefine keyword in Enum.GetValues(typeof(BuiltinShaderDefine)))
            {
                if (platformKeywordSet.IsEnabled(keyword))
                {
                    platformKeywords.Add(keyword);
                }
            }
            return platformKeywords.ToArray();
        }

        /// <summary>
        /// 整个Build过程中，单个Shader的情况
        /// </summary>
        private class ShaderInfo
        {
            public HashSet<ShaderType> ShaderTypes = new HashSet<ShaderType>();
            public HashSet<PassType> PassTypes = new HashSet<PassType>();
            public int VariantCount = 0;
            public HashSet<string> Keywords = new HashSet<string>();
        }

        /// <summary>
        /// 每执行一次<see cref="OnProcessShaderInternal"/>就会生成一个<see cref="ProcessInfo"/>
        /// </summary>
        [Serializable]
        private class ProcessInfo
        {
            public bool IsBuiltinShader;
            public string ShaderName;
            public ShaderType ShaderType;
            public PassType PassType;
            public string PassName;
            public string[] Keywords;
            public CompilerInfo[] Compilers;
        }

        [Serializable]
        private class CompilerInfo
        {
            public string[] Keywords;
            public BuiltinShaderDefine[] PlatformKeywords;
            public ShaderRequirements ShaderRequirements;
            public GraphicsTier GraphicsTier;
            public ShaderCompilerPlatform ShaderCompilerPlatform;
        }
    }
}
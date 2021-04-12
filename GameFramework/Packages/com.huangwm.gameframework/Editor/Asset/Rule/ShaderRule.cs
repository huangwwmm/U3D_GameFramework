using System.Collections.Generic;
using System.IO;
using GF.Common;
using GF.Core.Renderer;
using GFEditor.Asset.Build;
using GFEditor.Common.Utility;
using UnityEditor;
using UnityEngine;

namespace GFEditor.Asset.Rule
{
    [CTCreateAssetMenuItem("AssetBundle/创建规则/Shader", "E_1_Shader")]
    public class ShaderRule : BaseRule
    {
        private const string HELP_TEXT = "搜集所有用到的Shader放到一个单独的Bundle里";

        public string BundleName = string.Empty;
        public IShaders Shaders;
        public bool AutoGenerateShaderVariantCollection;
        public string AutoGenerateShaderVariantCollectionPath;
        public bool CustomShaderVariantCollectionPathsFoldout;
        public string[] CustomShaderVariantCollectionPaths;

        public override void Execute(Context context)
        {
            Shaders?.ClearShaders();
            Shaders?.ClearComputeShaders();
            Shaders?.ClearShaderVariantCollections();

            if (AutoGenerateShaderVariantCollection)
            {
                UnityEditorReflectionUtility.ShaderUtil.SaveCurrentShaderVariantCollection(AutoGenerateShaderVariantCollectionPath);
                AddShaderVariantCollection(context, AutoGenerateShaderVariantCollectionPath);
            }

            if (CustomShaderVariantCollectionPaths != null)
            {
                for (int iShader = 0; iShader < CustomShaderVariantCollectionPaths.Length; iShader++)
                {
                    AddShaderVariantCollection(context, CustomShaderVariantCollectionPaths[iShader]);
                }
            }

            Dictionary<string, HashSet<string>> assetDependenciesToBundle = context.GetAssetDependenciesToBundle();
            foreach (string asset in assetDependenciesToBundle.Keys)
            {
                string extension = Path.GetExtension(asset).ToLower();
                if (extension == ".shader")
                {
                    Shader shader = AssetDatabase.LoadMainAssetAtPath(asset) as Shader;
                    if (shader)
                    {
                        Shaders?.AddShader(shader );
                        context.AddAsset(asset, null, _Name);
                    }
                }
                else if (extension == ".compute")
                {
                    ComputeShader shader = AssetDatabase.LoadMainAssetAtPath(asset) as ComputeShader;
                    if (shader)
                    {
                        Shaders?.AddComputeShader(shader );
                        context.AddAsset(asset, null, _Name);
                    }
                }
            }

            if (Shaders != null)
            {
                EditorUtility.SetDirty(Shaders);
            }
        }

        private void AddShaderVariantCollection(Context context
            , string path)
        {
            ShaderVariantCollection shaderVariantCollection = AssetDatabase.LoadMainAssetAtPath(path) as ShaderVariantCollection;
            if (shaderVariantCollection == null)
            {
                GF.Common.Debug.MDebug.LogError("Builder", "Not found ShaderVariantCollection at path: " + path);
            }
            else
            {
                Shaders?.AddShaderVariantCollection(shaderVariantCollection);
                context.AddAsset(path, null, _Name);
            }
        }

        public override string GetHelpText()
        {
            return HELP_TEXT;
        }
    }

    [CustomEditor(typeof(ShaderRule))]
    public class ShaderRuleEditor : Editor
    {
        private ShaderRule m_Rule;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_Rule._OnInspectorGUI();

            m_Rule.BundleName = EditorGUILayout.TextField("Bundle名字", m_Rule.BundleName).ToLower();
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("可以没有Shaders\n"
                    + "用途：方便查看用到的Shader\n"
                    + "\t在Shaders上扩展功能来收集分析Shader\n"
                    + "用法:\n"
                    + "\t1. 写一个继承自IShaders的类\n"
                    + "\t2. 创建这个类的Asset，拖到下方"
                , MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Shaders")
                , EditorGUIUtility.TrTextContent("Shaders")
                , true);
            EditorGUILayout.Space();

            m_Rule.AutoGenerateShaderVariantCollection = EditorGUILayout.Toggle("自动生成变体集"
               , m_Rule.AutoGenerateShaderVariantCollection);
            if (m_Rule.AutoGenerateShaderVariantCollection)
            {
                m_Rule.AutoGenerateShaderVariantCollectionPath = EGLUtility.AssetPath<ShaderVariantCollection>("自动变体集路径"
                    , m_Rule.AutoGenerateShaderVariantCollectionPath);
            }

            m_Rule.CustomShaderVariantCollectionPathsFoldout = EditorGUILayout.Foldout(m_Rule.CustomShaderVariantCollectionPathsFoldout
                , "自定义变体集");
            if (m_Rule.CustomShaderVariantCollectionPathsFoldout)
            {
                EditorGUI.indentLevel++;
                int oldCount = m_Rule.CustomShaderVariantCollectionPaths == null ? 0 : m_Rule.CustomShaderVariantCollectionPaths.Length;
                int count = EditorGUILayout.DelayedIntField("数量", oldCount);
                if (count != oldCount)
                {
                    string[] customShaderVariantCollectionPaths = new string[count];
                    count = Mathf.Min(count, oldCount);
                    for (int iShader = 0; iShader < count; iShader++)
                    {
                        customShaderVariantCollectionPaths[iShader] = m_Rule.CustomShaderVariantCollectionPaths[iShader];
                    }

                    m_Rule.CustomShaderVariantCollectionPaths = customShaderVariantCollectionPaths;
                }

                count = m_Rule.CustomShaderVariantCollectionPaths == null ? 0 : m_Rule.CustomShaderVariantCollectionPaths.Length;
                for (int iShader = 0; iShader < count; iShader++)
                {
                    m_Rule.CustomShaderVariantCollectionPaths[iShader] = EGLUtility.AssetPath<ShaderVariantCollection>("自定义变体集路径"
                        , m_Rule.CustomShaderVariantCollectionPaths[iShader]);
                }
                EditorGUI.indentLevel--;
            }

            GUI.enabled = true;
            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        protected void OnEnable()
        {
            m_Rule = target as ShaderRule;
        }

        protected void OnDisable()
        {
            m_Rule = null;
        }
    }
}
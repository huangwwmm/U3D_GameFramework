using UnityEngine;
using UnityEditor;
using GFEditor.Asset.AssetBundle.Build;

namespace GFEditor.Asset.AssetBundle.Rule
{
    public abstract class BaseRule : ScriptableObject
    {
        /// <summary>
        /// 从这个规则的文件名中获取{_Enable}_{_Order}_{_Name}
        /// </summary>
        internal int _Order;
        internal bool _Enable;
        internal string _Name;

        public abstract void Execute(Context context);

        public abstract string GetHelpText();

        public static int Comparison(BaseRule a, BaseRule b)
        {
            return a._Order - b._Order;
        }

        public void _OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(GetHelpText(), MessageType.Info);
            if (GUILayout.Button("测试"))
            {
                Context context = new Context();
                Execute(context);
                context.LogDebugInfo();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
    }
}
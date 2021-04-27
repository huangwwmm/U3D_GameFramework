using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Menu
{
    [MenuItem("SlideCube/LevelCreate")]
    public static void LevleCreate()
    {
        LevelWindow win = EditorWindow.GetWindow<LevelWindow>();
        win.titleContent=new GUIContent("关卡编辑");
        win.Show();
    }
}

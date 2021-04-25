using System;
using System.Collections;
using System.Collections.Generic;
using GF.Asset;
using GF.Core;
using UnityEditor;
using UnityEngine;

namespace GF.UI
{
    public class Test_Ui : MonoBehaviour
    {
        public static Test_Ui Instance;
    
        
        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            DontDestroyOnLoad(this);
            KernelInitializeData kernelInitializeData = new KernelInitializeData().RestoreToDefault();
            kernelInitializeData.EventTypes = new List<Type>
            {
                //typeof(SlideEventNames)
            };
            kernelInitializeData.LuaEnableHighPerformanceLog = true;

#if UNITY_EDITOR
            kernelInitializeData.LoadLuaByAssetDatabaseWhenEditor = true;
#endif

            yield return Kernel.Initialize(this,kernelInitializeData);
            OnStart();
        }
        
        private void OnStart()
        {
            Kernel.UiManager.OpenWindow(typeof(TestUI));
            //Kernel.UiManager.PushPanel(AssetKey.Fgui_Teeeesssss_fui_bytes);
            // TextAsset asset = (TextAsset)AssetDatabase.LoadAssetAtPath("Assets/UI_Test/Res/Fgui/Teeeesssss_fui.bytes",typeof(TextAsset));
            //  UIPackage.AddPackage("Assets/UI_Test/Res/Fgui/Teeeesssss");
            // GComponent _mainView;
            //
            // //UIPackage.AddPackage("Assets/UI_Test/Res/Fgui/Teeeesssss");
            // UIPackage.AddPackage(obj as AssetBundle);
            //
            // _mainView = UIPackage.CreateObject("Package1", "Component1").asCom;
            // _mainView.fairyBatching = true;
            // _mainView.SetSize(GRoot.inst.width, GRoot.inst.height);
            // _mainView.AddRelation(GRoot.inst, RelationType.Size);
            //
            // GRoot.inst.AddChild(_mainView);
        }
    }

}

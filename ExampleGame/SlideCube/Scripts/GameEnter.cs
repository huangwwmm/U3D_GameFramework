using System;
using System.Collections;
using System.Collections.Generic;
using GF.Core;
using Test.Game;
using UnityEngine;

namespace GF.ExampleGames.SlideCube
{
    public class GameEnter : MonoBehaviour
    {
        public static GameEnter Instance;

        public CubeManager CubeManager;
        public InputControl InputControl;
        public UIManager UIManager;

        public AnimationCurve Curve;

        
        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            DontDestroyOnLoad(this);
            KernelInitializeData kernelInitializeData = new KernelInitializeData().RestoreToDefault();
            kernelInitializeData.BundleMapFile = UnityEngine.Application.dataPath + "/../../ExampleGame/SlideCube/Editor/Build/BundleInfos.json";
            kernelInitializeData.BundlePath = UnityEngine.Application.dataPath + "/../../ExampleGame/SlideCube/Editor//Build/StandaloneWindows64/AssetBundles";
            kernelInitializeData.AssetInfosFile = UnityEngine.Application.dataPath+"/../../ExampleGame/SlideCube/Editor/Build/AssetInfos.json";
            kernelInitializeData.EventTypes = new List<Type>
            {
                typeof(SlideEventNames)
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
            CubeManager = new CubeManager();
            InputControl = new InputControl();
            UIManager = new UIManager();
            CubeManager.Init(2, 2);
        }

    }
}

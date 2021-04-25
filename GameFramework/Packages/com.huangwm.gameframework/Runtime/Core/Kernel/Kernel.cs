using GF.Common.Debug;
using GF.Core.Behaviour;
using GF.Core.Event;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using GF.Core.Lua;
using GF.Core.Entity;
using GF.Asset;
using GF.UI;

namespace GF.Core
{
    public static class Kernel
    {
        public static ObjectPoolManager ObjectPool;
        public static EventCenter EventCenter;
		
		public static IAssetManager AssetManager;

        public static ILuaManager LuaManager;
        public static BehaviourManager BehaviourManager;
        public static EntityManager EntityManager;

        public static IFairyGUIWindowManager UiManager;
        private static bool ms_IsInitialized = false;

		public static MonoBehaviour Mono;
		public static TempAssetInitManager AssetInitManager;
		public static TempDownloadManager DownloadManager;

        public static IEnumerator Initialize(MonoBehaviour mono, KernelInitializeData initializeData)
        {
            MDebug.Assert(ms_IsInitialized == false, "ms_IsInitialized == false");
            ms_IsInitialized = true;
			Mono = mono;

			MDebug.Log("Core", "Initialize kernel with date:\n" + JsonUtility.ToJson(initializeData, true));

            BehaviourManager = new GameObject("GF.Core").AddComponent<BehaviourManager>();
            yield return null;

            ObjectPool = new ObjectPoolManager(initializeData);
            yield return null;

            // add gf event
            if (initializeData.EventTypes == null)
            {
                initializeData.EventTypes = new List<Type>();
            }
            initializeData.EventTypes.Insert(0, typeof(EventName));
            EventCenter = new EventCenter(initializeData);
            yield return null;

			
            EntityManager = new EntityManager(initializeData);
            yield return null;

			DownloadManager = new TempDownloadManager();
			yield return DownloadManager.InitializeAsync(initializeData);

			if (initializeData.UseAssetBundle)
			{
				AssetManager = new AssetManager();
				yield return ((AssetManager)AssetManager).InitializeAsync(initializeData);
			}

			//TempAssetInitManager
			AssetInitManager = new TempAssetInitManager();
			yield return AssetInitManager.InitializeAsync(initializeData);
			
#if UNITY_EDITOR
			UiManager = new EditorFairyGUIWindowManager();
			
#else
			UiManager = new FairyGUIWindowManager();
#endif
	        yield return null;
#region Initialize Packages
            List<Common.Utility.ReflectionUtility.MethodAndAttributeData> initializePackages = new List<Common.Utility.ReflectionUtility.MethodAndAttributeData>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int iAssembly = 0; iAssembly < assemblies.Length; iAssembly++)
            {
                Common.Utility.ReflectionUtility.CollectionMethodWithAttribute(initializePackages
                   , assemblies[iAssembly]
                   , BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
                   , typeof(InitializePackageAttribute)
                   , false);
            }

            List<InitializePackageData> initializePackageDatas = new List<InitializePackageData>();
            for (int iPackage = 0; iPackage < initializePackages.Count; iPackage++)
            {
                initializePackageDatas.Add(new InitializePackageData(initializePackages[iPackage]));
            }
            initializePackageDatas.Sort(InitializePackageData.ComparerByPriority);
            yield return null;

            object[] initializePackageParameters = new object[] { initializeData };
            for (int iPackage = 0; iPackage < initializePackageDatas.Count; iPackage++)
            {
                InitializePackageData iterPackageData = initializePackageDatas[iPackage];
                MDebug.Log("Core"
                    , $"Begin initialize package {iterPackageData.Name}");
                object result = iterPackageData.Method.Invoke(null, initializePackageParameters);
                if (result != null
                    && result is IEnumerator enumerator)
                {
                    yield return enumerator;
                }
                MDebug.Log("Core"
                  , $"End initialize package {iterPackageData.Name}");
                yield return null;
            }
#endregion
        }
    }
}
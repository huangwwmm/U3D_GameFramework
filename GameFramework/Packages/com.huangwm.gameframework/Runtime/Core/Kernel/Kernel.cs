﻿using GF.Common.Debug;
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

namespace GF.Core
{
    public static class Kernel
    {
        public static ObjectPoolManager ObjectPool;
        public static EventCenter EventCenter;
        public static AssetManager AssetManager;
        public static ILuaManager LuaManager;
        public static BehaviourManager BehaviourManager;
        public static EntityManager EntityManager;

        private static bool ms_IsInitialized = false;

        public static IEnumerator Initialize(KernelInitializeData initializeData)
        {
            MDebug.Assert(ms_IsInitialized == false, "ms_IsInitialized == false");
            ms_IsInitialized = true;

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

            AssetManager = new AssetManager();
            yield return AssetManager.InitializeAsync(initializeData);

            EntityManager = new EntityManager(initializeData);
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
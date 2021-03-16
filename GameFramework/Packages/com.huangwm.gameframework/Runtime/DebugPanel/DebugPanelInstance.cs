using GF.Common.Debug;
using GF.Common.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GF.DebugPanel
{
    public static class DebugPanelInstance
    {
        public static IDebugPanel _ms_Instance;

        public static IDebugPanel GetInstance()
        {
            if (_ms_Instance == null)
            {
                _ms_Instance = new DummyDebugPanel();
            }
            return _ms_Instance;
        }

        public static void RegistStartupTabs()
        {
            List<ReflectionUtility.TypeAndAttributeData> tabTypes = new List<ReflectionUtility.TypeAndAttributeData>();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int iAssembly = 0; iAssembly < assemblies.Length; iAssembly++)
            {
                ReflectionUtility.CollectionTypeWithAttribute(tabTypes
                    , assemblies[iAssembly]
                    , typeof(StartupTabAttribute)
                    , false);
            }

            for (int iTab = 0; iTab < tabTypes.Count; iTab++)
            {
                ReflectionUtility.TypeAndAttributeData iterTabData = tabTypes[iTab];
                try
                {
                    StartupTabAttribute iterAttribute = (StartupTabAttribute)iterTabData.Attribute;
                    if (iterAttribute.OnlyRuntime
                        && Application.isEditor)
                    {
                        continue;
                    }
                    ITab iterTab = (ITab)iterTabData.Type.Assembly.CreateInstance(iterTabData.Type.FullName);
                    _ms_Instance.RegistGUI(iterAttribute.TabName
                        , iterTab.DoGUI
                        , iterAttribute.OnlyRuntime);
                }
                catch (Exception e)
                {
                    MDebug.LogWarning("DebugPanel"
                        , $"加载DebugPanel.Tab({iterTabData.Type.FullName})失败\n{e.ToString()}");
                }
            }
        }

#if UNITY_EDITOR
        public static void _SetInstanceToDummy()
        {
            _ms_Instance = new DummyDebugPanel();
        }
#endif
    }
}
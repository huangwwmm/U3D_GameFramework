﻿using System;
using System.Collections.Generic;

namespace GF.Core
{
    [Serializable]
    public class KernelInitializeData
    {
        /// <summary>
        /// 这个Type是游戏自定义的Event枚举, GF框架中的Event对应<see cref="Event.EventName"/>
        /// 
        /// 为了减少内存碎片，<see cref="Event.EventCenter.m_Listeners"/>用的数组形式，EventId对应数组的索引
        /// 所以游戏自定义的枚举应该从<see cref="Event.EventName.GFEnd"/>开始
        /// </summary>
        public List<Type> EventTypes;

        #region Lua
        /// <summary>
        /// 资源<see cref="GF.Core.Lua.ILuaScripts"/>的Key
        /// </summary>
        public string LuaScriptsAssetKey;
        /// <summary>
        /// 游戏逻辑的起始文件
        /// 和<see cref="LuaEnterFunction"/>结合起来，相当于进程的入口函数(Main)
        /// </summary>
        public string LuaEnterFile;
        /// <summary>
        /// 游戏逻辑的入口函数
        /// <see cref="LuaEnterFile"/>
        /// </summary>
        public string LuaEnterFunction;

        /// <summary>
        /// True : Lua的log会先缓存到c的LogBuffer中，每帧结束的时候c#获取c的LogBuffer解析并输出。
        ///         缺点：如果是C#使用多线程和Lua线程同步执行，Log打印时机不是按真实的顺序
        ///             Log的打印时间会有延迟
        /// False : Lua会直接调用c#的Log函数输出
        ///         缺点：Lua和C#频繁交互，性能不好
        /// </summary>
        public bool LuaEnableHighPerformanceLog;

#if UNITY_EDITOR
        /// <summary>
        /// Editor下使用<see cref="UnityEditor.AssetDatabase"/>来加载Lua脚本
        /// </summary>
        public bool LoadLuaByAssetDatabaseWhenEditor;
#endif

        /// <summary>
        /// lua的Lib
        /// </summary>
        public Lua.LuaLibItem[] LuaLibs;

        /// <summary>
        /// Lua脚本中支持的宏定义
        /// </summary>
        /// <remarks>
        /// 在Lua中
        ///     --[#if     <==>    #if
        ///     --]#endif  <==>    #endif
        /// 具体使用规则和C#中相同
        /// 例：
        /// <code>
        /// --[#if UNITY_EDITOR || (UNITY_ANDROID && GF_DEBUG)
        /// gflog.log("a", "nsaer")
        /// --]#endif
        /// 
        /// --[#if GF_DEBUG && !UNITY_EDITOR
        /// gflog.error("c3wf", "432qg")
        /// --]#endif
        /// 
        /// --[#if GF_DEBUG && UNITY_EDITOR
        /// gflog.warning("f23f", "asdfe")
        /// --]#endif
        /// </code>
        /// </remarks>
        public HashSet<string> LuaScriptingDefine;
        #endregion

        #region AssetBundle

        /// <summary>
        /// 该文件存储：资源文件Key值与实际路径和AB包名称的对应关系
        /// </summary>
        public string AssetInfosFile;

        /// <summary>
        /// 该文件存储： AB包的直接依赖关系
        /// </summary>
        public string BundleMapFile;

        /// <summary>
        /// AB包存储路径
        /// </summary>
        public string BundlePath;

		// TODO 是否可以优化,临时设置Bool，编辑器模式下，直接加载资源，运行时，加载AssetBundle资源
		public bool UseAssetBundle;

        #endregion

        public KernelInitializeData RestoreToDefault()
        {
            EventTypes = null;

            LuaScriptsAssetKey = "LuaScripts";
            LuaEnterFile = "TEST_Game.Main";
            LuaEnterFunction = "Main.Start()";

            LuaEnableHighPerformanceLog = true;
            LuaLibs = null;

#if UNITY_EDITOR
            LoadLuaByAssetDatabaseWhenEditor = false;
#endif


#if UNITY_EDITOR
			UseAssetBundle = false;
#else
			UseAssetBundle = true;
#endif

			LuaScriptingDefine = new HashSet<string>();

#if UNITY_EDITOR
            LuaScriptingDefine.Add("UNITY_EDITOR");
#endif

#if GF_DEBUG
            LuaScriptingDefine.Add("GF_DEBUG");
#endif
            // HACK just testAssetBundle;
			//todo,Editor,runtime  两种方式获取，先写死测试 默认Editor
			//todo 根据不同平台，设置不同路径例如Windows/AssetBundles, Android/AssetBundles
            BundleMapFile = UnityEngine.Application.dataPath + "/../Build/BundleInfos.json";
            BundlePath = UnityEngine.Application.dataPath + "/../Build/StandaloneWindows64/AssetBundles";
            AssetInfosFile = UnityEngine.Application.dataPath+"/../Build/AssetInfos.json";

            return this;
        }
    }
}
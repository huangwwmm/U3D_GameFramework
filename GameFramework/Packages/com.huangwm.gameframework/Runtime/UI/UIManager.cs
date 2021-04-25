using System;
using System.Collections.Generic;
using FairyGUI;
using GF.Asset;
using GF.Common.Data;
using GF.Common.Debug;
using GF.Core;
using GF.Core.Behaviour;
using UnityEngine;

namespace GF.UI
{ 
    public class UIManager
    {

        #region 单例
        private static UIManager _instance;

        public static UIManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new UIManager();
            }
            return _instance;
        }
        #endregion

        #region 构造
        /// <summary>
        /// 只需要将Prefabs路径读进内存中
        /// </summary>
        public UIManager()
        {
            panels = new Dictionary<int, BasePanel>(16);
            removePanelIds = new List<int>(16);
            panelLinkedList = new LinkedList<BasePanel>();
        }
        #endregion

        #region 字段
        /// <summary>
        /// UI关闭多少秒，就可以destroy
        /// </summary>
        private const long AUTO_GC_MILLISECONDS = 1000 * 60 * 5;
        
        /// <summary>
        /// 保存已经存在的BasePanel类型
        /// </summary>
        private Dictionary<int, BasePanel> panels;


        private List<int> removePanelIds;

        /// <summary>
        /// 管理保存所有显示的面板Panel
        /// </summary>
        private LinkedList<BasePanel> panelLinkedList;

        /// <summary>
        /// 所有Panel都需要在Canvas下
        /// </summary>
        private Transform _canvas;
        private Transform Canvas
        {
            get
            {
                if (_canvas == null)
                    _canvas = GameObject.Find("Canvas").transform;
                return _canvas;
            }
        }
        #endregion

        #region 公开的方法
        /// <summary>
        /// 向栈中添加面板
        /// </summary>
        /// <param name="type"></param>
        public void PushPanel(AssetKey type)
        {
            BasePanel basePanel = GetPanel(type);
            if (basePanel != null)              // 如果不为空处理，为空不作处理
            {
                if (panelLinkedList.Count > 0)       // 如果栈中存在Panel
                {
                    panelLinkedList.Last.Value.OnPause();// 调用已经存在的Panel的OnPause方法(做些禁用面板或者什么操作)
                }

                basePanel.OnEnter();            // 调用需要加入的Panel的OnEnter方法(做些面板入场动画之类的)
                panelLinkedList.AddLast(basePanel);     // 然后将面板放入到栈中
            }
        }

        public void PopPanel()
        {
            if (panelLinkedList.Count > 0)                   // 当栈中的有面板的时候才处理
            {
                BasePanel basepanel = panelLinkedList.Last.Value; // 直接弹出栈顶的Panel，然后调用栈顶Panel的OnExit()方法（处理一些隐藏之类的动画啥的）
                basepanel.OnExit();
                panelLinkedList.RemoveLast();
                if (panelLinkedList.Count > 0)               // 弹出以后，发现还有Panel在栈中的时候，就调用新栈顶Panel的OnResume()，恢复可操作之类的
                {
                    panelLinkedList.Last.Value.OnResume();
                }
            }
        }
        #endregion

        #region 帮助方法

        /// <summary>
        /// 获取实例化好的游戏对象组件BasePanel，如果不存在就实例化，存在直接返回
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private BasePanel GetPanel(AssetKey _type)
        {
            // 但字典中有的时候直接返回就行了
            //BasePanel basePanel = null;
            //if (panels.TryGetValue(type, out basePanel))
            //{
            //    return basePanel;
            //}
            int type = (int) _type;
            // 这里用到了Dictionary的扩展方法
            BasePanel basePanel = panels.TryGet(type);
            
            if (basePanel == null)
            {
                // string path = panelPaths.TryGet(type);
                // if (path != null)
                // {
                //     GameObject go = Resources.Load(path) as GameObject;     // 加载Prefab
                //     basePanel = GameObject.Instantiate(go).GetComponent<BasePanel>();   // 实例化到场景中
                //  //   basePanel.transform.SetParent(Canvas, false);   // 使用局部坐标
                //     panels.Add(type, basePanel);
                // }
                // else
                // {
                //     throw new Exception(type + "不存在");
                // }
                
                #if UNITY_EDITOR
                UIPackage.AddPackage("Assets/UI_Test/Res/Fgui/Teeeesssss");
                GComponent _mainView;

                _mainView = UIPackage.CreateObject("Package1", "Component1").asCom;
                _mainView.fairyBatching = true;
                _mainView.SetSize(GRoot.inst.width, GRoot.inst.height);
                _mainView.AddRelation(GRoot.inst, RelationType.Size);
                GRoot.inst.AddChild(_mainView);
                
                #else
                Kernel.AssetManager.LoadAssetAsync(_type, (GF.Asset.AssetKey key, UnityEngine.Object tmpObj) =>
                {
                   // GameObject go = tmpObj as GameObject;
                    // basePanel = go.GetComponent<BasePanel>();   // 实例化到场景中
                    // basePanel.transform.SetParent(Canvas, false);   // 使用局部坐标
                    // panels.Add(type, basePanel);
                    if (tmpObj.Equals("")) return;
                    UIPackage.AddPackage(tmpObj as AssetBundle);
                    GComponent _mainView;
                    _mainView = UIPackage.CreateObject("BundleUsage", "Main").asCom;
                    _mainView.fairyBatching = true;
                    _mainView.SetSize(GRoot.inst.width, GRoot.inst.height);
                    _mainView.AddRelation(GRoot.inst, RelationType.Size);

                    GRoot.inst.AddChild(_mainView);
                });
                #endif
               
            }

            return basePanel;
        }
        
        
        // public override void OnLateUpdate(float deltaTime)
        // {
        //     long currentTime = MDebug.GetMillisecondsSinceStartup();
        //     foreach (var key in panels.Keys)
        //     {
        //         var panel = panels[key];
        //         if (panel.CanDestroy() && currentTime - panel.LastUseTime > AUTO_GC_MILLISECONDS)
        //         {
        //             MDebug.Log("UI"
        //                 , $"Panel<{panel.name}> will be GC.");
        //             panelLinkedList.Remove(panel);
        //             removePanelIds.Add(key);
        //             Kernel.AssetManager.ReleaseGameObjectAsync(panel.gameObject);
        //         }
        //     }
        //
        //     for (int i = 0; i < removePanelIds.Count; i++)
        //     {
        //         panels.Remove(removePanelIds[i]);
        //     }
        // }
        #endregion
    }

}

using System.Collections;
using System.Collections.Generic;
using GF.Core.Behaviour;
using UnityEngine;

namespace GF.UI
{
    public abstract class BasePanel : MonoBehaviour {
    
        protected CanvasGroup canvasGroup;
        protected virtual CanvasGroup CanvasGroup
        {
            get
            {
                return canvasGroup;
            }
        }
        
        
    
        private float lastUseTime;
        public float LastUseTime
        {
            get => lastUseTime;
            set => lastUseTime = value;
        }
    
        private PanelState state;
    
        public bool CanDestroy()
        {
            return state == PanelState.Exit;
        }
    
        /// <summary>
        /// Panel加载出来的时候调用 各种可交互
        /// </summary>
        public virtual void OnEnter()
        {
            state = PanelState.Enter;
        }
    
        /// <summary>
        /// Panel暂停，使得交互不可用
        /// </summary>
        public virtual void OnPause()
        {
            state = PanelState.Pause;
        }
    
        /// <summary>
        /// 恢复可交互状态
        /// </summary>
        public virtual void OnResume()
        {
            state = PanelState.Resume;
        }
    
        /// <summary>
        /// 界面退出 不显示
        /// </summary>
        public virtual void OnExit()
        {
            state = PanelState.Exit;
        }
    }

}

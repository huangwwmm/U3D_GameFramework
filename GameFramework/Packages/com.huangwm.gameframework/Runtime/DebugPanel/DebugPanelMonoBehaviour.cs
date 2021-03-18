using GF.Common.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GF.DebugPanel
{
    [ExecuteInEditMode]
    public class DebugPanelMonoBehaviour : MonoBehaviour
    {
#if UNITY_EDITOR
        private const long NOTSET_EDITMODE_MILLISECONDS = long.MinValue;
        private const float EDITMODE_DEFAULT_UPDATE_RATE = 60.0f;
#endif

        private static DebugPanelMonoBehaviour ms_Instance;

        private List<BaseMonoBehaviourTab> m_Tabs;


#if UNITY_EDITOR
        /// <summary>
        /// EditMode时的更新频率，单位 次/秒
        /// </summary>
        internal float _EditModeUpdateRate = EDITMODE_DEFAULT_UPDATE_RATE;
        /// <summary>
        /// 编辑模式下的<see cref="Time.time"/>
        /// 单位毫秒
        /// </summary>
        private long m_LastEditModeMilliseconds;
#endif


        public static DebugPanelMonoBehaviour GetInstance()
        {
            if (ms_Instance == null)
            {
                GameObject gameObject = new GameObject();
                ms_Instance = gameObject.AddComponent<DebugPanelMonoBehaviour>();
#if UNITY_EDITOR
                if (Application.isPlaying)
#endif
                {
                    DontDestroyOnLoad(gameObject);
                }

                gameObject.hideFlags = HideFlags.HideInHierarchy
                    | HideFlags.DontSave;
            }

            return ms_Instance;
        }

        public void AddTab(BaseMonoBehaviourTab tab)
        {
            m_Tabs.Add(tab);
        }

        public void RemoveTab(BaseMonoBehaviourTab tab)
        {
            m_Tabs.Remove(tab);
        }

        protected void Update()
        {
            if (!CheckAlive())
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            float time = Time.time;
            float deltaTime = Time.deltaTime;
            for (int iTab = 0; iTab < m_Tabs.Count; iTab++)
            {
                m_Tabs[iTab].OnUpdate(time, deltaTime);
            }
        }

        protected void FixedUpdate()
        {
            if (!CheckAlive())
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            float time = Time.time;
            float deltaTime = Time.fixedDeltaTime;
            for (int iTab = 0; iTab < m_Tabs.Count; iTab++)
            {
                m_Tabs[iTab].OnFixedUpdate(time, deltaTime);
            }
        }

        protected void LateUpdate()
        {
            if (!CheckAlive())
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            float time = Time.time;
            float deltaTime = Time.deltaTime;
            for (int iTab = 0; iTab < m_Tabs.Count; iTab++)
            {
                m_Tabs[iTab].OnLateUpdate(time, deltaTime);
            }
        }

        private bool CheckAlive()
        {
            if (ms_Instance != this
                || this == null)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.update -= EditorUpdate;
#endif

                m_Tabs = null;
                DestroyImmediate(gameObject);
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private void EditorUpdate()
        {
            if (!CheckAlive())
            {
                return;
            }

            if (Application.isPlaying)
            {
                return;
            }

            // 把自己胡乱拖动, 让编辑器维持更新
            transform.position = UnityEngine.Random.insideUnitSphere;

            // 编辑器下Update的回调
            // 在编辑器下 EditorApplication.update 的更新间隔与Time.deltaTime返回的时间不一致
            long editModeMilliseconds = MDebug.GetMillisecondsSinceStartup();
            if (m_LastEditModeMilliseconds == NOTSET_EDITMODE_MILLISECONDS)
            {
                m_LastEditModeMilliseconds = editModeMilliseconds;
            }
            else
            {
                // 0.001: milliseconds to seconds
                float deltaTime = (editModeMilliseconds - m_LastEditModeMilliseconds) * 0.001f;
                // 60: 60FPS
                if (deltaTime >= 1.0f / _EditModeUpdateRate)
                {
                    float time = editModeMilliseconds * 0.001f;
                    m_LastEditModeMilliseconds = editModeMilliseconds;

                    for (int iTab = 0; iTab < m_Tabs.Count; iTab++)
                    {
                        m_Tabs[iTab].OnEditorUpdate(time, deltaTime);
                    }
                }
            }
        }
#endif

        private DebugPanelMonoBehaviour()
        {
            m_Tabs = new List<BaseMonoBehaviourTab>();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorUpdate;
#endif
        }
    }
}
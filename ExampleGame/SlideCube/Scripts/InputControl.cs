using System;
using System.Collections;
using System.Collections.Generic;
using GF.Core;
using GF.Core.Behaviour;
using UnityEngine;

namespace GF.ExampleGames.SlideCube
{
    /// <summary>
    /// 输入控制类
    /// </summary>
    public class InputControl : BaseBehaviour
    {

        private SlideData m_MoveData;

        private Ray m_Ray;
        private RaycastHit m_Hit;
        private Vector2 m_StartPosition, m_EndPosition;
        
        public InputControl()
            :base("SlideControl", (int)BehaviourPriority.GF_Start, BehaviourGroup.Default.ToString())
        {
            m_MoveData = new SlideData();
        }

        public override void OnUpdate(float deltaTime)
        {
#if UNITY_EDITOR|| UNITY_STANDALONE_WIN
            if (Input.GetMouseButtonDown(0))
            {
                m_StartPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                m_Ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Debug.Log($"StartPos:{m_StartPosition}");
                if (Physics.Raycast(m_Ray, out m_Hit, 100.0f))
                {
                    //Debug.Log("射线检测到的物体名称: " + hit.transform.name);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                m_EndPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                Debug.Log($"EndPos:{m_StartPosition}");
                if (m_Hit.transform != null)
                {
                    m_MoveData.transform = m_Hit.transform;
                    m_MoveData.startPosition = m_StartPosition;
                    m_MoveData.endPosition = m_EndPosition;
                    Kernel.EventCenter.SendImmediately((int)SlideEventNames.CubeSlide, m_MoveData);
                }
                else
                {
                    //Debug.Log("hit is null");
                }
            }
#elif UNITY_ANDROID||UUNITY_IPHONE
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) 
        {
            m_StartPosition = Input.GetTouch(0).deltaPosition;
            m_Ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position); 
            if(Physics.Raycast(ray, out hit, 100.0f))
            {
                //Debug.Log("射线检测到的物体名称: " + hit.transform.name); 
            } 
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            m_EndPosition = Input.GetTouch(0).deltaPosition;
            if (hit.transform!=null)
            {
                m_MoveData.transform = m_Hit.transform;
                m_MoveData.startPosition = m_StartPosition;
                m_MoveData.endPosition = m_EndPosition;
                Kernel.EventCenter.SendImmediately((int)SlideEventNames.CubeSlide, m_MoveData);
            }
            else
            {
                //Debug.Log("hit is null");
            }
        }
#endif
        }

        public override void OnRelease()
        {
            m_MoveData = null;
        }

    }
}
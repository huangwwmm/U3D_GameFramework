using System;
using System.Collections;
using System.Collections.Generic;
using GF.Common.Data;
using GF.Core.Behaviour;
using UnityEngine;

namespace GF.ExampleGames.SlideCube
{
    public class CubeItem: IObjectPoolItem
    {

        private int m_BelongRow = -1;
        private int m_BelongColomn = -1;

        private UnityEngine.Transform m_Transform;

        public UnityEngine.Transform Transform
        {
            get
            {
                return m_Transform;
            }
        }

        public int BelongRow
        {
            get
            {
                return m_BelongRow;
            }
            set
            {
                m_BelongRow = value;
            }
        }

        public int BelongColomn
        {
            get
            {
                return m_BelongColomn;
            }
            set
            {
                m_BelongColomn = value;
            }
        }


        public void Init(int row, int colomn, UnityEngine.Transform transform)
        {
            BelongRow = row;
            BelongColomn = colomn;
            m_Transform = transform;
        }

        /// <summary>
        /// 获取滑动位置初始点
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="firstPosition"></param>
        /// <param name="lastPosition"></param>
        /// <returns></returns>
        public Vector3 GetMoveOriginPosition(Vector3 direction, Vector3 firstPosition, Vector3 lastPosition)
        {
            if (Mathf.Abs(m_Transform.localPosition.x - firstPosition.x) < 0.1f && direction == Vector3.left)
            {
                m_Transform.localPosition = new Vector3(lastPosition.x + GlobalConfig.ITEM_SIZE, 0, m_Transform.position.z);
            }
            else if (Mathf.Abs(m_Transform.localPosition.x - lastPosition.x) < 0.1f && direction == Vector3.right)
            {
                m_Transform.localPosition = new Vector3(firstPosition.x - GlobalConfig.ITEM_SIZE, 0, m_Transform.position.z);
            }
            else if (Mathf.Abs(m_Transform.localPosition.z - firstPosition.z) < 0.1f && direction == Vector3.forward)
            {
                m_Transform.localPosition = new Vector3(m_Transform.position.x, 0, lastPosition.z - GlobalConfig.ITEM_SIZE);
            }
            else if (Mathf.Abs(m_Transform.localPosition.z - lastPosition.z) < 0.1f && direction == Vector3.back)
            {
                m_Transform.localPosition = new Vector3(m_Transform.position.x, 0, firstPosition.z + GlobalConfig.ITEM_SIZE);
            }
            else
            {

            }
            return m_Transform.localPosition;

        }
        public void OnAlloc()
        {
            
        }

        public void OnRelease()
        {
            m_Transform = null;
        }
    }
}

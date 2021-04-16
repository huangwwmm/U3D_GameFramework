using System.Collections;
using System.Collections.Generic;
using GF.Common.Data;
using GF.Core;
using GF.Core.Behaviour;
using GF.Core.Event;
using UnityEngine;

namespace GF.ExampleGames.SlideCube
{
    public class CubeManager : BaseBehaviour
    {
        private float m_OriginX, m_OriginY = 0;
        private float m_CenterX, m_CenterY = 0;
        private int m_Row, m_Colomn = -1;

        private List<CubeItem> m_CubeItems;
        private Vector3 m_FirstPosition, m_LastPosition;
        private CubeItem m_CurrentItem;
        private List<CubeItem> m_TargetList;

        private UnityEngine.Transform m_CubeContainer;
        private UnityEngine.GameObject m_CubePrefab;
        
        private Vector3 m_SlideDirection = Vector3.zero;
        private List<UnityEngine.Transform> m_SlideTransforms;
        private List<Vector3> m_SlideOriginPositions;
        private float m_SlideSpeed = 1;
        private bool m_CanSlide = false;
        private float m_Percent = 0;

        private ObjectPool<CubeItem> m_CubeItemPool;
        private CubeItem m_TempCubeItem;

        public CubeManager()
            : base("CubeManager", (int)BehaviourPriority.GF_Start, BehaviourGroup.Default.ToString())
        {
            m_CubeItemPool = new ObjectPool<CubeItem>();
            m_CubeItems = new List<CubeItem>();
            m_TargetList = new List<CubeItem>();
            m_SlideTransforms = new List<Transform>();
            m_SlideOriginPositions = new List<Vector3>();
            m_CubeContainer = new GameObject("CubeContainer").GetComponent<UnityEngine.Transform>();
            Kernel.EventCenter.AddListen((int)SlideEventNames.CubeSlide, OnCubeSlide);
        }


        public void Init(int row, int colomn)
        {
            if (row < 2 || colomn < 2)
                return;
            m_CubeItems.Clear();
            m_TargetList.Clear();
            m_Row = row;
            m_Colomn = colomn;

            Kernel.AssetManager.InstantiateGameObjectAsync(GF.Asset.AssetKey.Prefabs_Box001_prefab, (GF.Asset.AssetKey key, UnityEngine.Object tmpObj) =>
            {
                m_CubePrefab = tmpObj as GameObject;
                InitCubus(m_CubePrefab);

            });

        }

        /// <summary>
        /// 初始化场景Cubes
        /// </summary>
        /// <param name="prefabCube"></param>
        /// <param name="row"></param>
        /// <param name="colomn"></param>
        private void InitCubus(UnityEngine.GameObject prefabCube)
        {
            m_CenterX = (int)(m_Row * 0.5f);
            m_CenterY = (int)(m_Colomn * 0.5f);
            m_OriginX = m_Row % 2 == 0 ? -GlobalConfig.ITEM_SIZE * 0.5f : -GlobalConfig.ITEM_SIZE;
            m_OriginY = m_Colomn % 2 == 0 ? GlobalConfig.ITEM_SIZE * 0.5f : GlobalConfig.ITEM_SIZE;
            Vector3 centerPosition = new Vector3(m_OriginX * m_CenterX, 0, m_OriginY * m_CenterY);
            int index = 0;
            for (int i = 0; i < m_Colomn; i++)
            {
                for (int j = 0; j < m_Row; j++)
                {
                    Vector3 initPosition = new Vector3(centerPosition.x + GlobalConfig.ITEM_SIZE * j, 0,
                        centerPosition.z - GlobalConfig.ITEM_SIZE * i);
                    GameObject cube = UnityEngine.GameObject.Instantiate(prefabCube, initPosition, Quaternion.identity);
                    cube.transform.SetParent(m_CubeContainer);
                    cube.name = $"CubeItem{index}";
                    cube.transform.localEulerAngles = new Vector3(-90, 0, 0);

                    m_TempCubeItem=m_CubeItemPool.Alloc();
                    m_TempCubeItem.Init(i, j, cube.GetComponent<Transform>());
                    m_CubeItems.Add(m_TempCubeItem);
                    index++;
                }
            }
            m_FirstPosition = m_CubeItems[0].Transform.position;
            m_LastPosition = m_CubeItems[m_CubeItems.Count - 1].Transform.position;
            Kernel.AssetManager.ReleaseGameObjectAsync(m_CubePrefab);
        }

        /// <summary>
        /// Cube滑动事件
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="isImmediately"></param>
        /// <param name="data"></param>
        private void OnCubeSlide(int eventId, bool isImmediately, IUserData data)
        {
            SlideData slideData = (SlideData)data;
            m_CurrentItem = GetHitCubeItem(slideData.transform);
            m_SlideDirection = GetSlideDirection(slideData.startPosition, slideData.endPosition);
            if (m_CurrentItem == null)
            {
                //Debug.Log("hit is null!");
                return;
            }
            m_TargetList.Clear();
            m_SlideTransforms.Clear();
            m_SlideOriginPositions.Clear();

            if (m_SlideDirection==Vector3.left)
            {
                m_TargetList = GetTargetRowList(m_CurrentItem);
                DoLeftOrder(m_TargetList);
            }
            else if (m_SlideDirection==Vector3.right)
            {
                m_TargetList = GetTargetRowList(m_CurrentItem);
                DoRightOrder(m_TargetList);
            }
            else if (m_SlideDirection==Vector3.forward)
            {
                m_TargetList = GetTargetColomnList(m_CurrentItem);
                DoUpOrder(m_TargetList);
            }
            else
            {
                m_TargetList = GetTargetColomnList(m_CurrentItem);
                DoDownOrder(m_TargetList);
            }
            
        }


        /// <summary>
        /// 检查是否完成
        /// </summary>
        private void CheckFinish()
        {
            //Todo 检查是否完成拼图
        }

        /// <summary>
        /// 获取当前射线检测Cube
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        private CubeItem GetHitCubeItem(UnityEngine.Transform transform)
        {
            for (int i = 0; i < m_CubeItems.Count; i++)
            {
                if (m_CubeItems[i].Transform == transform)
                {
                    return m_CubeItems[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取移动方向
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="endPositon"></param>
        /// <returns></returns>
        private Vector3 GetSlideDirection(Vector2 startPosition, Vector2 endPositon)
        {
            Vector2 direction = endPositon - startPosition;
            Vector3 retDirection = Vector3.zero;
            if (direction.y < direction.x && direction.y > -direction.x)
            {
                retDirection = Vector3.right;
            }

            else if (direction.y > direction.x && direction.y < -direction.x)
            {
                retDirection = Vector3.left;
            }
            else if (direction.y > direction.x && direction.y > -direction.x)
            {
                retDirection = Vector3.forward;
            }
            else
            {
                retDirection = Vector3.back;
            }
            return retDirection;
        }

        public override void OnUpdate(float deltaTime)
        {
            if (m_CanSlide)
            {
                if (m_SlideTransforms.Count == 0 || m_SlideDirection == Vector3.zero)
                {
                    return;
                }

                if (m_Percent < 1)
                {
                    m_Percent += Time.deltaTime / GlobalConfig.ANIMATION_MOVETIME;
                    for (int i = 0; i < m_SlideTransforms.Count; i++)
                    {
                        m_SlideTransforms[i].localPosition = m_SlideOriginPositions[i] + m_SlideSpeed * m_SlideDirection * GameEnter.Instance.Curve.Evaluate(m_Percent) * GlobalConfig.ITEM_SIZE;
                    }
                }
                else
                {
                    m_CanSlide = false;
                    m_Percent = 0;
                    CheckFinish();
                }
            }

        }

        private void DoLeftOrder(List<CubeItem> cubeItems)
        {
            if (cubeItems == null || cubeItems.Count == 0)
            {
                return;
            }
            for (int i = 0; i < cubeItems.Count; i++)
            {
                cubeItems[i].BelongColomn = (cubeItems[i].BelongColomn - 1) < 0 ? m_Colomn - 1 : (cubeItems[i].BelongColomn - 1);
                m_SlideTransforms.Add(cubeItems[i].Transform);
                m_SlideOriginPositions.Add(cubeItems[i].GetMoveOriginPosition(Vector3.left, m_FirstPosition, m_LastPosition));
            }

            m_CanSlide = true;
        }

        private void DoRightOrder(List<CubeItem> cubeItems)
        {
            if (cubeItems == null || cubeItems.Count == 0)
            {
                return;
            }
            for (int i = 0; i < cubeItems.Count; i++)
            {
                cubeItems[i].BelongColomn = (cubeItems[i].BelongColomn + 1) >= cubeItems.Count ? 0 : (cubeItems[i].BelongColomn + 1);
                m_SlideTransforms.Add(cubeItems[i].Transform);
                m_SlideOriginPositions.Add(cubeItems[i].GetMoveOriginPosition(Vector3.right, m_FirstPosition, m_LastPosition));
            }
            m_CanSlide = true;
        }

        private void DoUpOrder(List<CubeItem> cubeItems)
        {
            if (cubeItems == null || cubeItems.Count == 0)
            {
                return;
            }
            for (int i = 0; i < cubeItems.Count; i++)
            {
                cubeItems[i].BelongRow = (cubeItems[i].BelongRow - 1) < 0 ? m_Row - 1 : (cubeItems[i].BelongRow - 1);
                m_SlideTransforms.Add(cubeItems[i].Transform);
                m_SlideOriginPositions.Add(cubeItems[i].GetMoveOriginPosition(Vector3.forward, m_FirstPosition, m_LastPosition));
            }
            m_CanSlide = true;
        }

        private void DoDownOrder(List<CubeItem> cubeItems)
        {
            if (cubeItems == null || cubeItems.Count == 0)
            {
                return;
            }
            for (int i = 0; i < cubeItems.Count; i++)
            {
                cubeItems[i].BelongRow = (cubeItems[i].BelongRow + 1) >= cubeItems.Count ? 0 : (cubeItems[i].BelongRow + 1);
                m_SlideTransforms.Add(cubeItems[i].Transform);
                m_SlideOriginPositions.Add(cubeItems[i].GetMoveOriginPosition(Vector3.back, m_FirstPosition, m_LastPosition));
            }
            m_CanSlide = true;
        }

        private List<CubeItem> GetTargetRowList(CubeItem item)
        {
            List<CubeItem> m_RetList = new List<CubeItem>();
            for (int i = 0; i < m_CubeItems.Count; i++)
            {
                if (m_CubeItems[i].BelongRow == item.BelongRow)
                    m_RetList.Add(m_CubeItems[i]);
            }
            return m_RetList;
        }

        private List<CubeItem> GetTargetColomnList(CubeItem item)
        {
            List<CubeItem> m_RetList = new List<CubeItem>();
            for (int i = 0; i < m_CubeItems.Count; i++)
            {
                if (m_CubeItems[i].BelongColomn == item.BelongColomn)
                    m_RetList.Add(m_CubeItems[i]);
            }

            return m_RetList;
        }
        
        private void ClearCubeContainer()
        {
            if (m_CubeContainer.childCount>0)
            {
                for (int i = 0; i < m_CubeContainer.childCount; i++)
                {
                    GameObject.Destroy(m_CubeContainer.GetChild(i));
                }
            }
        }

        public override void OnRelease()
        {
            m_CubeItems.Clear();
            m_TargetList.Clear();
            m_SlideTransforms.Clear();
            m_SlideOriginPositions.Clear();
            Kernel.EventCenter.RemoveListen((int)SlideEventNames.CubeSlide, OnCubeSlide);

            ClearCubeContainer();
            m_CubeContainer = null;
            m_CurrentItem = null;
            m_CubeItemPool.Release(m_TempCubeItem);
        }

    }
}
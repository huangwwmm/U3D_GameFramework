using System;
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
        private int m_Row, m_Colomn = -1;
        private int m_CameraViewHeight = 20;

        private List<CubeItem> m_CubeItems;
        private Vector3 m_FirstPosition, m_LastPosition;
        private CubeItem m_CurrentItem;
        private List<CubeItem> m_TargetList;

        private UnityEngine.Transform m_CubeContainer;
        private UnityEngine.GameObject m_CubePrefab;
        
        private Vector3 m_SlideDirection = Vector3.zero;
        private List<UnityEngine.Transform> m_SlideTransforms;
        private List<Vector3> m_SlideOriginPositions;
        private bool m_CanSlide = false;
        private bool m_CanFirstTransformSlide = false;
        private bool m_CanOtherTransformSlide = false;
        private bool m_CanLastTransformSlide = false;
        private bool m_FirstSlideDone = false;
        private float m_FirstPercent = 0;
        private float m_Percent = 0;
        private float m_LastPercent = 0;
        private float m_MinSlideDistance = 0;
        private UnityEngine.Transform m_EdgeCubeItem;
        private Vector3 m_OldEdgeOriginPositon;
        private Vector3 m_NewEdgeOriginPositon;

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
            ClearCubeContainer();
            m_CubeItems.Clear();
            m_TargetList.Clear();
            m_Row = row;
            m_Colomn = colomn;

            int zoomMultiple = m_Row > m_Colomn ? m_Row : m_Colomn;
            m_MinSlideDistance = zoomMultiple - 2 == 0? GlobalConfig.ITEM_HALFSLIDEDISTANCE: GlobalConfig.ITEM_HALFSLIDEDISTANCE / (2 * (zoomMultiple - 2));
            m_CameraViewHeight = zoomMultiple * 10;
            Camera.main.transform.position = new Vector3(0, m_CameraViewHeight, 0.5f);
            Kernel.AssetManager.InstantiateGameObjectAsync((GF.Asset.AssetKey) AssetKey.Prefabs_Box001_prefab, (GF.Asset.AssetKey key, UnityEngine.Object tmpObj) =>
            {
                m_CubePrefab = tmpObj as GameObject;
                m_CubePrefab.AddComponent<BoxCollider>();
                InitCubes(m_CubePrefab);
            
            });

        }

        /// <summary>
        /// 初始化场景Cubes
        /// </summary>
        /// <param name="prefabCube"></param>
        /// <param name="row"></param>
        /// <param name="colomn"></param>
        private void InitCubes(UnityEngine.GameObject prefabCube)
        {
            Vector3 firstCubePosition = new Vector3(-GlobalConfig.ITEM_SIZE*0.5f*(m_Colomn-1), 0, GlobalConfig.ITEM_SIZE*0.5f*(m_Row-1));
            int index = 0;
            for (int i = 0; i < m_Row; i++)
            {
                for (int j = 0; j < m_Colomn; j++)
                {
                    Vector3 initPosition = new Vector3(firstCubePosition.x + GlobalConfig.ITEM_SIZE * j, 0,
                        firstCubePosition.z - GlobalConfig.ITEM_SIZE * i);
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
            if (m_CanSlide)
            {
                return;
            }
            SlideData slideData = (SlideData)data;
            if (CheckSlideCondition(slideData.startPosition, slideData.endPosition))
            {
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

                if (m_SlideDirection == Vector3.left)
                {
                    m_TargetList = GetTargetRowList(m_CurrentItem);
                    DoLeftOrder(m_TargetList);
                }
                else if (m_SlideDirection == Vector3.right)
                {
                    m_TargetList = GetTargetRowList(m_CurrentItem);
                    DoRightOrder(m_TargetList);
                }
                else if (m_SlideDirection == Vector3.forward)
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
        }

        /// <summary>
        /// 检查滑动满足条件
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        /// <returns></returns>
        private bool CheckSlideCondition(Vector2 startPosition, Vector2 endPosition)
        {
            if (Mathf.Abs(startPosition.x - endPosition.x) < m_MinSlideDistance && Mathf.Abs(startPosition.y - endPosition.y) < m_MinSlideDistance)
            {
                return false;
            }
            Vector2 targetDir = endPosition - startPosition;
            float angle = Vector2.Angle(Vector2.right, targetDir);
            angle = Mathf.Abs(90 - angle);
            if (Mathf.Abs(angle - 45) <= GlobalConfig.ANGLE_TOLERANCE)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查是否完成
        /// </summary>
        private void CheckFinish()
        {
            //Todo 检查是否完成拼图
        }

        /// <summary>
        /// 获取当前射线检测CubeItem
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
            #region SlideAnimation
            if (m_CanSlide)
            {
                if (m_SlideTransforms.Count==0|| m_SlideDirection==Vector3.zero)
                {
                    return;
                }

                int count = m_SlideTransforms.Count;
                if (m_FirstPercent<1)
                {
                    m_FirstPercent += Time.deltaTime / GlobalConfig.ANIMATION_SLIDETIME;
                    m_EdgeCubeItem.localPosition = m_OldEdgeOriginPositon + GlobalConfig.SLIDE_SPEED * m_SlideDirection * GameEnter.Instance.Curve.Evaluate(m_FirstPercent) * GlobalConfig.ITEM_SIZE;
                }
                else
                {
                    if (m_Percent<1)
                    {
                        if (m_EdgeCubeItem.gameObject.activeSelf)
                        {
                            m_EdgeCubeItem.gameObject.SetActive(false);
                        }
                        
                        m_Percent += Time.deltaTime / GlobalConfig.ANIMATION_SLIDETIME;
                        for (int i = 0; i < count; i++)
                        {
                            if (m_SlideTransforms[i]==m_EdgeCubeItem)
                            {
                                continue;
                            }
                            m_SlideTransforms[i].localPosition = m_SlideOriginPositions[i] + GlobalConfig.SLIDE_SPEED * m_SlideDirection * GameEnter.Instance.Curve.Evaluate(m_Percent) * GlobalConfig.ITEM_SIZE;
                        }
                    }
                    else
                    {
                        if (!m_EdgeCubeItem.gameObject.activeSelf)
                        {
                            m_EdgeCubeItem.gameObject.SetActive(true);
                        }
                        if (m_LastPercent<1)
                        {
                            m_LastPercent += Time.deltaTime / GlobalConfig.ANIMATION_SLIDETIME;
                            m_EdgeCubeItem.localPosition = m_NewEdgeOriginPositon + GlobalConfig.SLIDE_SPEED * m_SlideDirection * GameEnter.Instance.Curve.Evaluate(m_LastPercent) * GlobalConfig.ITEM_SIZE;
                        }
                        else
                        {
                            m_CanSlide = false;
                            m_FirstPercent = 0;
                            m_Percent = 0;
                            m_LastPercent = 0;
                            CheckFinish();
                        }
                    }
                }
            }
            #endregion

        }

        #region 执行方向行列计算

        private void DoLeftOrder(List<CubeItem> cubeItems)
        {
            if (cubeItems == null || cubeItems.Count == 0)
            {
                return;
            }
            int count = cubeItems.Count;
            for (int i = 0; i < count; i++)
            {
                if ((cubeItems[i].BelongColomn - 1) < 0)
                {
                    cubeItems[i].BelongColomn = m_Colomn - 1;
                    m_EdgeCubeItem = cubeItems[i].Transform;
                    m_OldEdgeOriginPositon = m_EdgeCubeItem.position;
                }
                else
                {
                    cubeItems[i].BelongColomn = (cubeItems[i].BelongColomn - 1);
                }
                m_SlideTransforms.Add(cubeItems[i].Transform);
                m_SlideOriginPositions.Add(cubeItems[i].Transform.position);
                m_NewEdgeOriginPositon = new Vector3(m_LastPosition.x+GlobalConfig.ITEM_SIZE,0,m_OldEdgeOriginPositon.z);
            }
            m_CanSlide = true;
        }

        private void DoRightOrder(List<CubeItem> cubeItems)
        {
            if (cubeItems == null || cubeItems.Count == 0)
            {
                return;
            }
            int count = cubeItems.Count;
            for (int i = 0; i < count; i++)
            {
                if ((cubeItems[i].BelongColomn + 1) >= count)
                {
                    cubeItems[i].BelongColomn = 0;
                    m_EdgeCubeItem = cubeItems[i].Transform;
                    m_OldEdgeOriginPositon = m_EdgeCubeItem.position;
                }
                else
                {
                    cubeItems[i].BelongColomn=(cubeItems[i].BelongColomn + 1);
                }
                m_SlideTransforms.Add(cubeItems[i].Transform);
                m_SlideOriginPositions.Add(cubeItems[i].Transform.position);
                m_NewEdgeOriginPositon = new Vector3(m_FirstPosition.x-GlobalConfig.ITEM_SIZE,0,m_OldEdgeOriginPositon.z);
            }
            m_CanSlide = true;
        }

        private void DoUpOrder(List<CubeItem> cubeItems)
        {
            if (cubeItems == null || cubeItems.Count == 0)
            {
                return;
            }
            int count = cubeItems.Count;
            for (int i = 0; i < count; i++)
            {
                if ((cubeItems[i].BelongRow - 1) < 0)
                {
                    cubeItems[i].BelongRow = m_Row - 1;
                    m_EdgeCubeItem = cubeItems[i].Transform;
                    m_OldEdgeOriginPositon = m_EdgeCubeItem.position;
                }
                else
                {
                    cubeItems[i].BelongRow =cubeItems[i].BelongRow - 1;
                }
                m_SlideTransforms.Add(cubeItems[i].Transform);
                m_SlideOriginPositions.Add(cubeItems[i].Transform.position);
                m_NewEdgeOriginPositon=new Vector3(m_OldEdgeOriginPositon.x,0,m_LastPosition.z-GlobalConfig.ITEM_SIZE);
            }
            m_CanSlide = true;
        }

        private void DoDownOrder(List<CubeItem> cubeItems)
        {
            if (cubeItems == null || cubeItems.Count == 0)
            {
                return;
            }
            int count = cubeItems.Count;
            for (int i = 0; i < count; i++)
            {
                if ((cubeItems[i].BelongRow + 1) >= cubeItems.Count)
                {
                    cubeItems[i].BelongRow = 0;
                    m_EdgeCubeItem = cubeItems[i].Transform;
                    m_OldEdgeOriginPositon = m_EdgeCubeItem.position;
                }
                else
                {
                    cubeItems[i].BelongRow=(cubeItems[i].BelongRow + 1);
                }
                m_SlideTransforms.Add(cubeItems[i].Transform);
                m_SlideOriginPositions.Add(cubeItems[i].Transform.position);
                m_NewEdgeOriginPositon=new Vector3(m_OldEdgeOriginPositon.x,0,m_FirstPosition.z+GlobalConfig.ITEM_SIZE);
            }
            m_CanSlide = true;
        }

        #endregion

        #region 获取当前目标行列

        private List<CubeItem> GetTargetRowList(CubeItem item)
        {
            List<CubeItem> m_RetList = new List<CubeItem>();
            int count = m_CubeItems.Count;
            for (int i = 0; i < count; i++)
            {
                if (m_CubeItems[i].BelongRow == item.BelongRow)
                    m_RetList.Add(m_CubeItems[i]);
            }
            return m_RetList;
        }

        private List<CubeItem> GetTargetColomnList(CubeItem item)
        {
            List<CubeItem> m_RetList = new List<CubeItem>();
            int count = m_CubeItems.Count;
            for (int i = 0; i < count; i++)
            {
                if (m_CubeItems[i].BelongColomn == item.BelongColomn)
                    m_RetList.Add(m_CubeItems[i]);
            }

            return m_RetList;
        }

        #endregion

        
        private void ClearCubeContainer()
        {
            if (m_CubeContainer.childCount>0)
            {
                int childCount = m_CubeContainer.childCount;
                for (int i = childCount-1; i>=0; i--)
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
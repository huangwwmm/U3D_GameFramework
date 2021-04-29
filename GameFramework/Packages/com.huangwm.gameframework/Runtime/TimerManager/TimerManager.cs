using System;
using System.Collections;
using System.Collections.Generic;
using GF.Common.Data;
using GF.Core.Behaviour;
using UnityEngine;
/// <summary>
/// 定时器管理类
/// </summary>

namespace GF.Timer
{
    public class TimerManager : BaseBehaviour
    {
        List<MyTimer> m_timeerList;
        int m_tagCount = 1000;
        private ObjectPool<MyTimer> m_objectPool;

         public TimerManager()
         {
             m_timeerList = new List<MyTimer>();
             m_objectPool = new ObjectPool<MyTimer>();
         }
        public override void OnUpdate(float deltaTime)
        {
            if (m_timeerList.Count == 0) return;
            MyTimer pCur = null;
            for (int i = m_timeerList.Count - 1; i >= 0; --i)
            {
                pCur = m_timeerList[i];
                if (pCur == null)
                {
                    m_timeerList.RemoveAt(i);
                    continue;
                }

                if (pCur.m_frame != null)
                {
                    pCur.m_frame(deltaTime);
                }
                
                pCur.tm += deltaTime;
                if (pCur.tm >= pCur.life)
                {
                    if (pCur.m_scheduler != null)
                    {
                        pCur.m_scheduler(deltaTime);
                    }

                    pCur.tm -= pCur.life;

                    if (pCur.count != -1)
                    {
                        --pCur.count;
                        if (pCur.count == 0)
                        {
                            m_objectPool.Release(pCur);
                            m_timeerList.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public override void OnRelease()
        {
            for (int i = 0; i < m_timeerList.Count; i++)
            {
                m_objectPool.Release(m_timeerList[i]);
            }
            m_objectPool = null;
            m_timeerList.Clear();
        }

        #region 注册
        /// <summary>
        /// 延时调度
        /// 注册一个 仅调度一次的定时器
        /// </summary>
        /// <param name="delay">等待时间 单位秒</param>
        /// <param name="on_time_func">完成的时候的调度方法</param>
        /// <param name="on_time_func">每帧执行方法</param>
        /// <returns>每个调度器的标签值</returns>
        public int scheduleOnce(float delay, Action<float> on_time_func)
        {
            return schedule(delay, on_time_func,null, 1);
        }


        /// <summary>
        /// 注册一个定时器
        /// </summary>
        /// <param name="interval">间隔 单位秒</param>
        /// <param name="on_time_func">完成的时候的调度方法</param>
        /// <param name="on_time_func">每帧执行方法</param>
        /// <param name="count">调度次数  如果为-1 则永远调度</param>
        /// <returns>每个调度器的标签值</returns>
        public int schedule(float interval, Action<float> on_time_func,Action<float> per_frame_func = null,  int count = -1)
        {
            ++m_tagCount;
            var scheduler = m_objectPool.Alloc();
            scheduler.tm = 0;
            scheduler.life = interval;
            scheduler.m_scheduler = on_time_func;
            scheduler.m_frame = per_frame_func;
            scheduler.count = count;
            scheduler.tag = m_tagCount;
            m_timeerList.Add(scheduler);
            return m_tagCount;
        }
        #endregion

        #region 获取
        private MyTimer GetTimeById(int tag)
        {
            
            foreach(var scheduler in m_timeerList)
            {
                if(tag == scheduler.tag)
                {
                    return scheduler;
                }
            }

            return null;
        }
        

        #endregion

        #region 移除
        public void RemoveById(int index)
        {
            if (index <= 0)
            {
                Debug.LogError("remove timer id must bigger than 0 : " + index);
            }
            for (int i = 0; i < m_timeerList.Count; i++)
            {
                if(index == m_timeerList[i].tag)
                {
                    m_objectPool.Release(m_timeerList[i]);
                    m_timeerList.RemoveAt(i);
                    break;
                }
            }
        }
   
        #endregion
    }
}

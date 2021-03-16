using System.Collections.Generic;
using System;
using GF.Common.Data;
using GF.Common.Collection;
using GF.Common.Debug;
using GF.Core.Behaviour;

namespace GF.Core
{
    public class ObjectPoolManager : BaseBehaviour
    {
        /// <summary>
        /// 一个Pool经过多少毫秒不被使用,就认为它不需要了
        /// </summary>
        private const long AUTO_GC_MILLISECONDS = 1000 * 60 * 5;

        /// <summary>
        /// 持有池的弱引用
        /// </summary>
        private Dictionary<string, WeakReference> m_WeakObjectPools;
        /// <summary>
        /// 持有池的强引用
        /// </summary>
        private BetterList<IObjectPool> m_StrongObjectPools;

        internal ObjectPoolManager(KernelInitializeData initializeData) 
            : base("ObjectPoolManager", (int)BehaviourPriority.ObjectPoolManager, BehaviourGroup.Default.ToString())
        {
            m_WeakObjectPools = new Dictionary<string, WeakReference>();
            m_StrongObjectPools = new BetterList<IObjectPool>();
        }

        public T Alloc<T>() where T : class
            , IObjectPoolItem
            , new()
        {
            return GetPool<T>(true).Alloc();
        }

        public void Release<T>(T obj) where T : class
            , IObjectPoolItem
            , new()
        {
            ObjectPool<T> pool = GetPool<T>(false);
            if (pool != null)
            {
                pool.Release(obj);
            }
            else
            {
                MDebug.LogError("Pool"
                    , $"Release object<{typeof(T).FullName}> failed. Because ObjectPool not exist, maybe has memory leak.");
            }
        }

        public void Release(object obj)
        {
            Type type = obj.GetType();
            string key = TypeToKey(type);
            if (m_WeakObjectPools.TryGetValue(key, out WeakReference weakReference)
                && weakReference.IsAlive)
            {
                ((IObjectPool)weakReference.Target).Release(obj);
            }
            else
            {
                MDebug.LogError("Pool"
                    , $"Release object<{type.FullName}> failed. Because ObjectPool not exist, maybe has memory leak.");
            }
        }

        public ObjectPool<T> GetPool<T>(bool canCreate = true) where T : class
            , IObjectPoolItem
            , new()
        {
            Type type = typeof(T);
            string key = TypeToKey(type);
            if (!m_WeakObjectPools.TryGetValue(key, out WeakReference weakReference)
                || !weakReference.IsAlive)
            {
                if (!canCreate)
                {
                    return null;
                }

                if (weakReference == null)
                {
                    MDebug.Log("Pool", $"Create new ObjectPool<{type.FullName}>.");
                }
                else
                {
                    MDebug.Log("Pool", $"ObjectPool<{type.FullName}> has been GC. Will create new ObjectPool");
                }
                ObjectPool<T> pool = new ObjectPool<T>();
                m_StrongObjectPools.Add(pool);
                weakReference = new WeakReference(pool);
                m_WeakObjectPools[key] = weakReference;
            }

            return (ObjectPool<T>)weakReference.Target;
        }

        public string TypeToKey(Type type)
        {
            return $"{type.FullName}, {type.Assembly.FullName}";
        }

        public override void OnLateUpdate(float deltaTime)
        {
            long currentTime = MDebug.GetMillisecondsSinceStartup();
            for (int iPool = m_StrongObjectPools.Count - 1; iPool >= 0; iPool--)
            {
                IObjectPool iterPool = m_StrongObjectPools[iPool];
                if (iterPool.GetUsingCount() == 0
                    && currentTime - iterPool.GetLastUsingTime() > AUTO_GC_MILLISECONDS)
                {
                    MDebug.Log("Pool"
                        , $"ObjectPool<{iterPool.GetObjectType().FullName}> will be GC.");
                    m_StrongObjectPools.RemoveAt(iPool);
                    m_WeakObjectPools.Remove(TypeToKey(iterPool.GetObjectType()));
                }
            }
        }
    }
}
using GF.Common.Debug;
using GF.Common.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Common.Data
{
    public interface IObjectPoolItem
    {
        /// <summary>
        /// 对象被创建时触发
        /// </summary>
        void OnAlloc();
        /// <summary>
        /// 对象被放回到池里时触发
        /// </summary>
        void OnRelease();
    }

    public interface IObjectPool
    {
        object Get();
        void Release(object obj);

        Type GetObjectType();
        /// <summary>
        /// 被使用中的数量
        /// </summary>
        int GetUsingCount();
        /// <summary>
        /// 上一次被用的时间，单位毫秒
        /// </summary>
        long GetLastUsingTime();
    }

    public class ObjectPool<T> : IObjectPool where T : class
        , IObjectPoolItem
        , new()
    {
        /// <summary>
        /// 魔法数字
        /// 为了防止有人从池里拿了Object之后忘记还回来的会做定期检测
        /// </summary>
        private const long DEFAULT_MEMORY_LEAK_MILLISECONDS = 1000 * 60 * 5;

        /// <summary>
        /// 池中的对象
        /// </summary>
        private Stack<T> m_Stack;

        /// <summary>
        /// 通过这个对象池创建出来的对象数量
        /// </summary>
        private int m_AllCount;
        /// <summary>
        /// 使用中的对象数量
        /// </summary>
        private int m_UsingCount;
        private long m_LastUsingTime;

#if GF_DEBUG
        private List<UsingObject> m_Using;

        /// <summary>
        /// 一个对象被使用的时间超过这个值，我就认为它发生内存泄漏
        /// </summary>
        private long m_MemoryLeakMilliseconds;
#endif

        public ObjectPool(int capacity = 0
            , long memoryLeakMilliseconds = DEFAULT_MEMORY_LEAK_MILLISECONDS)
        {
            m_Stack = new Stack<T>();
            m_AllCount = 0;
            m_UsingCount = 0;
            m_LastUsingTime = 0;

#if GF_DEBUG
            m_Using = new List<UsingObject>();
            m_MemoryLeakMilliseconds = memoryLeakMilliseconds;
#endif

            for (int iObject = 0; iObject < capacity; iObject++)
            {
                T element = new T();
                element.OnAlloc();
                m_Stack.Push(element);

                m_AllCount++;
            }
        }

        ~ObjectPool()
        {
            MDebug.Assert(m_UsingCount == 0
                , "Pool"
                , $"ObjectPool<{typeof(T).FullName}> has memory leak!\n{ToString()}");

#if GF_DEBUG
            for (int iObject = 0; iObject < m_Using.Count; iObject++)
            {
                UsingObject usingObject = m_Using[iObject];
                MDebug.LogError("Pool"
                    , $"Object({usingObject.ToString()}) in ObjectPool<{typeof(T).FullName}> has memory leak!");
            }
            m_Using.Clear();
#endif

            while (m_Stack.Count > 0)
            {
                m_Stack.Pop().OnRelease();
            }
            m_Stack.Clear();

            MDebug.Log("Pool"
                , $"ObjectPool<{typeof(T).FullName}> destroyed.");
        }

        public T Alloc()
        {
            m_LastUsingTime = MDebug.GetMillisecondsSinceStartup();

            T element = default;
            if (m_Stack.Count == 0)
            {
#if GF_DEBUG
                CheckMemoryLeak();
#endif

                element = new T();
                element.OnAlloc();

                m_AllCount++;
            }
            else
            {
                element = m_Stack.Pop();
            }

#if GF_DEBUG
            m_Using.Add(new UsingObject(element));
#endif

            m_UsingCount++;
            return element;
        }

        public void Release(T element)
        {
            m_LastUsingTime = MDebug.GetMillisecondsSinceStartup();

#if GF_DEBUG
            MDebug.Assert(element != null, "Pool", $"element != null");

            bool removedFromUsing = false;
            for (int iObject = 0; iObject < m_Using.Count; iObject++)
            {
                UsingObject usingObject = m_Using[iObject];
                if (usingObject.Obj.Equals(element))
                {
                    removedFromUsing = true;
                    m_Using.RemoveAt(iObject);
                    break;
                }
            }

            if (!removedFromUsing)
            {
                MDebug.LogError("Pool"
                    , $"Object({element.ToString()}) not alloc by ObjectPool<{typeof(T).FullName}>!");
            }
#endif

            element.OnRelease();
            m_Stack.Push(element);

            m_UsingCount--;
        }

#if GF_DEBUG
        public void CheckMemoryLeak()
        {
            long currentTime = MDebug.GetMillisecondsSinceStartup();
            for (int iObject = 0; iObject < m_Using.Count; iObject++)
            {
                UsingObject usingObject = m_Using[iObject];
                if (currentTime - usingObject.WhenBeUse > m_MemoryLeakMilliseconds)
                {
                    MDebug.LogWarning("Pool"
                        , $"Object in ObjectPool<{typeof(T).FullName}> be used {(currentTime - usingObject.WhenBeUse) * 0.001:F2} seconds ago, maybe memory leak!\n({usingObject.ToString()})");
                }
            }
        }
#endif

        public override string ToString()
        {
            System.Text.StringBuilder stringBuilder = StringUtility.AllocStringBuilder();
            stringBuilder.Append($"ObjectPool<{typeof(T).FullName}> alloc {m_AllCount} objects. ")
                .Append($"{m_UsingCount} obejcts being used");
#if GF_DEBUG
            long currentTime = MDebug.GetMillisecondsSinceStartup();
            for (int iObject = 0; iObject < m_Using.Count; iObject++)
            {
                UsingObject usingObject = m_Using[iObject];
                float beUsedTime = (currentTime - usingObject.WhenBeUse) * 0.001f;
                stringBuilder.Append($"\nObject be used {beUsedTime:F2} seconds. ({usingObject.ToString()})");
            }
#endif
            return StringUtility.ReleaseStringBuilder(stringBuilder);
        }

        public int GetUsingCount()
        {
            return m_UsingCount;
        }

        public long GetLastUsingTime()
        {
            return m_LastUsingTime;
        }

        public Type GetObjectType()
        {
            return typeof(T);
        }

        object IObjectPool.Get()
        {
            return Alloc();
        }

        public void Release(object obj)
        {
            Release((T)obj);
        }

#if GF_DEBUG
        private struct UsingObject
        {
            public T Obj;
            /// <summary>
            /// 自游戏启动后多少毫秒被用
            /// </summary>
            public long WhenBeUse;

            public UsingObject(T element)
            {
                Obj = element;
                WhenBeUse = MDebug.GetMillisecondsSinceStartup();
            }
        }
#endif
    }
}
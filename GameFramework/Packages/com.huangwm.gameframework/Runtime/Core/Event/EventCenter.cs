using GF.Common.Collection;
using GF.Common.Debug;
using GF.Common.Data;
using GF.Common.Utility;
using GF.Core.Behaviour;
using System;
using System.Collections.Generic;

namespace GF.Core.Event
{
    public class EventCenter : BaseBehaviour
    {
        /// <summary>
        /// 监听者
        /// </summary>
        private EventFunction[] m_Listeners;
        private string[] m_EventIdToName;

        private ObjectPool<EventItem> m_EventItemPool;
        private BetterQueue<EventItem> m_MajorEventCache;
        private BetterQueue<EventItem>[] m_MajorEvents;

        public EventCenter(List<Type> eventTypes
            , string name
            , int priority
            , string groupName)
            : base(name, priority, groupName)
        {
            List<Array> eventEnumValues = new List<Array>();
            int maxEventID = int.MinValue;
            for (int iEventType = 0; iEventType < eventTypes.Count; iEventType++)
            {
                Array enumValues = eventTypes[iEventType].GetEnumValues();
                maxEventID = Math.Max(maxEventID, (int)enumValues.GetValue(enumValues.Length - 1));
                eventEnumValues.Add(enumValues);
            }

            m_Listeners = new EventFunction[maxEventID + 1];
            m_EventIdToName = new string[maxEventID + 1];
            for (int iEventType = 0; iEventType < eventEnumValues.Count; iEventType++)
            {
                Array enumValues = eventEnumValues[iEventType];
                for (int iEnum = 0; iEnum < enumValues.Length; iEnum++)
                {
                    object iterEnum = enumValues.GetValue(iEnum);
                    m_EventIdToName[(int)iterEnum] = iterEnum.ToString();
                }
            }

            System.Text.StringBuilder stringBuilder = StringUtility.AllocStringBuilder()
                .Append("Events id to name:\n");
            for (int iEvent = 0; iEvent < m_EventIdToName.Length; iEvent++)
            {
                stringBuilder.Append(iEvent).Append(", ").Append(m_EventIdToName[iEvent]).Append('\n');
            }
            MDebug.Log("EventCenter", StringUtility.ReleaseStringBuilder(stringBuilder));

            m_EventItemPool = new ObjectPool<EventItem>(32);
            m_MajorEventCache = new BetterQueue<EventItem>();
            m_MajorEvents = new BetterQueue<EventItem>[(int)UpdateMethod.Count];
            for (int iMajor = 0; iMajor < m_MajorEvents.Length; iMajor++)
            {
                m_MajorEvents[iMajor] = new BetterQueue<EventItem>();
            }
        }

        internal EventCenter(KernelInitializeData initializeData)
            : this(initializeData.EventTypes
                  , "EventCenter"
                  , (int)BehaviourPriority.EventCenter
                  , BehaviourGroup.Default.ToString())
        {
        }

        public void AddListen(int eventId, EventFunction listener)
        {
            m_Listeners[eventId] += listener;
        }

        public void RemoveListen(int eventId, EventFunction listener)
        {
            m_Listeners[eventId] -= listener;
        }

        public T GetUserData<T>() where T : class
            , IPoolUserData
            , new()
        {
            return Kernel.ObjectPool.Alloc<T>();
        }

        /// <summary>
        /// 立刻发送一个Event
        /// 使用<see cref="GetUserData"/>来获取<see cref="IUserData"/>
        /// </summary>
        public void SendImmediately(int eventId, IUserData userData)
        {
            SafeInvoke(eventId, true, userData);
        }

        /// <summary>
        /// 发送一个重要的异步Event，这个Event必定在下一帧发给Listener
        /// TODO 之后还会写一个 SendMinorAsync 用来发送不重要的Event
        /// </summary>
        public void SendMajorAsync(int eventId
            , IUserData userData
            , UpdateMethod updateMethod)
        {
            lock (m_MajorEventCache)
            {
                m_MajorEventCache
                    .Enqueue(m_EventItemPool.Alloc().SetData(eventId, userData, updateMethod));
            }
        }

        public override void OnFixedUpdate(float deltaTime)
        {
            lock (m_MajorEventCache)
            {
                while (m_MajorEventCache.Count > 0)
                {
                    EventItem item = m_MajorEventCache.Dequeue();
                    m_MajorEvents[(int)item.UpdateMethod].Enqueue(item);
                }
            }

            SendImmediatelyFromQueue(m_MajorEvents[(int)UpdateMethod.FixedUpdate]);
        }

        public override void OnUpdate(float deltaTime)
        {
            SendImmediatelyFromQueue(m_MajorEvents[(int)UpdateMethod.Update]);
        }

        public override void OnLateUpdate(float deltaTime)
        {
            SendImmediatelyFromQueue(m_MajorEvents[(int)UpdateMethod.LateUpdate]);
        }

        /// <summary>
        /// 立即发送队列中的Event
        /// </summary>
        private void SendImmediatelyFromQueue(BetterQueue<EventItem> events)
        {
            while (events.Count > 0)
            {
                EventItem eventItem = events.Dequeue();
                SafeInvoke(eventItem.EventID, false, eventItem.UserData);
                m_EventItemPool.Release(eventItem);
            }
        }

        private void SafeInvoke(int eventId, bool isImmediately, IUserData userData)
        {
            EventFunction listener = m_Listeners[eventId];
            if (listener == null)
            {
                MDebug.LogVerbose("EventCenter", $"Event ({m_EventIdToName[eventId]}) not have listener.");
                if (userData is IObjectPoolItem poolItem)
                {
                    Kernel.ObjectPool.Release(poolItem);
                }
                return;
            }

            try
            {
                MDebug.LogVerbose("EventCenter"
                    , $"Event ({m_EventIdToName[eventId]}) begin invoke.");

                listener.Invoke(eventId, isImmediately, userData);
                if (userData is IObjectPoolItem poolItem)
                {
                    Kernel.ObjectPool.Release(poolItem);
                }

                MDebug.LogVerbose("EventCenter"
                    , $"Event ({m_EventIdToName[eventId]}) end invoke.");
            }
            catch (Exception e)
            {
                MDebug.LogError("EventCenter"
                   , $"Event ({m_EventIdToName[eventId]}) invoke Exception:\n{e.ToString()}");
            }
        }

        private class EventItem : IObjectPoolItem
        {
            public int EventID;
            public IUserData UserData;
            public UpdateMethod UpdateMethod;

            public EventItem SetData(int eventID, IUserData userData, UpdateMethod updateMethod)
            {
                EventID = eventID;
                UserData = userData;
                UpdateMethod = updateMethod;
                return this;
            }

            public void OnAlloc()
            {
            }

            public void OnRelease()
            {
                UserData = null;
            }
        }
    }
}
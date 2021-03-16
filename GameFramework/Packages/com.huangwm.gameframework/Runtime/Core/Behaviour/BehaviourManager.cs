using GF.Common.Debug;
using GF.Common.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Core.Behaviour
{
    public class BehaviourManager : MonoBehaviour
    {
        /// <summary>
        /// 按优先级排序
        /// </summary>
        private BehaviourRBTree m_BehaviourOrdered;
        /// <summary>
        /// 用于遍历
        /// </summary>
        private List<BaseBehaviour> m_BehavioursForTraverse;
        private List<BaseBehaviour> m_AddBehavioursCache;
        private List<BaseBehaviour> m_RemoveBehavioursCache;

        private ObjectPool<ParallelUpdateItem> m_ParallelItemPool;
        private List<ParallelUpdateItem> m_ParallelItemsCache;

        private ObjectPool<TaskUpdateItem> m_TaskItemPool;
        private List<TaskUpdateItem> m_TaskItemsCache;

        /// <summary>
        /// <see cref="BeginFrame"/>是在FixedUpdate中执行的，FixedUpdate一帧可能执行多次，所以这里做个保护
        /// </summary>
        private int m_LastBeginFrame;

        internal void AddBehavior(BaseBehaviour behaviour)
        {
            if (!behaviour.IsAlive())
            {
                m_BehaviourOrdered.Add(behaviour);
                m_AddBehavioursCache.Add(behaviour);
            }
            else
            {
                MDebug.LogError("Core"
                    , $"AddBehavior ({behaviour.GetName()}) failed. It already alive.");
            }
        }

        internal void RemoveBehavior(BaseBehaviour behaviour)
        {
            if (behaviour.IsAlive())
            {
                behaviour.SetEnable(false);
                behaviour.SetAlive(false);
                m_BehaviourOrdered.DeleteByIndex(m_BehaviourOrdered.GetIndexByKey(behaviour));
                m_RemoveBehavioursCache.Add(behaviour);
            }
            else
            {
                MDebug.LogError("Core"
                    , $"RemoveBehavior ({behaviour.GetName()}) failed. It doesn't alive.");
            }
        }

        protected void Awake()
        {
            DontDestroyOnLoad(this);

            m_BehaviourOrdered = new BehaviourRBTree();
            m_BehavioursForTraverse = new List<BaseBehaviour>();
            m_AddBehavioursCache = new List<BaseBehaviour>();
            m_RemoveBehavioursCache = new List<BaseBehaviour>();

            m_ParallelItemPool = new ObjectPool<ParallelUpdateItem>();
            m_ParallelItemsCache = new List<ParallelUpdateItem>();

            m_TaskItemPool = new ObjectPool<TaskUpdateItem>();
            m_TaskItemsCache = new List<TaskUpdateItem>();

            m_LastBeginFrame = int.MinValue;
        }

        protected void FixedUpdate()
        {
            BeginFrame();

            float deltaTime = Time.fixedDeltaTime;

            for (int iBehaviour = 0; iBehaviour < m_BehavioursForTraverse.Count; iBehaviour++)
            {
                BaseBehaviour iterBehaviour = m_BehavioursForTraverse[iBehaviour];
                if (!iterBehaviour.CanUpdate())
                {
                    continue;
                }

                try
                {
                    MDebug.LogVerbose("Core"
                        , $"Before execute {iterBehaviour.GetName()}.OnFixedUpdate");

                    iterBehaviour.OnFixedUpdate(deltaTime);

                    MDebug.LogVerbose("Core"
                        , $"After execute {iterBehaviour.GetName()}.OnFixedUpdate");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Core"
                        , $"Execute {iterBehaviour.GetName()}.OnFixedUpdate Exception:{e.ToString()}");
                }
            }
        }

        protected void Update()
        {
            // 这里也要调用一次的原因是，当前帧可能没执行FixedUpdate
            BeginFrame();

            float deltaTime = Time.deltaTime;
            int frameCount = Time.frameCount;
            ParallelUpdate(deltaTime);

            for (int iBehaviour = 0; iBehaviour < m_BehavioursForTraverse.Count; iBehaviour++)
            {
                BaseBehaviour iterBehaviour = m_BehavioursForTraverse[iBehaviour];
                if (!iterBehaviour.CanUpdate())
                {
                    continue;
                }

                float behaviourDeltaTime;
                if (iterBehaviour.HasFeature(FeatureFlag.UpdateFrequency))
                {
                    try
                    {
                        if (!iterBehaviour.ControlUpdateFrequency(out behaviourDeltaTime, deltaTime, frameCount))
                        {
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        MDebug.LogError("Core"
                            , $"Execute {iterBehaviour.GetName()}.ControlUpdateFrequency Exception:{e.ToString()}");
                        continue;
                    }
                }
                else
                {
                    behaviourDeltaTime = deltaTime;
                }

                try
                {
                    MDebug.LogVerbose("Core"
                        , $"Before execute {iterBehaviour.GetName()}.OnUpdate");

                    iterBehaviour.OnUpdate(behaviourDeltaTime);

                    MDebug.LogVerbose("Core"
                        , $"After execute {iterBehaviour.GetName()}.OnUpdate");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Core"
                        , $"Execute {iterBehaviour.GetName()}.OnUpdate Exception:{e.ToString()}");
                }
            }
        }

        protected void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            int frameCount = Time.frameCount;

            for (int iBehaviour = 0; iBehaviour < m_BehavioursForTraverse.Count; iBehaviour++)
            {
                BaseBehaviour iterBehaviour = m_BehavioursForTraverse[iBehaviour];
                if (!iterBehaviour.CanUpdate())
                {
                    continue;
                }

                float behaviourDeltaTime;
                if (iterBehaviour.HasFeature(FeatureFlag.LateUpdateFrequency))
                {
                    try
                    {
                        if (!iterBehaviour.ControlLateUpdateFrequency(out behaviourDeltaTime, deltaTime, frameCount))
                        {
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        MDebug.LogError("Core"
                            , $"Execute {iterBehaviour.GetName()}.ControlLateUpdateFrequency Exception:{e.ToString()}");
                        continue;
                    }
                }
                else
                {
                    behaviourDeltaTime = deltaTime;
                }

                try
                {
                    MDebug.LogVerbose("Core"
                       , $"Before execute {iterBehaviour.GetName()}.OnLateUpdate");

                    iterBehaviour.OnLateUpdate(deltaTime);

                    MDebug.LogVerbose("Core"
                       , $"After execute {iterBehaviour.GetName()}.OnLateUpdate");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Core"
                        , $"Execute {iterBehaviour.GetName()}.OnLateUpdate Exception:{e.ToString()}");
                }
            }

            EndFrame();
        }

        protected void OnApplicationQuit()
        {
            WaitTaskUpdate();
            CollectionBehavioursForTraverse();

            for (int iBehaviour = 0; iBehaviour < m_BehavioursForTraverse.Count; iBehaviour++)
            {
                BaseBehaviour behaviour = m_BehavioursForTraverse[iBehaviour];
                behaviour.SetEnable(false);
                DisableBehaviour(behaviour);
            }

            for (int iBehaviour = 0; iBehaviour < m_BehavioursForTraverse.Count; iBehaviour++)
            {
                BaseBehaviour behaviour = m_BehavioursForTraverse[iBehaviour];
                behaviour.SetAlive(false);
                ReleaseBehaviour(behaviour);
            }

            m_BehavioursForTraverse.Clear();
            m_AddBehavioursCache.Clear();
            m_RemoveBehavioursCache.Clear();
            m_BehaviourOrdered.Clear();
        }

        private void TaskUpdate(float deltaTime)
        {
            MDebug.Assert(m_TaskItemsCache.Count == 0, "Core", "m_TaskItemsCache.Count == 0");

            for (int iBehaviour = 0; iBehaviour < m_BehavioursForTraverse.Count; iBehaviour++)
            {
                BaseBehaviour iterBehaviour = m_BehavioursForTraverse[iBehaviour];
                if (!iterBehaviour.CanUpdate()
                    || !iterBehaviour.HasFeature(FeatureFlag.TaskUpdate))
                {
                    continue;
                }

                TaskUpdateItem iterTaskItem = m_TaskItemPool.Alloc()
                    .SetData(iterBehaviour, deltaTime);
                m_TaskItemsCache.Add(iterTaskItem);

                try
                {
                    MDebug.LogVerbose("Core"
                        , $"Before execute {iterBehaviour.GetName()}.OnBeforeTastUpdate");

                    iterTaskItem.Input = iterBehaviour.OnBeforeTastUpdate();

                    MDebug.LogVerbose("Core"
                        , $"After execute {iterBehaviour.GetName()}.OnBeforeTastUpdate");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Core"
                        , $"Execute {iterBehaviour.GetName()}.OnBeforeTastUpdate Exception:{e.ToString()}");
                }

                System.Threading.ThreadPool.QueueUserWorkItem(iterBehaviour.DoTaskUpdate_Thread, iterTaskItem);
            }
        }

        private void ParallelUpdate(float deltaTime)
        {
            MDebug.Assert(m_ParallelItemsCache.Count == 0, "Core", "m_ParallelTasksCache.Count == 0");

            string lastGroupName = string.Empty;
            for (int iBehaviour = 0; iBehaviour < m_BehavioursForTraverse.Count; iBehaviour++)
            {
                BaseBehaviour iterBehaviour = m_BehavioursForTraverse[iBehaviour];
                if (!iterBehaviour.CanUpdate()
                    || !iterBehaviour.HasFeature(FeatureFlag.ParallelUpdate))
                {
                    continue;
                }

                string groupName = iterBehaviour.GetGroup();
                if (groupName != lastGroupName)
                {
                    ExecuteParallelUpdateGroup();
                }

                lastGroupName = groupName;
                m_ParallelItemsCache.Add(m_ParallelItemPool.Alloc()
                    .SetData(iterBehaviour, deltaTime));
            }

            ExecuteParallelUpdateGroup();
        }

        private void ExecuteParallelUpdateGroup()
        {
            if (m_ParallelItemsCache.Count == 0)
            {
                return;
            }

            for (int iBehaviour = 0; iBehaviour < m_ParallelItemsCache.Count; iBehaviour++)
            {
                ParallelUpdateItem iterTask = m_ParallelItemsCache[iBehaviour];
                BaseBehaviour iterBehaviour = iterTask.Behaviour;

                try
                {
                    MDebug.LogVerbose("Core"
                        , $"Before execute {iterBehaviour.GetName()}.OnBeforeParallelUpdate");

                    iterTask.Input = iterBehaviour.OnBeforeParallelUpdate(iterTask.DeltaTime);

                    MDebug.LogVerbose("Core"
                        , $"After execute {iterBehaviour.GetName()}.OnBeforeParallelUpdate");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Core"
                        , $"Execute {iterBehaviour.GetName()}.OnBeforeParallelUpdate Exception:{e.ToString()}");
                }
            }

            System.Threading.Tasks.Parallel.For(0, m_ParallelItemsCache.Count, (iBehaviour) =>
            {
                ParallelUpdateItem iterTask = m_ParallelItemsCache[iBehaviour];
                BaseBehaviour iterBehaviour = iterTask.Behaviour;

                try
                {
                    MDebug.LogVerbose("Core"
                        , $"Before execute {iterBehaviour.GetName()}.OnParallelUpdate_Thread");

                    iterTask.Output = iterBehaviour.OnParallelUpdate_Thread(iterTask.Input, iterTask.DeltaTime);

                    MDebug.LogVerbose("Core"
                        , $"After execute {iterBehaviour.GetName()}.OnParallelUpdate_Thread");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Core"
                        , $"Execute {iterBehaviour.GetName()}.OnParallelUpdate_Thread Exception:{e.ToString()}");
                }
            });

            for (int iBehaviour = 0; iBehaviour < m_ParallelItemsCache.Count; iBehaviour++)
            {
                ParallelUpdateItem iterTask = m_ParallelItemsCache[iBehaviour];
                BaseBehaviour iterBehaviour = iterTask.Behaviour;

                try
                {
                    MDebug.LogVerbose("Core"
                        , $"Before execute {iterBehaviour.GetName()}.OnAfterParallelUpdate");

                    iterBehaviour.OnAfterParallelUpdate(iterTask.Output, iterTask.DeltaTime);

                    MDebug.LogVerbose("Core"
                        , $"After execute {iterBehaviour.GetName()}.OnAfterParallelUpdate");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Core"
                        , $"Execute {iterBehaviour.GetName()}.OnAfterParallelUpdate Exception:{e.ToString()}");
                }

                m_ParallelItemPool.Release(iterTask);
            }

            m_ParallelItemsCache.Clear();
        }

        private void BeginFrame()
        {
            if (Time.frameCount <= m_LastBeginFrame)
            {
                return;
            }
            m_LastBeginFrame = Time.frameCount;

            MDebug.LogVerbose("Core", "BeginFrame " + m_LastBeginFrame);
            WaitTaskUpdate();

            CollectionBehavioursForTraverse();

            #region Handle Initialize
            for (int iBehaviour = 0; iBehaviour < m_AddBehavioursCache.Count; iBehaviour++)
            {
                BaseBehaviour behaviour = m_AddBehavioursCache[iBehaviour];

                try
                {
                    MDebug.LogVerbose("Core"
                        , $"Before execute {behaviour.GetName()}.OnInitialize");

                    behaviour.OnInitialize();

                    MDebug.LogVerbose("Core"
                        , $"After execute {behaviour.GetName()}.OnInitialize");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Core"
                        , $"Execute {behaviour.GetName()}.OnInitialize Exception:{e.ToString()}");
                }
                behaviour.SetAlive(true);
            }
            m_AddBehavioursCache.Clear();
            #endregion 

            #region Handle Enable
            for (int iBehaviour = 0; iBehaviour < m_BehavioursForTraverse.Count; iBehaviour++)
            {
                BaseBehaviour behaviour = m_BehavioursForTraverse[iBehaviour];
                if (behaviour.IsEnable() && !behaviour.IsLastEnable())
                {
                    try
                    {
                        MDebug.LogVerbose("Core"
                            , $"Before execute {behaviour.GetName()}.OnEnable");

                        behaviour.OnEnable();

                        MDebug.LogVerbose("Core"
                            , $"After execute {behaviour.GetName()}.OnEnable");
                    }
                    catch (Exception e)
                    {
                        MDebug.LogError("Core"
                            , $"Execute {behaviour.GetName()}.OnEnable Exception:{e.ToString()}");
                    }
                    finally
                    {
                        behaviour.SetLastEnable(true);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 收集用于遍历的<see cref="BaseBehaviour"/>到<see cref="m_BehavioursForTraverse"/>
        /// </summary>
        private void CollectionBehavioursForTraverse()
        {
            MDebug.Assert(m_BehavioursForTraverse.Count == 0, "Core", "m_BehavioursForTraverse.Count == 0");
#if GF_DEBUG
            int lastPriority = int.MinValue;
            int lastInstanceID = int.MinValue;
#endif
            foreach (BaseBehaviour behaviour in m_BehaviourOrdered)
            {
                m_BehavioursForTraverse.Add(behaviour);
#if GF_DEBUG
                int priority = behaviour.GetPriority();
                int instanceID = behaviour.GetInstanceID();
                MDebug.Assert(priority > lastPriority || (priority == lastPriority && instanceID > lastInstanceID)
                    , "Core"
                    , "priority > lastPriority || (priority == lastPriority && instanceID > lastInstanceID)");
                lastPriority = priority;
                lastInstanceID = instanceID;
#endif
            }
        }

        /// <summary>
        /// 等待所有的<see cref="TaskUpdate"/>完成
        /// </summary>
        private void WaitTaskUpdate()
        {
            for (int iTask = 0; iTask < m_TaskItemsCache.Count; iTask++)
            {
                TaskUpdateItem taskItem = m_TaskItemsCache[iTask];
                taskItem.ManualResetEvent.WaitOne();

                BaseBehaviour iterBehaviour = taskItem.Behaviour;
                try
                {
                    MDebug.LogVerbose("Core"
                        , $"Before execute {iterBehaviour.GetName()}.OnAfterTaskUpdate");

                    iterBehaviour.OnAfterTaskUpdate(taskItem.Output);

                    MDebug.LogVerbose("Core"
                        , $"After execute {iterBehaviour.GetName()}.OnAfterTaskUpdate");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Core"
                        , $"Execute {iterBehaviour.GetName()}.OnAfterTaskUpdate Exception:{e.ToString()}");
                }

                m_TaskItemPool.Release(taskItem);
            }

            m_TaskItemsCache.Clear();
        }

        private void EndFrame()
        {
            TaskUpdate(Time.deltaTime);

            #region Handle Disable
            for (int iBehaviour = 0; iBehaviour < m_BehavioursForTraverse.Count; iBehaviour++)
            {
                BaseBehaviour behaviour = m_BehavioursForTraverse[iBehaviour];
                DisableBehaviour(behaviour);                
            }
            #endregion

            #region Handle Release
            for (int iBehaviour = 0; iBehaviour < m_RemoveBehavioursCache.Count; iBehaviour++)
            {
                BaseBehaviour behaviour = m_RemoveBehavioursCache[iBehaviour];
                ReleaseBehaviour(behaviour);
            }
            m_RemoveBehavioursCache.Clear();
            #endregion

            m_BehavioursForTraverse.Clear();

            MDebug.LogVerbose("Core", "EndFrame " + Time.frameCount);
        }

        private void DisableBehaviour(BaseBehaviour behaviour)
        {
            if (!behaviour.IsEnable() && behaviour.IsLastEnable())
            {
                try
                {
                    MDebug.LogVerbose("Core"
                        , $"Before execute {behaviour.GetName()}.OnDisable");

                    behaviour.OnDisable();

                    MDebug.LogVerbose("Core"
                        , $"After execute {behaviour.GetName()}.OnDisable");
                }
                catch (Exception e)
                {
                    MDebug.LogError("Core"
                        , $"Execute {behaviour.GetName()}.OnDisable Exception:{e.ToString()}");
                }
                finally
                {
                    behaviour.SetLastEnable(false);
                }
            }
        }

        private void ReleaseBehaviour(BaseBehaviour behaviour)
        {
            try
            {
                MDebug.LogVerbose("Core"
                    , $"Before execute {behaviour.GetName()}.OnRelease");

                behaviour.OnRelease();

                MDebug.LogVerbose("Core"
                    , $"After execute {behaviour.GetName()}.OnRelease");
            }
            catch (Exception e)
            {
                MDebug.LogError("Core"
                    , $"Execute {behaviour.GetName()}.OnRelease Exception:{e.ToString()}");
            }
        }
    }
}
using GF.Common.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Core.Behaviour
{
	/// <summary>
	/// 相当于<see cref="BaseProjectSetting"/>
	/// 自己实现这个的原因是：
	///     Unity的MonoBehaviour性能开销比较大
	///     实现多线程的Update
	///     控制Update频率
	///     方便调试(监控Update的用时、内存变化)
	/// 
	/// 事件的执行顺序
	///     <see cref="OnInitialize"/>
	///     <see cref="OnEnable"/>
	///     <see cref="OnTaskUpdate_Thread"/> 等待上一帧的<see cref="OnTaskUpdate_Thread"/>执行完成
	///     <see cref="OnFixedUpdate"/>
	///     <see cref="OnBeforeParallelUpdate"/>
	///     <see cref="OnParallelUpdate_Thread"/>
	///     <see cref="OnAfterParallelUpdate"/>
	///     <see cref="OnUpdate"/>
	///     <see cref="OnLateUpdate"/>
	///     <see cref="OnTaskUpdate_Thread"/> 开始执行
	///     <see cref="OnDisable"/>
	///     <see cref="OnRelease"/>
	/// 上列函数中：
	///     <see cref="OnInitialize"/>和<see cref="BaseBehaviour"/>的调用顺序相同
	///     <see cref="OnRelease"/>和<see cref="DestorySelf"/>的调用顺序相同
	/// 其余函数的调用时机受<see cref="m_Priority"/>影响
	/// 如果两个Behavior的<see cref="m_Priority"/>相同，则<see cref="m_InstanceID"/>小的优先执行
	/// </summary>
	public class BaseBehaviour
    {
        private const string EMPTY_NAME = "New Behaviour";

        /// <summary>
        /// 实例ID，自增长全局唯一
        /// </summary>
        private readonly int m_InstanceID;
        /// <summary>
        /// 一般用于调试
        /// </summary>
        private string m_Name;
        /// <summary>
        /// 所属的组名，<see cref="m_Priority"/>相同且组名相同的Behaviour会在一起处理并发
        /// </summary>
        private string m_Group;
        /// <summary>
        /// 这个Behaviour的优先级
        /// </summary>
        private int m_Priority;
        /// <summary>
        /// 是否存活
        /// </summary>
        private bool m_Alive;
        /// <summary>
        /// 上一次的Enable状态
        /// 这个变量的用途是解决Order of Execution for Event Functions
        /// </summary>
        private bool m_LastEnable;
        /// <summary>
        /// 为false时不会触发各种Update
        /// </summary>
        private bool m_Enable;
        /// <summary>
        /// 这个Behaviour开启的功能
        /// </summary>
        private FeatureFlag m_FeatureFlag;

        public BaseBehaviour() : this(EMPTY_NAME
            , 0
            , BehaviourGroup.Default.ToString())
        {
        }

        public BaseBehaviour(string name) : this(name
            , 0
            , BehaviourGroup.Default.ToString())
        {
        }

        public BaseBehaviour(string name
            , int priority
            , string groupName)
        {
            m_InstanceID = AutoIncrementID.AutoID();
            m_Name = name;
            m_Group = groupName;
            m_Priority = priority;
            m_Alive = false;
            m_LastEnable = false;
            m_Enable = true;
            m_FeatureFlag = FeatureFlag.None;

            Kernel.BehaviourManager.AddBehavior(this);
        }

        #region Events
        public virtual void OnInitialize() { }
        public virtual void OnEnable() { }

        public virtual void OnFixedUpdate(float deltaTime) { }

        /// <summary>
        /// <see cref="OnParallelUpdate_Thread"/>
        /// </summary>
        /// <returns>input</returns>
        public virtual object OnBeforeParallelUpdate(float deltaTime) { return null; }
        /// <summary>
        /// 并行执行的Update，会堵塞主线程
        /// 需开启<see cref="FeatureFlag.ParallelUpdate"/>
        /// 
        /// 相同组名，且优先级连续的Behaviour会一起执行这个Update，例如：
        ///     ID   Group Priority
        ///      1     A      1  
        ///      2     A      1  
        ///      3     B      2  
        ///      4     A      3  
        ///      5     A      4
        /// ID(xx)表示一个并发执行，执行顺序是:
        ///     ID(1,2) ID(3) ID(4,5)
        ///     
        /// 以下函数在主线程被调用
        ///     <see cref="OnBeforeParallelUpdate"/> 准备用于多线程中的数据
        ///     <see cref="OnAfterParallelUpdate"/> 将多线程的计算结果应用到游戏中
        /// 添加这两个函数的原因是，Unity大部分API都不支持多线程调用
        /// 避免写出BUG，这里参考（渲染管线中的顶点\片段着色器）的实现方式
        ///     输入数据 -> 多线程 -> 输出数据
        /// </summary>
        /// <remarks>
        /// 用法例：子弹的Behaviour
        ///     <see cref="OnBeforeParallelUpdate"/> input = 子弹的位置，移动方向，速度
        ///     <see cref="OnParallelUpdate_Thread"/> output = return 用input计算的子弹移动后的位置
        ///     <see cref="OnAfterParallelUpdate"/> 子弹.transform.position = output
        ///     
        /// <see cref="OnParallelUpdate_Thread"/>:
        ///     用途：需要依赖其他模块
        ///     适用场景：群体AI，物体移动
        /// <see cref="OnTaskUpdate_Thread"/>:
        ///     用途：当前模块相对独立
        ///     适用场景：解析数据
        /// </remarks>
        /// <returns>output</returns>
        public virtual object OnParallelUpdate_Thread(object input, float deltaTime) { return input; }
        /// <summary>
        /// <see cref="OnParallelUpdate_Thread"/>
        /// </summary>
        public virtual void OnAfterParallelUpdate(object output, float deltaTime) { }

        public virtual void OnUpdate(float deltaTime) { }
        public virtual void OnLateUpdate(float deltaTime) { }

        /// <summary>
        /// <see cref="OnTaskUpdate_Thread"/>
        /// </summary>
        public virtual object OnBeforeTastUpdate() { return null; }
        /// <summary>
        /// 异步执行的Update，不会堵塞主线程，基本是和渲染线程同步执行
        /// 需开启<see cref="FeatureFlag.TaskUpdate"/>
        /// <see cref="OnParallelUpdate_Thread"/>
        /// 
        /// 这两个函数的用法可以参考
        /// <see cref="OnBeforeTastUpdate"/>   =>    <see cref="OnBeforeParallelUpdate"/>
        /// <see cref="OnAfterTaskUpdate"/>    =>    <see cref="OnAfterParallelUpdate"/>
        /// </summary>
        public virtual object OnTaskUpdate_Thread(object input, float deltaTime) { return input; }
        /// <summary>
        /// <see cref="OnTaskUpdate_Thread"/>
        /// </summary>
        public virtual void OnAfterTaskUpdate(object output) { }

        public virtual void OnDisable() { }
        public virtual void OnRelease() { }

        /// <summary>
        /// 控制<see cref="OnUpdate"/>的调用频率
        /// 需开启<see cref="FeatureFlag.UpdateFrequency"/>
        /// </summary>
        /// <param name="finalDelta">
        /// 最终的delta
        /// 需要这个变量是因为<see cref="OnUpdate"/>可能已经好几帧未执行了，当前帧的delta应该等于前几帧的和
        /// </param>
        /// <param name="currentDelta">当前帧的delta</param>
        /// <returns>当前帧能否执行<see cref="OnUpdate"/></returns>
        public virtual bool ControlUpdateFrequency(out float finalDelta
            , float currentDelta
            , int frameCount)
        { finalDelta = currentDelta; return false; }

        /// <summary>
        /// <see cref="ControlUpdateFrequency"/>
        /// </summary>
        public virtual bool ControlLateUpdateFrequency(out float finalDelta
            , float currentDelta
            , int frameCount)
        { finalDelta = currentDelta; return false; }
        #endregion

        /// <summary>
        /// 销毁自己
        /// </summary>
        public void DestorySelf()
        {
            Kernel.BehaviourManager.RemoveBehavior(this);
        }

        public int GetInstanceID()
        {
            return m_InstanceID;
        }

        public int GetPriority()
        {
            return m_Priority;
        }

        public bool HasFeature(FeatureFlag feature)
        {
            return (m_FeatureFlag & feature) != 0;
        }

        public void EnableFeature(FeatureFlag feature)
        {
            m_FeatureFlag |= feature;
        }

        public void DisableFeature(FeatureFlag feature)
        {
            m_FeatureFlag = m_FeatureFlag & (~feature);
        }

        internal bool CanUpdate()
        {
            return m_Alive 
                && m_LastEnable
                && m_Enable;
        }

        internal void SetLastEnable(bool enable)
        {
            m_LastEnable = enable;
        }

        internal bool IsLastEnable()
        {
            return m_LastEnable;
        }

        public void SetEnable(bool enable)
        {
            m_Enable = enable;
        }

        public bool IsEnable()
        {
            return m_Enable;
        }

        public string GetGroup()
        {
            return m_Group;
        }

        internal void SetAlive(bool alive)
        {
            m_Alive = alive;
            if (!alive)
            {
                m_Enable = false;
            }
        }

        internal bool IsAlive()
        {
            return m_Alive;
        }

        public void SetName(string name)
        {
            m_Name = name;
        }

        public string GetName()
        {
            return m_Name;
        }

        public override int GetHashCode()
        {
            return m_InstanceID;
        }

        /// <summary>
        /// 对<see cref="OnTaskUpdate_Thread"/>的一层封装
        ///     错误处理
        ///     处理<see cref="System.Threading.ManualResetEvent"/>
        /// </summary>
        internal void DoTaskUpdate_Thread(object state)
        {
            TaskUpdateItem taskItem = state as TaskUpdateItem;

            try
            {
                MDebug.LogVerbose("Core"
                    , $"Before execute {GetName()}.OnTaskUpdate_Thread");

                taskItem.Output = OnTaskUpdate_Thread(taskItem.Input, taskItem.DeltaTime);

                MDebug.LogVerbose("Core"
                    , $"After execute {GetName()}.OnTaskUpdate_Thread");
            }
            catch (Exception e)
            {
                MDebug.LogError("Core"
                    , $"Execute {GetName()}.OnTaskUpdate_Thread Exception:{e.ToString()}");
            }

            taskItem.ManualResetEvent.Set();
        }
    }
}
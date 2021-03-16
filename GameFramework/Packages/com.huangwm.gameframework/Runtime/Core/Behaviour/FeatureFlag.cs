namespace GF.Core.Behaviour
{
    /// <summary>
    /// <see cref="BaseBehaviour"/>拥有的功能
    /// 顺序没有影响
    /// </summary>
    [System.Flags]
    public enum FeatureFlag : ulong
    {
        None = 0,
        Default = None,

        /// <summary>
        /// 并行的Update
        /// <see cref="BaseBehaviour.OnParallelUpdate_Thread"/>
        /// </summary>
        ParallelUpdate = 1 << 0,
        /// <summary>
        /// 异步的Update
        /// <see cref="BaseBehaviour.OnTaskUpdate_Thread"/>
        /// </summary>
        TaskUpdate = 1 << 1,
        /// <summary>
        /// 控制<see cref="BaseBehaviour.OnUpdate"/>的调用频率
        /// 未开启时，每次Unity的事件都会调用一次Update
        /// </summary>
        UpdateFrequency = 1 << 2,
        /// <summary>
        /// 控制<see cref="BaseBehaviour.OnLateUpdate"/>的调用频率
        /// 和<see cref="UpdateFrequency"/>一样
        /// </summary>
        LateUpdateFrequency = 1 << 3,
    }
}
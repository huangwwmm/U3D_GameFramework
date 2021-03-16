namespace GF.Core.Behaviour.UpdateFrequency
{
    /// <summary>
    /// 更新频率的规则
    /// <see cref="BaseBehaviour.ControlUpdateFrequency"/>
    /// 
    /// 这里约定命名规则
    ///     继承自这个接口的类名以 UFR 结尾
    /// </summary>
    public interface IUpdateFrequencyRule
    {
        bool ControlUpdateFrequency(out float finalDelta, float currentDelta, int frameCount);
    }
}
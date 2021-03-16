using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GF.Core.Behaviour.UpdateFrequency
{
    /// <summary>
    /// 间隔一定时间执行一次Update
    /// 如无特殊标注，该类的所有时间变量的单位为 秒
    /// </summary>
    public struct TimeIntervalUFR : IUpdateFrequencyRule
    {
        /// <summary>
        /// 两次Update之间的最小时间间隔
        /// </summary>
        public float TimeInterval;
        /// <summary>
        /// 当前累计的时间
        /// </summary>
        /// <remarks>
        /// 小技巧：
        ///     如果某一时间创建了大量的拥有相同<see cref="TimeInterval"/>的<see cref="TimeIntervalUFR"/>
        ///     会导致某一帧负载特别高，其他帧都闲置的情况
        /// 解决方法：
        ///     创建<see cref="TimeIntervalUFR"/>时<see cref="AccumulatedTime"/>是个随机值
        ///     即可让不同的<see cref="TimeIntervalUFR"/>分布到不同的帧中
        ///     
        /// PS：这种方法并不能把Update均匀分布到所有帧中，但是可以极大避免某一帧高负载
        /// 如果有均匀分布的需求，再考虑对应的方案
        /// </remarks>
        public float AccumulatedTime;
        /// <summary>
        /// 每次Update时的DeltaTime是否等于<see cref="TimeInterval"/>
        /// True：等于
        /// False：等于最近几帧DeltaTime的和
        /// </summary>
        /// <remarks>
        /// 如果这个值为True，且<see cref="TimeInterval"/>值较小
        /// 可能会导致每帧都执行Update
        /// </remarks>
        public bool IsStrictTimeInterval;

        public TimeIntervalUFR(float timeInterval)
            : this(timeInterval, 0, false)
        {
        }

        public TimeIntervalUFR(float timeInterval, float accumulatedTime , bool isStrictTimeInterval)
        {
            TimeInterval = timeInterval;
            AccumulatedTime = accumulatedTime;
            IsStrictTimeInterval = isStrictTimeInterval;
        }

        public bool ControlUpdateFrequency(out float finalDelta, float currentDelta, int frameCount)
        {
            AccumulatedTime += currentDelta;
            if (AccumulatedTime >= TimeInterval)
            {
                if (IsStrictTimeInterval)
                {
                    AccumulatedTime = AccumulatedTime - TimeInterval;
                    finalDelta = TimeInterval;
                }
                else
                {
                    finalDelta = AccumulatedTime;
                    AccumulatedTime = 0;
                }
                return true;
            }
            else
            {
                finalDelta = 0;
                return false;
            }
        }
    }
}
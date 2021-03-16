namespace GF.Core.Event
{
    public enum UpdateMethod : int
    {
        FixedUpdate,

        Update,

        LateUpdate,

        /// <summary>
        /// 用于计数，必须放在最后
        /// </summary>
        Count,
    }
}
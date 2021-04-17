namespace GF.Core.Event
{
	/// <summary>
	/// 临时写，Todo，为每个系统组件设置一个时间区域范围
	/// </summary>
    public enum EventName
    {
		GFAsset = 0,
		GFAssetInit,
		GFDownload = 1000,
        /// <summary>
        /// 必须放在最后
        /// </summary>
        GFEnd = 5000
    }
}
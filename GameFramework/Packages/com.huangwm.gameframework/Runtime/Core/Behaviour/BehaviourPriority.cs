namespace GF.Core.Behaviour
{
    public enum BehaviourPriority : int
    {
        GF_Start = -10000,

        EventCenter,
        ObjectPoolManager,
		DownloadManager,
		AssetManager,

        LuaManager,
        LuaLog,

        TcpClient
    }
}
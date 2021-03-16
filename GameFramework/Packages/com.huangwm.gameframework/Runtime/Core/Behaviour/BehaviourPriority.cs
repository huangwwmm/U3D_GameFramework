namespace GF.Core.Behaviour
{
    public enum BehaviourPriority : int
    {
        GF_Start = -10000,

        EventCenter,
        ObjectPoolManager,
        AssetManager,

        LuaManager,
        LuaLog,

        TcpClient
    }
}
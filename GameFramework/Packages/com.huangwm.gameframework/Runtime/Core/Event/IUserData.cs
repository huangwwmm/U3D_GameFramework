using GF.Common.Data;

namespace GF.Core.Event
{
    public interface IUserData
    {
        
    }

    public interface IPoolUserData : IUserData, IObjectPoolItem
    {

    }
}
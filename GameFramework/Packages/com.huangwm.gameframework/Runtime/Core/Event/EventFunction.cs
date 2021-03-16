namespace GF.Core.Event
{
    /// <param name="eventID">EventID</param>
    /// <param name="isImmediately">True：这条消息是立即发送  False：这条消息是延迟发送的</param>
    /// <param name="userData">消息附带的数据</param>
    public delegate void EventFunction(int eventID, bool isImmediately, IUserData userData);
}
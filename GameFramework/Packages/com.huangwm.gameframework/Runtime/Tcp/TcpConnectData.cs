using GF.Core.Event;
using System.Net.Sockets;

namespace GF.Net.Tcp
{
    public class TcpConnectData
    {
        public readonly TcpClient Client;
        public readonly SocketAsyncEventArgs SocketAsyncEventArgs;
        public readonly ConnectState ConnectState;

        public TcpConnectData(TcpClient client
            , SocketAsyncEventArgs socketAsyncEventArgs
            , ConnectState connectState)
        {
            Client = client;
            SocketAsyncEventArgs = socketAsyncEventArgs;
            ConnectState = connectState;
        }
    }
}
using GF.Common.Data;
using GF.Common.Debug;

namespace GF.Net.Tcp.Rpc
{
    public class RpcWrapper
    {
        private const int DEFAULT_BUFFER_SIZE = 1024 * 8;
        private const int DEFAULT_RPC_VALUE_POOL_SIZE = 512;

        public TcpClient TcpClient;

        public ArrayPool<RpcValue> RpcValueArrayPool;
        public ObjectPool<RpcValue> RpcValuePool;

        public RpcWrapper(TcpClient tcpClient)
        {
            TcpClient = tcpClient;

            RpcValueArrayPool = new ArrayPool<RpcValue>(DEFAULT_RPC_VALUE_POOL_SIZE);
            RpcValuePool = new ObjectPool<RpcValue>();
        }

        public void Release()
        {
            RpcValueArrayPool.Release();
            RpcValueArrayPool = null;
            RpcValuePool = null;

            TcpClient = null;
        }

        public void ReleaseRpcValue(RpcValue rpcValue)
        {
            rpcValue.Release(RpcValueArrayPool, RpcValuePool);
            RpcValuePool.Release(rpcValue);
        }

        public void Test(RpcValue paramater)
        {
            ArrayPool<byte> bufferPool = TcpClient.GetBufferPool();
            ArrayPool<byte>.Node buffer;
            lock (bufferPool)
            {
                buffer = bufferPool.AllocBuffer(DEFAULT_BUFFER_SIZE);
            }

            int serializePoint = buffer.GetOffset();
            paramater.Serialize(buffer.GetBuffer(), ref serializePoint);

            int dserializePoint = buffer.GetOffset();
            paramater.Deserialize(RpcValueArrayPool
                , RpcValuePool
                , buffer.GetBuffer()
                , ref dserializePoint);

            lock (bufferPool)
            {
                 bufferPool.ReleaseBuffer(buffer);
            }

            MDebug.Assert(serializePoint == dserializePoint
                , "Rpc"
                , "serializePoint == dserializePoint");
        }
    }
}
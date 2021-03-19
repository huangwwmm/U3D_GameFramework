using GF.Common.Collection;
using GF.Common.Data;
using GF.Common.Debug;
using GF.Core.Behaviour;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
// 出于性能考虑，修改这个类型的时候需要修改这个文件中(SIGN:TLength)对应位置的代码
using TLength = System.Int32;

namespace GF.Net.Tcp
{
    /// <summary>
    /// TODO <see cref="Disconnect"/>立即触发会导致当前正在发送接收的消息失败，添加一个延迟来解决。影响不大先不管了
    /// </summary>
    public class TcpClient : BaseBehaviour
    {
        private const string LOG_TAG = "Tcp";
        /// <summary>
        /// TODO 临时写法，之后会改成可配置的
        /// </summary>
        private const int DEFAULT_BUFFER_SIZE = 1024 * 32;
        private const int PACKAGE_HEADER_SIZE = sizeof(TLength);

        public Action<TcpConnectData> OnConnected;
        public Action<TcpConnectData> OnConnectFailed;
        public Action<TcpConnectData> OnDisconnected;
        public Action<ArrayPool<byte>.Node> OnReceivedPackage;

        private Socket m_Client;

        private ArrayPool<byte> m_BufferPool;

        private SocketAsyncEventArgs m_SendEventArgs;
        /// <summary>
        /// 发送中的Buffer
        /// </summary>
        private ArrayPool<byte>.Node m_SendingBuffer;
        /// <summary>
        /// 等待发送的Buffer
        /// </summary>
        private ArrayPool<byte>.Node m_WaitSendBuffer;

        /// <summary>
        /// <see cref="m_WaitSendBuffer"/>实际要发送数据的大小
        /// <see cref="ArrayPool.Node.GetSize()"/>指的是缓冲区的大小
        /// </summary>
        private int m_WaitSendBufferSize;
        /// <summary>
        /// 是否正在发送中
        /// </summary>
        private bool m_IsSending;

        private SocketAsyncEventArgs m_ReceiveEventArgs;
        private ArrayPool<byte>.Node m_ReceiveBuffer;

        private ReceiveState m_ReceiveState;
        /// <summary>
        /// 当前接收的数据包的包头缓存
        /// </summary>
        private ArrayPool<byte>.Node m_ReceiveHeaderBuffer;
        /// <summary>
        /// 当前接收的数据包的包体
        /// </summary>
        private ArrayPool<byte>.Node m_ReceiveBodyBuffer;
        /// <summary>
        /// <see cref="m_ReceiveHeaderBuffer"/>或<see cref="m_ReceiveBodyBuffer"/>中已经接收的数据
        /// </summary>
        private int m_ReceivedPackageCount;

        private TcpConnectData m_TcpConnectData;
        private object m_TcpConnectActionLock;

        private object m_ReceiveLock;
        private Queue<ArrayPool<byte>.Node> m_ReceivedPackages;
        /// <summary>
        /// <see cref="m_ReceivedPackages"/>的双缓冲
        /// </summary>
        private BetterList<ArrayPool<byte>.Node> m_ReceivedPackages2;
        private object m_ReceivedPackagesLock;

        public TcpClient(string name)
            : base(name, (int)BehaviourPriority.TcpClient, BehaviourGroup.Default.ToString())
        {
            m_TcpConnectActionLock = new object();
            m_ReceiveLock = new object();
        }

        public void Connect(string host, int port)
        {
            IPAddress[] addressList = Dns.GetHostEntry(host).AddressList;
            IPEndPoint endPoint = new IPEndPoint(addressList[addressList.Length - 1], port);
            MDebug.Log(LOG_TAG, $"Client({GetName()}) 开始连接{endPoint.ToString()} ({host}:{port})");

            m_Client = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(ProcessConnect);
            socketAsyncEventArgs.RemoteEndPoint = endPoint;

            if (!m_Client.ConnectAsync(socketAsyncEventArgs))
            {
                ProcessConnect(m_Client, socketAsyncEventArgs);
            }
        }

        public void Disconnect()
        {
            Disconnect(null);
        }

        public bool IsConnected()
        {
            return m_Client != null
                && m_Client.Connected;
        }

        public void Send(byte[] data)
        {
            Send(data, 0, data.Length);
        }

        public unsafe void Send(byte[] data, int offset, int count)
        {
            if (!IsConnected())
            {
                MDebug.LogError(LOG_TAG, $"Client({GetName()}) 并未连接，无法发送数据");
                return;
            }

            byte[] buffer = m_WaitSendBuffer.GetBuffer();
            int writePosition = m_WaitSendBuffer.GetOffset() + m_WaitSendBufferSize;
            fixed (byte* pBuffer = &buffer[writePosition])
            {
                *((TLength*)pBuffer) = PACKAGE_HEADER_SIZE + count;
                writePosition += PACKAGE_HEADER_SIZE;
                m_WaitSendBufferSize += PACKAGE_HEADER_SIZE;
            }

            Array.Copy(data
                , offset
                , m_WaitSendBuffer.GetBuffer()
                , writePosition
                , count);
            m_WaitSendBufferSize += count;

            StartSend();
        }

        public override void OnUpdate(float deltaTime)
        {
            lock (m_TcpConnectActionLock)
            {
                if (m_TcpConnectData != null)
                {
                    try
                    {
                        switch (m_TcpConnectData.ConnectState)
                        {
                            case ConnectState.Connected:
                                OnConnected?.Invoke(m_TcpConnectData);
                                break;
                            case ConnectState.ConnectFailed:
                                OnConnectFailed?.Invoke(m_TcpConnectData);
                                break;
                            case ConnectState.Disconnected:
                                OnDisconnected?.Invoke(m_TcpConnectData);
                                break;
                            default:
                                MDebug.Assert(false, LOG_TAG, "Not handle ConnectState: " + m_TcpConnectData.ConnectState);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        MDebug.LogError(LOG_TAG
                            , $"Inove connect:{m_TcpConnectData.ConnectState} Exception:\n{e.ToString()}");
                    }
                    m_TcpConnectData = null;
                }
            }

            if (!IsConnected()
                || m_ReceivedPackages2 == null)
            {
                return;
            }

            int packageCount;
            lock (m_ReceiveLock)
            {
                packageCount = m_ReceivedPackages.Count;
                if (m_ReceivedPackages2.Capacity < packageCount)
                {
                    m_ReceivedPackages2.Capacity = m_ReceivedPackages2.Capacity * 2;
                    m_ReceivedPackages2._SetCount(m_ReceivedPackages2.Capacity);
                }
                for (int iPackage = 0; iPackage < packageCount; iPackage++)
                {
                    m_ReceivedPackages2[iPackage] = m_ReceivedPackages.Dequeue();
                }
#if GF_DEBUG
                MDebug.Assert(m_ReceivedPackages.Count == 0, LOG_TAG, "m_ReceivedPackages.Count == 0");
#endif
            }

            if (packageCount > 0)
            {
                if (OnReceivedPackage != null)
                {
                    for (int iPackage = 0; iPackage < packageCount; iPackage++)
                    {
                        ArrayPool<byte>.Node iterPackage = m_ReceivedPackages2[iPackage];
                        try
                        {
                            OnReceivedPackage.Invoke(iterPackage);
                        }
                        catch (Exception e)
                        {
                            MDebug.LogError(LOG_TAG
                                , $"Invoke OnReceivedPackage length: {iterPackage.GetSize()}. Exception:\n{e.ToString()}");
                        }
                    }
                }

                lock (m_BufferPool)
                {
                    for (int iPackage = 0; iPackage < packageCount; iPackage++)
                    {
                        m_BufferPool.ReleaseBuffer(m_ReceivedPackages2[iPackage]);
                        m_ReceivedPackages2[iPackage] = null;
                    }
                }
            }
        }
        
        public ArrayPool<byte> GetBufferPool()
        {
            return m_BufferPool;
        }

        private void Disconnect(SocketAsyncEventArgs e)
        {
            if (m_Client == null)
            {
                return;
            }

            if (m_BufferPool != null)
            {
                lock (m_BufferPool)
                {
                    m_SendEventArgs = null;
                    m_BufferPool.ReleaseBuffer(m_SendingBuffer);
                    m_SendingBuffer = null;
                    m_BufferPool.ReleaseBuffer(m_WaitSendBuffer);
                    m_WaitSendBuffer = null;

                    m_ReceiveEventArgs = null;
                    m_BufferPool.ReleaseBuffer(m_ReceiveBuffer);
                    m_ReceiveBuffer = null;
                    if (m_ReceiveBodyBuffer != null)
                    {
                        m_BufferPool.ReleaseBuffer(m_ReceiveBodyBuffer);
                        m_ReceiveBodyBuffer = null;
                    }

                    m_BufferPool.ReleaseBuffer(m_ReceiveHeaderBuffer);
                    m_ReceiveHeaderBuffer = null;
                }

                lock (m_BufferPool)
                {
                    lock (m_ReceiveLock)
                    {
                        while (m_ReceivedPackages.Count > 0)
                        {
                            m_BufferPool.ReleaseBuffer(m_ReceivedPackages.Dequeue());
                        }
                    }
                }

                m_BufferPool.Release();
                m_BufferPool = null;
            }

            m_Client.Close();
            m_Client = null;

            lock (m_TcpConnectActionLock)
            {
                m_TcpConnectData = new TcpConnectData(this, e, ConnectState.Disconnected);
            }
        }

        private void StartReceive()
        {
            if (!m_Client.ReceiveAsync(m_ReceiveEventArgs))
            {
                ProcessReceive(m_Client, m_ReceiveEventArgs);
            }
        }

        private void StartSend()
        {
            lock (m_SendEventArgs)
            {
                if (m_IsSending)
                {
                    return;
                }

                if (m_WaitSendBufferSize == 0)
                {
                    return;
                }

                m_IsSending = true;

                ArrayPool<byte>.Node swapBuffer = m_SendingBuffer;
                m_SendingBuffer = m_WaitSendBuffer;
                m_WaitSendBuffer = swapBuffer;

                m_SendEventArgs.SetBuffer(m_SendingBuffer.GetBuffer()
                    , m_SendingBuffer.GetOffset()
                    , m_WaitSendBufferSize);
                m_WaitSendBufferSize = 0;
            }

            if (!m_Client.SendAsync(m_SendEventArgs))
            {
                ProcessSend(m_Client, m_SendEventArgs);
            }
        }

        private void ProcessConnect(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                MDebug.Log(LOG_TAG, $"Client({GetName()}) 连接成功");

                m_BufferPool = new ArrayPool<byte>(1024 * 512);
                lock (m_TcpConnectActionLock)
                {
                    m_TcpConnectData = new TcpConnectData(this, e, ConnectState.Connected);
                }
                m_IsSending = false;

                lock (m_BufferPool)
                {
                    m_SendEventArgs = new SocketAsyncEventArgs();
                    m_SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(ProcessSend);
                    m_SendingBuffer = m_BufferPool.AllocBuffer(DEFAULT_BUFFER_SIZE);
                    m_WaitSendBuffer = m_BufferPool.AllocBuffer(DEFAULT_BUFFER_SIZE);
                    m_WaitSendBufferSize = 0;
                    m_SendEventArgs.SetBuffer(m_SendingBuffer.GetBuffer(), m_SendingBuffer.GetOffset(), 0);

                    m_ReceiveEventArgs = new SocketAsyncEventArgs();
                    m_ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(ProcessReceive);
                    m_ReceiveBuffer = m_BufferPool.AllocBuffer(DEFAULT_BUFFER_SIZE);
                    m_ReceiveEventArgs.SetBuffer(m_ReceiveBuffer.GetBuffer(), m_ReceiveBuffer.GetOffset(), m_ReceiveBuffer.GetSize());

                    m_ReceiveState = ReceiveState.ReceivingPackageLength;
                    m_ReceivedPackageCount = 0;
                    m_ReceiveHeaderBuffer = m_BufferPool.AllocBuffer(PACKAGE_HEADER_SIZE);
                }

                lock (m_ReceiveLock)
                {
                    m_ReceivedPackages = new Queue<ArrayPool<byte>.Node>();
                    m_ReceivedPackages2 = new BetterList<ArrayPool<byte>.Node>(4);
                    m_ReceivedPackages2._SetCount(m_ReceivedPackages2.Capacity);
                }

                StartReceive();
            }
            else
            {
                MDebug.LogError(LOG_TAG
                    , $"Client({GetName()}) 连接失败: {e.SocketError.ToString()}\n{e.ConnectByNameError.ToString()}");
                lock (m_TcpConnectActionLock)
                {
                    m_TcpConnectData = new TcpConnectData(this, e, ConnectState.ConnectFailed);
                }

                m_Client.Close();
                m_Client = null;
            }
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            lock (m_SendEventArgs)
            {
                m_IsSending = false;
            }

            if (e.SocketError == SocketError.Success)
            {
                StartSend();
            }
            else
            {
                MDebug.LogError(LOG_TAG
                    , $"Client({GetName()}) 发送消息失败: {e.SocketError.ToString()}, 即将断开连接");
                Disconnect(e);
            }
        }

        private void ProcessReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                MDebug.LogVerbose(LOG_TAG
                    , $"Client({GetName()}) 接收长度: {e.BytesTransferred}");

                OnReceived(m_ReceiveBuffer.GetBuffer()
                    , m_ReceiveBuffer.GetOffset()
                    , e.BytesTransferred);

                StartReceive();
            }
            else
            {
                MDebug.LogError(LOG_TAG
                    , $"Client({GetName()}) 接受消息失败: {e.SocketError.ToString()}, 即将断开连接。{e.BytesTransferred}|{e.SocketError}");
                Disconnect(e);
            }
        }

        private void OnReceived(byte[] buffer, int offset, int count)
        {
            int remainCount = count;
            int readPosition = offset;
            while (remainCount > 0)
            {
                switch (m_ReceiveState)
                {
                    case ReceiveState.ReceivingPackageLength:
                        if (ReceiveToPackageBuffer(buffer
                            , ref remainCount
                            , ref readPosition
                            , m_ReceiveHeaderBuffer
                            , PACKAGE_HEADER_SIZE))
                        {
                            // SIGN:TLength
                            TLength packageLength = BitConverter.ToInt32(m_ReceiveHeaderBuffer.GetBuffer()
                                , m_ReceiveHeaderBuffer.GetOffset());
                            lock (m_BufferPool)
                            {
                                m_ReceiveBodyBuffer = m_BufferPool.AllocBuffer(packageLength - PACKAGE_HEADER_SIZE);
                            }

                            m_ReceiveState = ReceiveState.ReceivingPackageBody;
                        }
                        break;
                    case ReceiveState.ReceivingPackageBody:
                        if (ReceiveToPackageBuffer(buffer
                            , ref remainCount
                            , ref readPosition
                            , m_ReceiveBodyBuffer
                            , m_ReceiveBodyBuffer.GetSize()))
                        {
                            lock (m_ReceiveLock)
                            {
                                m_ReceivedPackages.Enqueue(m_ReceiveBodyBuffer);
                            }

                            m_ReceiveBodyBuffer = null;
                            m_ReceiveState = ReceiveState.ReceivingPackageLength;
                        }
                        break;
                }
            }
        }

        private bool ReceiveToPackageBuffer(byte[] buffer
            , ref int remainCount
            , ref int readPosition
            , ArrayPool<byte>.Node receiveBuffer
            , int targetSize)
        {
            int needReadCount = Mathf.Min(targetSize - m_ReceivedPackageCount
                , remainCount);

            Array.Copy(buffer
                , readPosition
                , receiveBuffer.GetBuffer()
                , receiveBuffer.GetOffset() + m_ReceivedPackageCount
                , needReadCount);
            remainCount -= needReadCount;
            readPosition += needReadCount;
            m_ReceivedPackageCount += needReadCount;

            if (m_ReceivedPackageCount == targetSize)
            {
                m_ReceivedPackageCount = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private enum ReceiveState
        {
            /// <summary>
            /// 正在接收包长度
            /// </summary>
            ReceivingPackageLength,
            /// <summary>
            /// 正在接收包体
            /// </summary>
            ReceivingPackageBody,
        }
    }
}
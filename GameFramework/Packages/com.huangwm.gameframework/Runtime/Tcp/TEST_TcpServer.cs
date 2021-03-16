using GF.Common.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace GF.Net.Tcp
{
    public class TEST_TcpServer
    {
        private Socket m_Server;
        private List<Socket> m_Clients;

        public void Start(int port, int bufferSize)
        {
            IPAddress[] addressList = Dns.GetHostEntry(Environment.MachineName).AddressList;
            IPEndPoint localEndPoint = new IPEndPoint(addressList[addressList.Length - 1], port);

            m_Server = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            m_Server.ReceiveBufferSize = bufferSize;
            m_Server.SendBufferSize = bufferSize;

            if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                m_Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                m_Server.Bind(new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port));
            }
            else
            {
                m_Server.Bind(localEndPoint);
            }

            m_Server.Listen(16);
            MDebug.Log("Tcp", $"Server: 监听 {localEndPoint.ToString()}");

            m_Clients = new List<Socket>();

            new Thread(Accept).Start();
            new Thread(Run).Start();
        }

        private void Accept(object obj)
        {
            while (true)
            {
                Socket client = m_Server.Accept();
                MDebug.Log("Tcp", "Server: 新连接");
                lock (m_Clients)
                {
                    m_Clients.Add(client);
                }
                Thread.Sleep(100);
            }
        }

        private void Run(object obj)
        {
            while (true)
            {
                lock (m_Clients)
                {
                    for (int iClient = 0; iClient < m_Clients.Count; iClient++)
                    {
                        Socket iterClient = m_Clients[iClient];
                        if (!iterClient.Connected)
                        {
                            MDebug.Log("Tcp", "Server: 断开连接");
                            m_Clients.RemoveAt(iClient);
                            iClient--;
                            continue;
                        }

                        if (iterClient.Available > 0)
                        {
                            MDebug.Log("Tcp", "Server: 接收 " + iterClient.Available);
                            byte[] buffer = new byte[iterClient.Available];
                            iterClient.Receive(buffer);
                            iterClient.Send(buffer);
                        }
                    }
                }

                Thread.Sleep(10);
            }
        }
    }
}
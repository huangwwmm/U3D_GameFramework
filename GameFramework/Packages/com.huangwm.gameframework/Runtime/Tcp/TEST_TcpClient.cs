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
    public class TEST_TcpClient
    {
        private Socket m_Client;

        public void Start(string host, int port)
        {
            IPAddress[] addressList = Dns.GetHostEntry(host).AddressList;
            IPEndPoint localEndPoint = new IPEndPoint(addressList[addressList.Length - 1], port);

            m_Client = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            m_Client.Connect(host, port);

            MDebug.Log("Tcp", $"Client: 连接 {host}:{port}");

            new Thread(Run).Start();
        }

        private void Run(object obj)
        {
            while (true)
            {
                if (m_Client.Available > 0)
                {
                    byte[] buffer = new byte[m_Client.Available];
                    m_Client.Receive(buffer);
                    MDebug.Log("Tcp", "Client: 接收 " + System.Text.Encoding.UTF8.GetString(buffer));
                }
                Thread.Sleep(50);
            }
        }

        public void Send(string message)
        {
            m_Client.Send(System.Text.Encoding.UTF8.GetBytes(message));
        }
    }
}
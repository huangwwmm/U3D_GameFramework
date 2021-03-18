using GF.Common.Data;
using GF.Common.Debug;
using GF.Core;
using GF.Net.Tcp;
using GF.Net.Tcp.Rpc;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Test.Game
{
    public class TEST_Game : MonoBehaviour
    {
        private IEnumerator Start()
        {
            DontDestroyOnLoad(this);

            KernelInitializeData kernelInitializeData = new KernelInitializeData().RestoreToDefault();
            kernelInitializeData.EventTypes = new List<Type>
            {
                typeof(EventName)
            };
            kernelInitializeData.LuaEnableHighPerformanceLog = true;

#if UNITY_EDITOR
            kernelInitializeData.LoadLuaByAssetDatabaseWhenEditor = true;
#endif

            yield return Kernel.Initialize(kernelInitializeData);

            int port = 3487;
            new TEST_TcpServer().Start(port, 1024 * 512);
            yield return new WaitForSeconds(0.5f);

            RpcUtiltiy.GetOrCollectAllStaticMethods();

            TcpClient client = new TcpClient("Player A");

            client.OnConnected += OnConnected;
            client.OnDisconnected += OnDisconnected;
            client.OnConnectFailed += OnConnectFailed;
            client.OnReceivedPackage += OnReceivedPackage;

            client.Connect(Environment.MachineName, port);
            yield return new WaitForSeconds(2.0f);

            {
                RpcWrapper rpcWrapper = new RpcWrapper(client);

                RpcValue paramater = rpcWrapper.RpcValuePool.Alloc();
                paramater.ValueType = GF.Net.Tcp.Rpc.ValueType.Float;
                paramater.FloatValue = 32.0f;

                rpcWrapper.Test(paramater);
                rpcWrapper.ReleaseRpcValue(paramater);

                paramater = null;
                rpcWrapper.Release();
                rpcWrapper = null;
                yield return new WaitForSeconds(0.5f);
            }

            GC.Collect();
            GC.Collect();

            client.Send(System.Text.Encoding.UTF8.GetBytes("sdf"));
            client.Send(System.Text.Encoding.UTF8.GetBytes("fgawery"));
            client.Send(System.Text.Encoding.UTF8.GetBytes("besart4"));
            yield return new WaitForSeconds(2.0f);
            client.Send(System.Text.Encoding.UTF8.GetBytes("fae7"));
            client.Send(System.Text.Encoding.UTF8.GetBytes("g34tg56"));
            client.Send(System.Text.Encoding.UTF8.GetBytes("gwa3tr3"));
            yield return new WaitForSeconds(2.0f);
            client.Disconnect();
        }

        private void OnReceivedPackage(ArrayPool<byte>.Node obj)
        {
            MDebug.Log("T", "OnReceivedPackage: "
                + System.Text.Encoding.UTF8.GetString(obj.GetBuffer(), obj.GetOffset(), obj.GetSize()));
        }

        private void OnConnectFailed(TcpConnectData obj)
        {
            MDebug.Log("T", "OnConnectFailed");
        }

        private void OnConnected(TcpConnectData obj)
        {
            MDebug.Log("T", "OnConnected");
        }

        private void OnDisconnected(TcpConnectData obj)
        {
            MDebug.Log("T", "OnDisconnected");
        }

        public enum EventName : int
        {
            Start = GF.Core.Event.EventName.GFEnd,
        }
    }
}
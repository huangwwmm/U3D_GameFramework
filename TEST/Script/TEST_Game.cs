using GF.Asset;
using GF.Common.Data;
using GF.Common.Debug;
using GF.Core;
using GF.Core.Event;
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
		private GameObject obj;
      

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

            yield return Kernel.Initialize(this,kernelInitializeData);

			Kernel.EventCenter.AddListen((int)EventName.EventA, OnEventA);
			Kernel.EventCenter.SendImmediately((int)EventName.EventA, Kernel.EventCenter.GetUserData<EventAUserData>());
			Kernel.EventCenter.RemoveListen((int)EventName.EventA, OnEventA);

			Kernel.EventCenter.AddListen((int)EventName.EventB, OnEventB);
			Kernel.EventCenter.SendImmediately((int)EventName.EventB, new EventBUserData());
			Kernel.EventCenter.RemoveListen((int)EventName.EventB, OnEventB);

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

		private void OnEventB(int eventID, bool isImmediately, IUserData userData)
		{
			
		}

		private void OnEventA(int eventID, bool isImmediately, IUserData userData)
		{
		}

		private void Update()
		{
			//if (Input.GetKeyDown(KeyCode.G))
			//{
			//	Debug.Log("Instantiate GameObject send!");
   //             Kernel.AssetManager.InstantiateGameObjectAsync(AssetKey.Prefabs_Box001_Variant_1_prefab, (GF.Asset.AssetKey key, UnityEngine.Object tmpObj) =>
   //             {
   //                 obj = tmpObj as GameObject;
   //                 Debug.Log("Instantiate GameObject Success!");
   //             }, new GF.Asset.InstantiateBasicData() { IsWorldSpace = false, Parent = this.transform, Position = Vector3.one });

   //         }


			//if (Input.GetKeyDown(KeyCode.D))
			//{
			//	Kernel.AssetManager.ReleaseGameObjectAsync(obj);
			//}
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

			EventA,
			EventB,
		}

		public class EventAUserData : IPoolUserData
		{
			public void OnAlloc()
			{
			}

			public void OnRelease()
			{
			}
		}

		public class EventBUserData : IUserData
		{

		}

	}



}
﻿using System;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Net.P2P
{
    /// <summary>P2P测试</summary>
    public static class P2PTest
    {
        /// <summary>开始</summary>
        /// <param name="port"></param>
        public static void StartHole(Int32 port = 15)
        {
            var server = new HoleServer();
            server.AddServer(IPAddress.Any, port);
            server.AddServer(IPAddress.Any, port + 1);
            server.Start();
        }

        /// <summary>开始客户端</summary>
        /// <param name="name">名称</param>
        /// <param name="server"></param>
        /// <param name="serverport"></param>
        /// <param name="isTcp"></param>
        public static void StartClient(String name, String server = "127.0.0.1", Int32 serverport = 15, Boolean isTcp = false)
        {
            var client = new P2PClient();
            // 这里决定是Udp穿透还是Tcp穿透
            if (isTcp) client.ProtocolType = NetType.Tcp;
            client.HoleServer = new IPEndPoint(NetHelper.ParseAddress(server), serverport);
            client.EnsureServer();
            //var s = client.Server as UdpServer;
            //s.Send("test", null, client.HoleServer);
            //s.Send("nnhy", null, new IPEndPoint(client.HoleServer.Address, serverport + 1));
            Console.WriteLine("任意键开始！");
            Console.ReadKey(true);
            client.Start(name);
        }
    }
}
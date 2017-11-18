﻿using System;
using System.Net.Sockets;
using NewLife.Model;
using NewLife.Net.Proxy;
using NewLife.Net.Sockets;

namespace NewLife.Net
{
    /// <summary>网络服务对象提供者</summary>
    public class NetService //: ServiceContainer<NetService>
    {
        #region 当前静态服务容器
        /// <summary>当前对象容器</summary>
        public static IObjectContainer Container { get { return ObjectContainer.Current; } }
        #endregion

        static NetService()
        {
            var container = Container;
            container.Register<IProxySession, ProxySession>()
                .Register<ISocketServer, TcpServer>(ProtocolType.Tcp)
                .Register<ISocketServer, UdpServer>(ProtocolType.Udp)
                .Register<ISocketClient, TcpSession>(ProtocolType.Tcp)
                .Register<ISocketClient, UdpServer>(ProtocolType.Udp)
                .Register<ISocketSession, TcpSession>(ProtocolType.Tcp)
                //.Register<ISocketSession, UdpServer>(ProtocolType.Udp)
                //.Register<IStatistics, Statistics>()
                .Register<INetSession, NetSession>();
        }

        /// <summary>安装，引发静态构造函数</summary>
        public static void Install() { }

        #region 方法
        /// <summary>根据网络标识创建客户端并连接（对Tcp）</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static ISocketClient CreateClient(NetUri uri)
        {
            if (uri == null) throw new ArgumentNullException("uri");

            var client = Container.Resolve<ISocketClient>(uri.ProtocolType);
            //if (uri.EndPoint != null)
            //{
            //    if (uri.ProtocolType == ProtocolType.Tcp) client.Connect(uri.EndPoint);
            //}
            client.Remote = uri;

            return client;
        }

        /// <summary>根据网络标识创建客户端会话并连接（对Tcp）</summary>
        /// <param name="uri"></param>
        /// <param name="timeout">数据接收超时时间</param>
        /// <returns></returns>
        public static ISocketSession CreateSession(NetUri uri, int timeout = 3000)
        {
            if (uri == null) throw new ArgumentNullException("uri");

            // 特殊处理UDP
            if (uri.ProtocolType == ProtocolType.Udp)
            {
                var server = Container.Resolve<ISocketServer>(uri.ProtocolType);
                if (server is UdpServer)
                {
                    var udp = server as UdpServer;
                    udp.Timeout = timeout;
                    //udp.Open();
                    //udp.ReceiveAsync();
                    return udp.CreateSession(uri.EndPoint);
                }
            }

            var client = Container.Resolve<ISocketSession>(uri.ProtocolType);
            if (uri.EndPoint != null)
            {
                //if (uri.ProtocolType == ProtocolType.Tcp) (client as ISocketClient).Connect(uri.EndPoint);
                if (client is ISocketClient && !uri.EndPoint.IsAny()) //(client as ISocketClient).Connect(uri.EndPoint);
                {
                    var socketClient = client as ISocketClient;
                    socketClient.Timeout = timeout;
                }
            }
            client.Remote = uri;

            return client;
        }
        #endregion
    }
}
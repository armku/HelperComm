﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Udp
{
    /// <summary>UDP服务器</summary>
    /// <remarks>
    /// 核心工作：启动服务<see cref="OnStart"/>时，监听端口，并启用多个（逻辑处理器数的10倍）异步接收操作<see cref="ReceiveAsync"/>。
    /// 接收到的数据全部转接到<see cref="Received"/>事件中。
    /// 
    /// 服务器完全处于异步工作状态，任何操作都不可能被阻塞。
    /// </remarks>
    public class UdpServer : SocketServer, IUdp
    {
        #region 属性
        /// <summary>已重载。</summary>
        public override ProtocolType ProtocolType { get { return ProtocolType.Udp; } }
        #endregion

        #region 构造
        /// <summary>构造一个UDP服务器实例</summary>
        public UdpServer() : base(IPAddress.Any, 0) { }

        /// <summary>构造一个UDP服务器实例</summary>
        /// <param name="port"></param>
        public UdpServer(Int32 port) : base(IPAddress.Any, port) { }

        /// <summary>构造一个UDP服务器实例</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public UdpServer(IPAddress address, Int32 port) : base(address, port) { }

        /// <summary>构造一个UDP服务器实例</summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public UdpServer(String hostname, Int32 port) : base(hostname, port) { }
        #endregion

        #region 开始停止
        /// <summary>开始</summary>
        protected override void OnStart()
        {
            Server.EnableBroadcast = true;

            base.OnStart();

            ReceiveAsync();
        }

        /// <summary>开始异步接收数据</summary>
        public virtual void ReceiveAsync()
        {
            StartAsync(e =>
            {
                var server = Server;
                if (server == null || Disposed) { e.Cancel = true; return false; }

                // 兼容IPV6
                var address = AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;
                e.RemoteEndPoint = new IPEndPoint(address, 0);
                // 不能用ReceiveAsync，否则得不到远程地址
                return server.ReceiveFromAsync(e);
            });
        }
        #endregion

        #region 事件
        /// <summary>数据到达。在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        public event EventHandler<NetEventArgs> Received;

        /// <summary>接收到数据时</summary>
        /// <param name="e"></param>
        protected virtual void OnReceive(NetEventArgs e)
        {
            // 没有接收事件时，马上开始处理重建委托
            if (Received == null)
            {
                ReceiveAsync();
                return;
            }

            Process(e, ReceiveAsync, ProcessReceive);
        }

        void ProcessReceive(NetEventArgs e)
        {
            // 统计接收数
            IncCounter();

            CheckBufferSize(e);
            if (Received != null)
            {
                e.Session = CreateSession(e.RemoteIPEndPoint);
                Received(this, e);
            }
        }

        /// <summary>已重载。</summary>
        /// <param name="e"></param>
        protected override void OnComplete(NetEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.ReceiveFrom:
                case SocketAsyncOperation.ReceiveMessageFrom:
                    OnReceive(e);
                    return;
                default:
                    break;
            }

            base.OnComplete(e);
        }
        #endregion

        #region 发送
        /// <summary>向指定目的地发送信息</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="size"></param>
        /// <param name="remoteEP"></param>
        public void Send(Byte[] buffer, Int32 offset = 0, Int32 size = 0, EndPoint remoteEP = null)
        {
            if (size <= 0) size = buffer.Length - offset;
            var socket = Server;
            if (socket.Connected)
                socket.Send(buffer, offset, size, SocketFlags.None);
            else
                socket.SendTo(buffer, offset, size, SocketFlags.None, remoteEP);

            //return this;
        }
        #endregion

        #region 接收
        /// <summary>接收数据</summary>
        /// <returns></returns>
        public Byte[] Receive()
        {
            //Byte[] buffer = new Byte[BufferSize];
            //if (!Server.IsBound) Bind();

            //Int32 size = Server.Receive(buffer);
            //if (size <= 0) return null;

            //Byte[] data = new Byte[size];
            //Buffer.BlockCopy(buffer, 0, data, 0, size);
            //return data;

            throw new NotSupportedException();
        }

        /// <summary>接收字符串</summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public String ReceiveString(Encoding encoding = null)
        {
            //Byte[] buffer = Receive();
            //if (buffer == null || buffer.Length < 1) return null;

            //if (encoding == null) encoding = Encoding.UTF8;
            //return encoding.GetString(buffer);

            throw new NotSupportedException();
        }
        #endregion

        #region 创建会话
        /// <summary>为指定地址创建会话。对于无连接Socket，必须指定远程地址；对于有连接Socket，指定的远程地址将不起任何作用</summary>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        ISocketSession CreateSession(IPEndPoint remoteEP = null)
        {
            if (!Server.Connected && remoteEP == null) throw new ArgumentNullException("remoteEP", "未连接Udp必须指定远程地址！");

            return new UdpSession(this, remoteEP);
        }
        #endregion
    }
}
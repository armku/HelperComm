﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using NewLife.Messaging;
using NewLife.Net.Sockets;

namespace NewLife.Net.Common
{
    /// <summary>客户端消息提供者</summary>
    public class ClientMessageProvider : MessageProvider
    {
        private ISocketClient _Session;
        /// <summary>客户端</summary>
        public ISocketClient Session
        {
            get { return _Session; }
            set
            {
                if (_Session != value)
                {
                    _Session = value;

                    if (value != null)
                    {
                        //// 只有Udp需要控制包大小
                        //MaxMessageSize = value.ProtocolType == ProtocolType.Udp ? 1460 : 0;
                        //MaxMessageSize = 1460;

                        // 鉴于Internet上的标准MTU值为576字节,所以我建议在进行Internet的UDP编程时.
                        // 最好将UDP的数据长度控件在548字节(576-8-20)以内.
                        // 局域网环境下,UDP包大小为1024*8,速度达到2M/s,丢包情况理想.
                        // 外网环境下,UDP包大小为548,速度理想,丢包情况理想.
                        // http://www.cnblogs.com/begingame/archive/2011/08/18/2145138.html
                        MaxMessageSize = value.Local.ProtocolType == ProtocolType.Udp ? 1472 : 1460;
                        //MaxMessageSize = value.ProtocolType == ProtocolType.Udp ? 1024 * 8 : 1460;
                        //value.Host.Socket.DontFragment = true;

                        if (value.UseReceiveAsync)
                        {
                            value.Received += new EventHandler<ReceivedEventArgs>(client_Received);

                            //if (!value.UseReceiveAsync) value.ReceiveAsync();
                        }
                    }
                }
            }
        }

        /// <summary>Session被关闭需要更新时触发。外部应该重新给Session赋值</summary>
        public event EventHandler OnUpdate;

        void client_Received(object sender, ReceivedEventArgs e)
        {
            var session = Session;
            try
            {
                var message = Message.Read(e.Stream);
                Process(message, session.Remote);
            }
            catch (Exception ex)
            {
                var msg = new ExceptionMessage() { Value = ex };
                Process(msg, session.Remote);
            }
        }

        /// <summary>发送消息。如果有响应，可在消息到达事件中获得。这里会实现大消息分包。</summary>
        /// <param name="message"></param>
        public override void Send(Message message)
        {
            // MaxMessageSize未设置时，先取一次Session，触发MaxMessageSize的设置
            if (MaxMessageSize <= 0 && GetSession() == null) throw new ArgumentNullException("会话为空！可能没有设置OnUpdate事件！");

            base.Send(message);
        }

        /// <summary>发送数据流。</summary>
        /// <param name="stream"></param>
        protected override void OnSend(Stream stream)
        {
            var session = GetSession();
            session.Send(stream);

            // UDP暂停一会，避免连续发送导致数据包丢失
            if (session.Local.ProtocolType == ProtocolType.Udp) Thread.Sleep(10);
        }

        ISocketClient GetSession()
        {
            var session = Session;
            Boolean needUpdate = false;
            if (session == null || session.Disposed /*|| session.Host == null*/)
                needUpdate = true;
            else
            {
                //var socket = session.Host.Socket;
                //if (socket == null || socket.ProtocolType == ProtocolType.Tcp && !socket.Connected)
                //    needUpdate = true;
            }

            if (needUpdate && OnUpdate != null) OnUpdate(this, EventArgs.Empty);

            return Session;
        }

        /// <summary>发送并接收</summary>
        /// <param name="message"></param>
        /// <param name="millisecondsTimeout"></param>
        /// <returns></returns>
        public override Message SendAndReceive(Message message, int millisecondsTimeout = 0)
        {
            var session = GetSession();
            if (session.UseReceiveAsync) return base.SendAndReceive(message, millisecondsTimeout);

            Send(message);
            var data = session.Receive();
            if (data == null || data.Length < 1) return null;

            var ms = new MemoryStream(data);
            return Message.Read(ms);
        }
    }
}
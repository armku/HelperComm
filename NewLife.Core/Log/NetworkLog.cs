﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NewLife.Net;

namespace NewLife.Log
{
    /// <summary>网络日志</summary>
    public class NetworkLog : Logger, IDisposable
    {
        /// <summary>网络套接字</summary>
        public UdpClient Client { get; set; }

        /// <summary>远程服务器地址</summary>
        public IPEndPoint Remote { get; set; }

        /// <summary>实例化网络日志。默认广播到514端口</summary>
        public NetworkLog()
        {
            Remote = new IPEndPoint(IPAddress.Broadcast, 514);
        }

        /// <summary>指定日志服务器地址来实例化网络日志</summary>
        /// <param name="server"></param>
        public NetworkLog(IPEndPoint server)
        {
            Remote = server;
        }

        /// <summary>销毁</summary>
        public void Dispose()
        {
            if (Client != null) Client.Close();
        }

        private Boolean _inited;
        void Init()
        {
            if (_inited) return;

            // 默认Udp广播
            var client = new UdpClient();
            client.Connect(Remote);
            if (Remote.Address.Equals(IPAddress.Broadcast)) client.EnableBroadcast = true;
            Client = client;

            try
            {
                // 首先发送日志头
                client.Send(GetHead().GetBytes());

                // 尝试向日志服务器表名身份
                var buf = "{0} {1}/{2} 准备上报日志".F(DateTime.Now.ToFullString(), Environment.UserName, Environment.MachineName).GetBytes();
                client.Send(buf);
            }
            catch (Exception ex) { client.Send(("读取环境变量错误=>" + ex.Message).GetBytes()); }

            _inited = true;
        }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            Init();

            var e = WriteLogEventArgs.Current.Set(level).Set(Format(format, args), null);
            var buf = e.ToString().GetBytes();
            if (Client.Client.ProtocolType == ProtocolType.Udp)
            {
                // 捕获异常，不能因为写日志异常导致上层出错
                try
                {
                    Client.Send(new MemoryStream(buf));
                }
                catch
                {
                    // 出错后重新初始化
                    _inited = false;
                }
            }
        }
    }
}
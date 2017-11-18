﻿using System;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Net;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>MQ客户端</summary>
    public class MQClient : DisposeBase
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>远程地址</summary>
        public NetUri Remote { get; set; }

        /// <summary>网络客户端</summary>
        public ApiClient Client { get; set; }

        /// <summary>已登录</summary>
        public Boolean Logined { get; private set; }
        #endregion

        #region 构造函数
        /// <summary>实例化</summary>
        public MQClient()
        {
            Remote = new NetUri(NetType.Tcp, NetHelper.MyIP(), 2234);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Close(GetType().Name + (disposing ? "Dispose" : "GC"));

            Client.TryDispose();
        }
        #endregion

        #region 打开关闭
        /// <summary>确保创建客户端</summary>
        public void EnsureCreate()
        {
            var ac = Client;
            if (ac == null || ac.Disposed)
            {
                ac = new ApiClient(Remote + "");
                ac.Encoder = new JsonEncoder();
                ac.Log = Log;
#if DEBUG
                ac.Client.Log = Log;
                ac.EncoderLog = Log;
#endif

                ac["user"] = Name;

                Client = ac;
            }
        }

        /// <summary>打开</summary>
        public void Open()
        {
            var ac = Client;
            if (ac != null && !ac.Disposed)
            {
                Logined = false;

                ac?.Open();
            }
        }

        /// <summary>关闭</summary>
        public void Close(String reason)
        {
            Client.Close(reason ?? (GetType().Name + "Close"));
        }
        #endregion

        #region 发布订阅
        ///// <summary>发布主题</summary>
        ///// <param name="topic"></param>
        ///// <returns></returns>
        //public async Task<Boolean> CreateTopic(String topic)
        //{
        //    Open();

        //    Log.Info("{0} 创建主题 {1}", Name, topic);

        //    var rs = await Client.InvokeAsync<Boolean>("Topic/Create", new { topic });

        //    return rs;
        //}

        /// <summary>订阅主题</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public async Task<Boolean> Subscribe(String topic)
        {
            Open();

            Log.Info("{0} 订阅主题 {1}", Name, topic);

            var rs = await Client.InvokeAsync<Boolean>("Topic/Subscribe", new { topic });

            //if (rs) Client.Register<ClientController>();

            return rs;
        }
        #endregion

        #region 收发消息
        /// <summary>发布消息</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<Boolean> Public(Object msg)
        {
            Open();

            Log.Info("{0} 发布消息 {1}", Name, msg);

            var m = new Message
            {
                Sender = Name,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddSeconds(60),
                Content = msg
            };

            var rs = await Client.InvokeAsync<Boolean>("Message/Public", new { msg = m });

            return rs;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}
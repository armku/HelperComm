﻿using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Pop3
{
    /// <summary>Pop3服务器</summary>
    public class Pop3Server : NetServer<Pop3Session>
    {
        //TODO 未实现Pop3服务端

        #region 属性
        //private Dictionary<Int32, Pop3Session> _Sessions;
        ///// <summary>会话集合</summary>
        //public IDictionary<Int32, Pop3Session> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<int, Pop3Session>()); } }
        #endregion

        /// <summary>实例化一个Pop3服务器</summary>
        public Pop3Server()
        {
            Port = 110;
            ProtocolType = NetType.Tcp;
        }
    }
}
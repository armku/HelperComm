﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;
using NewLife.Serialization;

namespace NewLife.Net.Common
{
    /// <summary>网络资源标识，指定协议、地址、端口、地址族（IPv4/IPv6）</summary>
    /// <remarks>
    /// 仅序列化<see cref="ProtocolType"/>和<see cref="EndPoint"/>，其它均是配角！
    /// 有可能<see cref="Host"/>代表主机域名，而<see cref="Address"/>指定主机IP地址。
    /// </remarks>
    public class NetUri : IAccessor
    {
        #region 属性
        private ProtocolType _ProtocolType;
        /// <summary>协议类型</summary>
        public ProtocolType ProtocolType { get { return _ProtocolType; } set { _ProtocolType = value; _Protocol = value.ToString(); } }

        [NonSerialized]
        private String _Protocol;
        /// <summary>协议</summary>
        [XmlIgnore]
        public String Protocol
        {
            get { return _Protocol; }
            set
            {
                _Protocol = value;
                if (String.IsNullOrEmpty(value))
                    _ProtocolType = ProtocolType.Unknown;
                else
                {
                    try
                    {
                        _ProtocolType = (ProtocolType)Enum.Parse(typeof(ProtocolType), value, true);
                        // 规范化名字
                        _Protocol = _ProtocolType.ToString();
                    }
                    catch { _ProtocolType = ProtocolType.Unknown; }
                }
            }
        }

        /// <summary>地址</summary>
        [XmlIgnore]
        public IPAddress Address { get { return EndPoint.Address; } set { EndPoint.Address = value; _Host = value + ""; } }

        [NonSerialized]
        private String _Host;
        /// <summary>主机</summary>
        [XmlIgnore]
        public String Host { get { return _Host; } set { _Host = value; try { EndPoint.Address = NetHelper.ParseAddress(value); } catch { } } }

        /// <summary>端口</summary>
        [XmlIgnore]
        public Int32 Port { get { return EndPoint.Port; } set { EndPoint.Port = value; } }

        private IPEndPoint _EndPoint;
        /// <summary>终结点</summary>
        public IPEndPoint EndPoint { get { return _EndPoint ?? (_EndPoint = new IPEndPoint(IPAddress.Any, 0)); } set { _EndPoint = value; _Host = value == null ? null : value.Address.ToString(); } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public NetUri() { }

        /// <summary>实例化</summary>
        /// <param name="uri"></param>
        public NetUri(String uri) { Parse(uri); }

        /// <summary>实例化</summary>
        /// <param name="protocol"></param>
        /// <param name="endpoint"></param>
        public NetUri(ProtocolType protocol, IPEndPoint endpoint)
        {
            ProtocolType = protocol;
            EndPoint = endpoint;
        }

        /// <summary>实例化</summary>
        /// <param name="protocol"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public NetUri(ProtocolType protocol, IPAddress address, Int32 port)
        {
            ProtocolType = protocol;
            Address = address;
            Port = port;
        }
        #endregion

        #region 方法
        static readonly String Sep = "://";

        /// <summary>分析</summary>
        /// <param name="uri"></param>
        public NetUri Parse(String uri)
        {
            if (uri.IsNullOrWhiteSpace()) return this;

            // 分析协议
            var p = uri.IndexOf(Sep);
            if (p > 0)
            {
                Protocol = uri.Substring(0, p);
                uri = uri.Substring(p + Sep.Length);
            }

            // 分析端口
            p = uri.LastIndexOf(":");
            if (p > 0)
            {
                var pt = uri.Substring(p + 1);
                Int32 port = 0;
                if (Int32.TryParse(pt, out port))
                {
                    Port = port;
                    uri = uri.Substring(0, p);
                }
            }

            Host = uri;

            return this;
        }

        /// <summary>克隆</summary>
        /// <returns></returns>
        public NetUri Clone()
        {
            return new NetUri().CopyFrom(this);
        }

        /// <summary>从另一个对象复制</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public NetUri CopyFrom(NetUri uri)
        {
            if (uri == null) return this;

            this.Protocol = uri.Protocol;
            this.Host = uri.Host;
            this.Port = uri.Port;

            return this;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Port > 0)
                return String.Format("{0}://{1}:{2}", Protocol, Host, Port);
            else
                return String.Format("{0}://{1}", Protocol, Host);
        }
        #endregion

        #region IAccessor 成员

        bool IAccessor.Read(IReader reader) { return false; }

        bool IAccessor.ReadComplete(IReader reader, bool success)
        {
            // 因为反序列化仅给字段复制，重新设置一下，保证Protocol等属性有值
            ProtocolType = ProtocolType;
            EndPoint = EndPoint;

            return success;
        }

        bool IAccessor.Write(IWriter writer) { return false; }

        bool IAccessor.WriteComplete(IWriter writer, bool success) { return success; }

        #endregion
    }
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using NewLife.Reflection;
using NewLife.Serialization;
using System.Linq;
using NewLife.Security;

namespace NewLife.Net.Stun
{
    /// <summary>Stun消息</summary>
    /// <remarks>未测试，可能没有考虑字节序</remarks>
    public class StunMessage //: IAccessor
    {
        #region 属性
        /* RFC 5389 6.             
                All STUN messages MUST start with a 20-byte header followed by zero
                or more Attributes.  The STUN header contains a STUN message type,
                magic cookie, transaction ID, and message length.

                 0                   1                   2                   3
                 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                 |0 0|     STUN Message Type     |         Message Length        |
                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                 |                         Magic Cookie                          |
                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                 |                                                               |
                 |                     Transaction ID (96 bits)                  |
                 |                                                               |
                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
              
               The message length is the count, in bytes, of the size of the
               message, not including the 20 byte header.
            */

        private StunMessageType _Type;
        /// <summary>消息类型</summary>
        public StunMessageType Type { get { return _Type; } set { _Type = value; } }

        private UInt16 _Length;
        /// <summary>消息长度</summary>
        public UInt16 Length { get { return _Length; } set { _Length = value; } }

        //private Int32 _MagicCookie = 0x2112A442;
        ///// <summary>幻数。0x2112A442</summary>
        //public Int32 MagicCookie { get { return _MagicCookie; } set { _MagicCookie = value; } }

        [FieldSize(16)]
        private Byte[] _TransactionID;
        /// <summary>会话编号</summary>
        public Byte[] TransactionID { get { return _TransactionID; } set { _TransactionID = value; } }
        #endregion

        #region 特性集合
        //[FieldSize("_Length")]
        //private Byte[] _Data;
        ///// <summary>数据</summary>
        //private Byte[] Data { get { return _Data; } set { _Data = value; } }

        [NonSerialized]
        private Dictionary<AttributeType, StunAttribute> _Atts;
        /// <summary>属性集合</summary>
        private Dictionary<AttributeType, StunAttribute> Atts { get { return _Atts ?? (_Atts = new Dictionary<AttributeType, StunAttribute>()); } }

        StunAttribute GetAtt(AttributeType type, Boolean create = false)
        {
            StunAttribute att = null;
            if (Atts.TryGetValue(type, out att)) return att;

            if (!create) return null;

            lock (Atts)
            {
                att = new StunAttribute();
                att.Type = type;
                Atts.Add(type, att);
                return att;
            }
        }

        T GetAtt<T>(AttributeType type, Int32 position = 0)
        {
            var att = GetAtt(type, false);
            if (att == null) return default(T);

            Object v = null;
            switch (type)
            {
                case AttributeType.ChangeRequest:
                    var d = att.Int;
                    if (position == 0)
                        v = (d & 4) != 0;
                    else
                        v = (d & 2) != 0;
                    return (T)v;
                case AttributeType.ErrorCode:
                    var data = att.Data;
                    var t = typeof(T);
                    if (t == typeof(Int32))
                        v = (data[2] & 0x7) * 100 + (data[3] & 0xFF);
                    else if (t == typeof(String))
                        v = Encoding.UTF8.GetString(data, 4, data.Length - 4);
                    return (T)v;
                default:
                    return att.GetValue<T>();
            }
        }

        void SetAtt<T>(AttributeType type, T value, Int32 position = 0)
        {
            var att = GetAtt(type, true);

            switch (type)
            {
                case AttributeType.ChangeRequest:
                    var d = att.Int;
                    var b = (Boolean)(Object)value;
                    if (position == 0)
                    {
                        if (b)
                            d |= 4;
                        else if ((d & 4) != 0)
                            d ^= 4;
                    }
                    else
                    {
                        if (b)
                            d |= 2;
                        else if ((d & 2) != 0)
                            d ^= 2;
                    }
                    att.Int = d;
                    break;
                case AttributeType.ErrorCode:
                    var data = att.Data;
                    var t = typeof(T);
                    if (t == typeof(Int32))
                    {
                        if (data == null)
                        {
                            data = new Byte[6];
                            att.Data = data;
                            data[3] = 4;
                        }
                        data[4] = (Byte)Math.Floor((Double)((Int32)(Object)value / 100));
                        data[5] = (Byte)((Int32)(Object)value & 0xFF);
                    }
                    else if (t == typeof(String))
                    {
                        var sd = Encoding.UTF8.GetBytes((String)(Object)value);
                        if (data == null)
                        {
                            data = new Byte[6 + sd.Length];
                            att.Data = data;
                            data[3] = (Byte)(4 + sd.Length);
                        }
                        sd.CopyTo(data, 6);
                    }
                    break;
                default:
                    att.SetValue<T>(value);
                    break;
            }
        }
        #endregion

        #region 扩展属性
        /// <summary>映射地址</summary>
        public IPEndPoint MappedAddress { get { return GetAtt<IPEndPoint>(AttributeType.MappedAddress); } set { SetAtt<IPEndPoint>(AttributeType.MappedAddress, value); } }

        /// <summary>响应地址</summary>
        public IPEndPoint ResponseAddress { get { return GetAtt<IPEndPoint>(AttributeType.ResponseAddress); } set { SetAtt<IPEndPoint>(AttributeType.ResponseAddress, value); } }

        /// <summary>请求改变</summary>
        public Boolean ChangeIP { get { return GetAtt<Boolean>(AttributeType.ChangeRequest, 0); } set { SetAtt<Boolean>(AttributeType.ChangeRequest, value, 0); } }

        /// <summary>请求改变</summary>
        public Boolean ChangePort { get { return GetAtt<Boolean>(AttributeType.ChangeRequest, 1); } set { SetAtt<Boolean>(AttributeType.ChangeRequest, value, 1); } }

        /// <summary>源地址</summary>
        public IPEndPoint SourceAddress { get { return GetAtt<IPEndPoint>(AttributeType.SourceAddress); } set { SetAtt<IPEndPoint>(AttributeType.SourceAddress, value); } }

        /// <summary>改变后的地址</summary>
        public IPEndPoint ChangedAddress { get { return GetAtt<IPEndPoint>(AttributeType.ChangedAddress); } set { SetAtt<IPEndPoint>(AttributeType.ChangedAddress, value); } }

        /// <summary>用户名</summary>
        public String UserName { get { return GetAtt<String>(AttributeType.Username); } set { SetAtt<String>(AttributeType.Username, value); } }

        /// <summary>密码</summary>
        public String Password { get { return GetAtt<String>(AttributeType.Password); } set { SetAtt<String>(AttributeType.Password, value); } }

        /// <summary>错误</summary>
        public Int32 ErrCode { get { return GetAtt<Int32>(AttributeType.ErrorCode); } set { SetAtt<Int32>(AttributeType.ErrorCode, value); } }

        /// <summary>错误</summary>
        public String ErrReason { get { return GetAtt<String>(AttributeType.ErrorCode); } set { SetAtt<String>(AttributeType.ErrorCode, value); } }

        /// <summary>服务端从客户端拿到的地址</summary>
        public IPEndPoint ReflectedFrom { get { return GetAtt<IPEndPoint>(AttributeType.ReflectedFrom); } set { SetAtt<IPEndPoint>(AttributeType.ReflectedFrom, value); } }

        /// <summary>服务器名称</summary>
        public String ServerName { get { return GetAtt<String>(AttributeType.ServerName); } set { SetAtt<String>(AttributeType.ServerName, value); } }
        #endregion

        #region 构造
        /// <summary>实例化一个Stun消息</summary>
        public StunMessage()
        {
            ResetTransactionID();

            ChangeIP = false;
            ChangePort = false;
        }
        #endregion

        #region 方法
        /// <summary>重置会话ID</summary>
        public void ResetTransactionID()
        {
            //if (TransactionID == null || TransactionID.Length != 16) TransactionID = new Byte[16];
            //var rnd = new Random();
            //rnd.NextBytes(TransactionID);
            TransactionID = Rand.NextBytes(16);
            TransactionID[0] = 0;
        }
        #endregion

        #region 读写
        /// <summary>从流中读取消息</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static StunMessage Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            var reader = new Binary();
            reader.Stream = stream;
            reader.EncodeInt = false;
            reader.IsLittleEndian = false;
            reader.UseFieldSize = true;

            var msg = reader.Read<StunMessage>();

            // 负载数据
            var p = stream.Position + msg.Length;
            if (p > stream.Length) p = stream.Length;
            while (stream.Position < p)
            {
                //var type = reader.Read<AttributeType>();
                var att = reader.Read<StunAttribute>();
                msg.Atts[att.Type] = att;
            }

            return msg;
        }

        /// <summary>把消息写入流中</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            var writer = new Binary();
            writer.EncodeInt = false;
            writer.IsLittleEndian = false;
            writer.UseFieldSize = true;

            // 先写特性，为了取得数据长度
            foreach (var att in Atts)
            {
                //writer.Write(att.Key);
                var str = att.Value.ToString();
                writer.Write(att.Value);
            }
            var ms = writer.Stream;
            Length = (UInt16)ms.Length;

            writer.Stream = stream;
            writer.Write(this);

            // 把特性数据写入
            stream.Write(ms.ToArray());
        }

        /// <summary>获取消息的数据流</summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            var ms = new MemoryStream();
            Write(ms);
            ms.Position = 0;
            return ms;
        }

        /// <summary>获取消息的字节码</summary>
        /// <returns></returns>
        public Byte[] ToArray()
        {
            var ms = new MemoryStream();
            Write(ms);
            return ms.ToArray();
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var tname = Type.ToString().TrimStart("Binding");
            if (Atts.Count == 0) return tname;

            if (Atts.Count == 1) return "{0} {1}".F(tname, Atts.FirstOrDefault().Value);

            return "{0}[{1}] {2}".F(tname, Atts.Count, Atts.FirstOrDefault().Value);
        }
        #endregion

        #region IAccessor 成员
        //bool IAccessor.Read(IReader reader) { return false; }

        //bool IAccessor.ReadComplete(IReader reader, bool success)
        //{
        //    // 分析属性
        //    if (Data != null && Data.Length > 0)
        //    {
        //        reader.Stream = new MemoryStream(Data);
        //        //_Atts = reader.ReadObject<Dictionary<AttributeType, StunAttribute>>();
        //        while (reader.Stream.Position < reader.Stream.Length)
        //        {
        //            var type = reader.ReadObject<AttributeType>();
        //            var att = reader.ReadObject<StunAttribute>();
        //            Atts[type] = att;
        //        }
        //    }

        //    return success;
        //}

        //bool IAccessor.Write(IWriter writer)
        //{
        //    // 处理属性
        //    if (Atts != null && Atts.Count > 0)
        //    {
        //        var wr = writer.GetType().CreateInstance() as IWriter;
        //        wr.Settings = writer.Settings;
        //        //wr.WriteObject(Atts);
        //        foreach (var item in Atts)
        //        {
        //            wr.WriteObject(item.Key);
        //            wr.WriteObject(item.Value);
        //        }
        //        wr.Stream.Position = 0;
        //        Data = wr.Stream.ReadBytes();
        //    }

        //    return false;
        //}

        //bool IAccessor.WriteComplete(IWriter writer, bool success) { return success; }
        #endregion
    }
}
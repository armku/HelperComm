﻿using ComHelper.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ComHelper.Net
{
    /// <summary>粘包处理接口</summary>
    public interface IPacket
    {
        /// <summary>创建消息</summary>
        /// <param name="pk">数据包</param>
        /// <returns></returns>
        //IMessage CreateMessage(Packet pk);

        /// <summary>加载消息</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        //IMessage LoadMessage(Packet pk);

        /// <summary>加入请求队列</summary>
        /// <param name="request">请求的数据</param>
        /// <param name="remote">远程</param>
        /// <param name="msTimeout">超时取消时间</param>
        //Task<Packet> Add(Packet request, IPEndPoint remote, Int32 msTimeout);

        /// <summary>检查请求队列是否有匹配该响应的请求</summary>
        /// <param name="response">响应的数据</param>
        /// <param name="remote">远程</param>
        /// <returns></returns>
        Boolean Match(Packet response, IPEndPoint remote);

        /// <summary>分析数据流，得到一帧数据</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        Packet[] Parse(Packet pk);
    }

    /// <summary>粘包处理接口工厂</summary>
    public interface IPacketFactory
    {
        /// <summary>创建粘包处理实例，内含缓冲区，不同会话不能共用</summary>
        /// <returns></returns>
        IPacket Create();
    }

    /// <summary>头部指明长度的封包格式</summary>
    [DisplayName("无头部封包")]
    public class PacketProvider : IPacket
    {
        #region 属性
        /// <summary>长度所在位置，默认-1表示没有头部</summary>
        public Int32 Offset { get; set; } = -1;

        /// <summary>长度占据字节数，1/2/4个字节，0表示压缩编码整数，默认2</summary>
        public Int32 Size { get; set; } = 2;

        /// <summary>过期时间，超过该时间后按废弃数据处理，默认500ms</summary>
        public Int32 Expire { get; set; } = 500;

        private DateTime _last;
        #endregion

        #region 创建消息
        /// <summary>创建消息</summary>
        /// <param name="pk">数据包</param>
        /// <returns></returns>
        //public virtual IMessage CreateMessage(Packet pk)
        //{
        //    // 创建没有头部的消息
        //    return new Message { Payload = pk };
        //}

        /// <summary>加载消息</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        //public virtual IMessage LoadMessage(Packet pk)
        //{
        //    if (pk == null || pk.Count == 0) return null;

        //    // 创建没有头部的消息
        //    var msg = new Message();
        //    msg.Read(pk);

        //    return msg;
        //}
        #endregion

        #region 匹配队列
        /// <summary>数据包匹配队列</summary>
        public IPacketQueue Queue { get; set; }

        /// <summary>加入请求队列</summary>
        /// <param name="request">请求的数据</param>
        /// <param name="remote">远程</param>
        /// <param name="msTimeout">超时取消时间</param>
        //public virtual Task<Packet> Add(Packet request, IPEndPoint remote, Int32 msTimeout)
        //{
        //    if (Queue == null) Queue = new DefaultPacketQueue();

        //    return Queue.Add(this, request, remote, msTimeout);
        //}

        /// <summary>检查请求队列是否有匹配该响应的请求</summary>
        /// <param name="response">响应的数据</param>
        /// <param name="remote">远程</param>
        /// <returns></returns>
        public virtual Boolean Match(Packet response, IPEndPoint remote)
        {
            if (Queue == null) return false;

            return Queue.Match(this, response, remote);
        }
        #endregion

        #region 粘包处理
        /// <summary>内部缓存</summary>
        private MemoryStream _ms;

        /// <summary>分析数据流，得到一帧数据</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public virtual Packet[] Parse(Packet pk)
        {
            if (Offset < 0) return new Packet[] { pk };

           var nodata = _ms == null || _ms.Position < 0 || _ms.Position >= _ms.Length;

            var list = new List<Packet>();
            // 内部缓存没有数据，直接判断输入数据流是否刚好一帧数据，快速处理，绝大多数是这种场景
            if (nodata)
            {
                if (pk == null) return list.ToArray();

                //var ms = pk.GetStream();
                var idx = 0;
                while (idx < pk.Count)
                {
                    var pk2 = new Packet(pk.Data, pk.Offset + idx, pk.Count - idx);
                    var len = GetLength(pk2.GetStream());
                    if (len <= 0 || len > pk2.Count) break;

                    pk2 = new Packet(pk.Data, pk.Offset + idx, len);
                    list.Add(pk2);
                    idx += len;
                }
                // 如果没有剩余，可以返回
                if (idx == pk.Count) return list.ToArray();

                // 剩下的
                pk = new Packet(pk.Data, pk.Offset + idx, pk.Count - idx);
            }

            if (_ms == null) _ms = new MemoryStream();

            // 加锁，避免多线程冲突
            lock (_ms)
            {
                if (pk != null)
                {
                    // 超过该时间后按废弃数据处理
                    var now = DateTime.Now;
                    if (_last.AddMilliseconds(Expire) < now)
                    {
                        _ms.SetLength(0);
                        _ms.Position = 0;
                    }
                    _last = now;

                    // 拷贝数据到最后面
                    var p = _ms.Position;
                    _ms.Position = _ms.Length;
                    //_ms.Write(pk.Data, pk.Offset, pk.Count);
                    pk.WriteTo(_ms);
                    _ms.Position = p;
                }

                while (_ms.Position < _ms.Length)
                {
                    var len = GetLength(_ms);
                    if (len <= 0) break;

                    var pk2 = new Packet(_ms.ReadBytes(len));
                    list.Add(pk2);
                }

                return list.ToArray();
            }
        }

        /// <summary>从数据流中获取整帧数据长度</summary>
        /// <param name="stream"></param>
        /// <returns>数据帧长度（包含头部长度位）</returns>
        protected virtual Int32 GetLength(Stream stream)
        {
            if (Offset < 0) return (Int32)(stream.Length - stream.Position);

            var p = stream.Position;
            // 数据不够，连长度都读取不了
            if (p + Offset >= stream.Length) return 0;

            // 移动到长度所在位置
            if (Offset > 0) stream.Seek(Offset, SeekOrigin.Current);

            // 读取大小
            var len = 0;
            switch (Size)
            {
                case 0:
                    len = stream.ReadEncodedInt();
                    break;
                case 1:
                    len = stream.ReadByte();
                    break;
                case 2:
                    len = stream.ReadBytes(2).ToInt();
                    break;
                case 4:
                    len = (Int32)stream.ReadBytes(4).ToUInt32();
                    break;
                case -2:
                    len = stream.ReadBytes(2).ToUInt16(0, false);
                    break;
                case -4:
                    len = (Int32)stream.ReadBytes(4).ToUInt32(0, false);
                    break;
                default:
                    throw new NotSupportedException();
            }

            // 判断后续数据是否足够
            if (stream.Position + len > stream.Length)
            {
                // 长度不足，恢复位置
                stream.Position = p;
                return 0;
            }

            // 数据长度加上头部长度
            len += (Int32)(stream.Position - p);

            // 恢复位置
            stream.Position = p;

            return len;
        }
        #endregion        
    }
}

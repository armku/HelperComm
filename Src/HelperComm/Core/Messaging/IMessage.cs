﻿using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Messaging
{
    /// <summary>消息命令</summary>
    public interface IMessage //: IAccessor
    {
        /// <summary>是否响应</summary>
        Boolean Reply { get; }

        /// <summary>负载数据</summary>
        Packet Payload { get; set; }

        /// <summary>根据请求创建配对的响应消息</summary>
        /// <returns></returns>
        IMessage CreateReply();

        /// <summary>从数据包中读取消息</summary>
        /// <param name="pk"></param>
        /// <returns>是否成功</returns>
        Boolean Read(Packet pk);

        /// <summary>把消息转为封包</summary>
        /// <returns></returns>
        Packet ToPacket();

        ///// <summary>把消息写入到数据流中</summary>
        ///// <param name="stream"></param>
        //void Write(Stream stream);
    }

    /// <summary>消息命令基类</summary>
    public class Message : IMessage
    {
        /// <summary>是否响应</summary>
        public Boolean Reply { get; set; }

        /// <summary>负载数据</summary>
        public Packet Payload { get; set; }

        /// <summary>根据请求创建配对的响应消息</summary>
        /// <returns></returns>
        public virtual IMessage CreateReply()
        {
            if (Reply) throw new Exception("不能根据响应消息创建响应消息");

            var msg = GetType().CreateInstance() as Message;
            msg.Reply = true;

            return msg;
        }

        /// <summary>从数据包中读取消息</summary>
        /// <param name="pk"></param>
        /// <returns>是否成功</returns>
        public virtual Boolean Read(Packet pk)
        {
            Payload = pk;

            return true;
        }

        /// <summary>把消息转为封包</summary>
        /// <returns></returns>
        public virtual Packet ToPacket() { return Payload; }
    }
}
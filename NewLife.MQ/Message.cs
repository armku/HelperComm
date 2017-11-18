﻿using System;
using System.Xml.Serialization;

namespace NewLife.MessageQueue
{
    /// <summary>消息</summary>
    public class Message
    {
        /// <summary>主题</summary>
        [XmlIgnore]
        public String Topic { get; set; }

        /// <summary>发送者</summary>
        public String Sender { get; set; }

        /// <summary>开始时间</summary>
        [XmlIgnore]
        public DateTime StartTime { get; set; }

        /// <summary>过期时间</summary>
        [XmlIgnore]
        public DateTime EndTime { get; set; }

        /// <summary>标签</summary>
        public String Tag { get; set; }

        /// <summary>主体</summary>
        public Object Content { get; set; }

        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString()
        {
            //var str = "";
            //var buf = Content as Byte[];
            //if (buf != null)
            //    str = buf.ToStr();
            //else
            //    str = Content + "";

            return "{0}#{1}".F(Sender, Topic);
        }
    }
}
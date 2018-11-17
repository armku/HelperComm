﻿using System;
using System.ComponentModel;
#if !__MOBILE__
using System.IO.Ports;
#endif
using System.Text;
using System.Xml.Serialization;
using NewLife.Xml;

namespace NewLife.Net
{
    /// <summary>串口配置</summary>
    [XmlConfigFile("Config\\Serial.config")]
    public class SerialPortConfig : XmlConfig<SerialPortConfig>
    {
        /// <summary>串口名</summary>
        [Description("串口名")]
        public String PortName { get; set; } = "COM1";

        /// <summary>波特率</summary>
        [Description("波特率")]
        public Int32 BaudRate { get; set; } = 115200;

        /// <summary>数据位</summary>
        [Description("数据位")]
        public Int32 DataBits { get; set; } = 8;
        
        /// <summary>停止位</summary>
        [Description("停止位 None/One/Two/OnePointFive")]
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>奇偶校验</summary>
        [Description("奇偶校验 None/Odd/Even/Mark/Space")]
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>文本编码</summary>
        [XmlIgnore]
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>编码</summary>
        [Description("编码 gb2312/us-ascii/utf-8")]
        public String WebEncoding { get { return Encoding.WebName; } set { Encoding = Encoding.GetEncoding(value); } }

        /// <summary>十六进制显示</summary>
        [Description("十六进制显示")]
        public Boolean HexShow { get; set; }

        /// <summary>十六进制自动换行</summary>
        [Description("十六进制自动换行")]
        public Boolean HexNewLine { get; set; }

        /// <summary>十六进制发送</summary>
        [Description("十六进制发送")]
        public Boolean HexSend { get; set; }
        /// <summary>
        /// 发送区内容
        /// </summary>
        [Description("发送区内容")]
        public String StrSend { get; set; } = "";
    }
}
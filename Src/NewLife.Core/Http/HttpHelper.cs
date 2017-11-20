using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Http
{
    /// <summary>Http帮助类</summary>
    public static class HttpHelper
    {
        #region Http封包解包
        
        private static Byte[] NewLine = new[] { (Byte)'\r', (Byte)'\n', (Byte)'\r', (Byte)'\n' };
        
        #endregion

        #region WebSocket
        
        /// <summary>握手</summary>
        /// <param name="key"></param>
        /// <param name="response"></param>
        public static void Handshake(String key, HttpResponse response)
        {
            if (key.IsNullOrEmpty()) return;

            var buf = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes());
            key = buf.ToBase64();

            //var sb = new StringBuilder();
            //sb.AppendLine("HTTP/1.1 101 Switching Protocols");
            //sb.AppendLine("Upgrade: websocket");
            //sb.AppendLine("Connection: Upgrade");
            //sb.AppendLine("Sec-WebSocket-Accept: " + key);
            //sb.AppendLine();

            //return sb.ToString().GetBytes();

            response.StatusCode = HttpStatusCode.SwitchingProtocols;
            response.Headers["Upgrade"] = "websocket";
            response.Headers["Connection"] = "Upgrade";
            response.Headers["Sec-WebSocket-Accept"] = key;
        }

        /// <summary>分析WS数据包</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static Packet ParseWS(Packet pk)
        {
            if (pk.Count < 2) return null;

            var ms = pk.GetStream();

            // 仅处理一个包
            var fin = (ms.ReadByte() & 0x80) == 0x80;
            if (!fin) return null;

            var len = ms.ReadByte();

            var mask = (len & 0x80) == 0x80;

            /*
             * 数据长度
             * len < 126    单字节表示长度
             * len = 126    后续2字节表示长度，大端
             * len = 127    后续8字节表示长度
             */
            len = len & 0x7F;
            if (len == 126)
                len = ms.ReadBytes(2).ToUInt16(0, false);
            else if (len == 127)
                // 没有人会传输超大数据
                len = (Int32)BitConverter.ToUInt64(ms.ReadBytes(8), 0);

            // 如果mask，剩下的就是数据，避免拷贝，提升性能
            if (!mask) return new Packet(pk.Data, pk.Offset + (Int32)ms.Position, len);

            var masks = new Byte[4];
            if (mask) masks = ms.ReadBytes(4);

            // 读取数据
            var data = ms.ReadBytes(len);

            if (mask)
            {
                for (var i = 0; i < len; i++)
                {
                    data[i] = (Byte)(data[i] ^ masks[i % 4]);
                }
            }

            return data;
        }

        /// <summary>创建WS请求包</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public static Packet MakeWS(Packet pk)
        {
            if (pk == null) return null;

            var size = pk.Count;

            var ms = new MemoryStream();
            ms.WriteByte(0x81);

            /*
             * 数据长度
             * len < 126    单字节表示长度
             * len = 126    后续2字节表示长度，大端
             * len = 127    后续8字节表示长度
             */
            if (size < 126)
                ms.WriteByte((Byte)size);
            else if (size < 0xFFFF)
            {
                ms.WriteByte(126);
                ms.Write(((Int16)size).GetBytes(false));
            }
            else
                throw new NotSupportedException();

            //pk.WriteTo(ms);

            //return new Packet(ms.ToArray());

            return new Packet(ms.ToArray()) { Next = pk };
        }
        #endregion
    }
}
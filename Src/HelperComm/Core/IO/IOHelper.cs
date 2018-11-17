using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using NewLife;
using NewLife.Reflection;

namespace System
{
    /// <summary>IO工具类</summary>
    public static class IOHelper
    {       

        #region 复制数据流
        
        /// <summary>把一个字节数组写入到一个数据流</summary>
        /// <param name="des">目的数据流</param>
        /// <param name="src">源数据流</param>
        /// <returns></returns>
        public static Stream Write(this Stream des, params Byte[] src)
        {
            if (src != null && src.Length > 0) des.Write(src, 0, src.Length);
            return des;
        }
        
        private static DateTime _dt1970 = new DateTime(1970, 1, 1);
       
        
        /// <summary>复制数组</summary>
        /// <param name="src">源数组</param>
        /// <param name="offset">起始位置</param>
        /// <param name="count">复制字节数</param>
        /// <returns>返回复制的总字节数</returns>
        public static Byte[] ReadBytes(this Byte[] src, Int32 offset = 0, Int32 count = -1)
        {
            if (count == 0) return new Byte[0];

            // 即使是全部，也要复制一份，而不只是返回原数组，因为可能就是为了复制数组
            if (count < 0) count = src.Length - offset;

            var bts = new Byte[count];
            Buffer.BlockCopy(src, offset, bts, 0, bts.Length);
            return bts;
        }
        
        #endregion

        #region 数据流转换
        /// <summary>数据流转为字节数组</summary>
        /// <remarks>
        /// 针对MemoryStream进行优化。内存流的Read实现是一个个字节复制，而ToArray是调用内部内存复制方法
        /// 如果要读完数据，又不支持定位，则采用内存流搬运
        /// 如果指定长度超过数据流长度，就让其报错，因为那是调用者所期望的值
        /// </remarks>
        /// <param name="stream">数据流</param>
        /// <param name="length">长度，0表示读到结束</param>
        /// <returns></returns>
        public static Byte[] ReadBytes(this Stream stream, Int64 length = -1)
        {
            if (stream == null) return null;
            if (length == 0) return new Byte[0];

            if (length > 0 && stream.CanSeek && stream.Length - stream.Position < length)
                throw new XException("无法从长度只有{0}的数据流里面读取{1}字节的数据", stream.Length - stream.Position, length);

            if (length > 0)
            {
                var buf = new Byte[length];
                var n = stream.Read(buf, 0, buf.Length);
                //if (n != buf.Length) buf = buf.ReadBytes(0, n);
                return buf;
            }

            // 如果要读完数据，又不支持定位，则采用内存流搬运
            if (!stream.CanSeek)
            {
                var ms = new MemoryStream();
                while (true)
                {
                    var buf = new Byte[1024];
                    var count = stream.Read(buf, 0, buf.Length);
                    if (count <= 0) break;

                    ms.Write(buf, 0, count);
                    if (count < buf.Length) break;
                }

                return ms.ToArray();
            }
            else
            {
                // 如果指定长度超过数据流长度，就让其报错，因为那是调用者所期望的值
                length = (Int32)(stream.Length - stream.Position);

                var buf = new Byte[length];
                stream.Read(buf, 0, buf.Length);
                return buf;
            }
        }
        /// <summary>字节数组转换为字符串</summary>
        /// <param name="buf">字节数组</param>
        /// <param name="encoding">编码格式</param>
        /// <param name="offset">字节数组中的偏移</param>
        /// <param name="count">字节数组中的查找长度</param>
        /// <returns></returns>
        public static String ToStr(this Byte[] buf, Encoding encoding = null, Int32 offset = 0, Int32 count = -1)
        {
            if (buf == null || buf.Length < 1 || offset >= buf.Length) return null;
            if (encoding == null) encoding = Encoding.UTF8;

            var size = buf.Length - offset;
            if (count < 0 || count > size) count = size;

            // 可能数据流前面有编码字节序列，需要先去掉
            var idx = 0;
            var preamble = encoding.GetPreamble();
            if (preamble != null && preamble.Length > 0 && buf.Length >= preamble.Length)
            {
                if (buf.ReadBytes(offset, preamble.Length).StartsWith(preamble)) idx = preamble.Length;
            }

            return encoding.GetString(buf, offset + idx, count - idx);
        }
        #endregion

        #region 数据转整数
        /// <summary>从字节数据指定位置读取一个无符号16位整数</summary>
        /// <param name="data"></param>
        /// <param name="offset">偏移</param>
        /// <param name="isLittleEndian">是否小端字节序</param>
        /// <returns></returns>
        public static UInt16 ToUInt16(this Byte[] data, Int32 offset = 0, Boolean isLittleEndian = true)
        {
            if (isLittleEndian)
                return (UInt16)((data[offset + 1] << 8) | data[offset]);
            else
                return (UInt16)((data[offset] << 8) | data[offset + 1]);
        }

        /// <summary>从字节数据指定位置读取一个无符号32位整数</summary>
        /// <param name="data"></param>
        /// <param name="offset">偏移</param>
        /// <param name="isLittleEndian">是否小端字节序</param>
        /// <returns></returns>
        public static UInt32 ToUInt32(this Byte[] data, Int32 offset = 0, Boolean isLittleEndian = true)
        {
            if (isLittleEndian) return BitConverter.ToUInt32(data, offset);

            // BitConverter得到小端，如果不是小端字节顺序，则倒序
            if (offset > 0) data = data.ReadBytes(offset, 4);
            if (isLittleEndian)
                return (UInt32)(data[0] | data[1] << 8 | data[2] << 0x10 | data[3] << 0x18);
            else
                return (UInt32)(data[0] << 0x18 | data[1] << 0x10 | data[2] << 8 | data[3]);
        }
        #endregion

        #region 7位压缩编码整数
        /// <summary>以压缩格式读取32位整数</summary>
        /// <param name="stream">数据流</param>
        /// <returns></returns>
        public static Int32 ReadEncodedInt(this Stream stream)
        {
            Byte b;
            var rs = 0;
            Byte n = 0;
            while (true)
            {
                var bt = stream.ReadByte();
                if (bt < 0) throw new Exception("数据流超出范围！已读取整数{0:n0}".F(rs));
                b = (Byte)bt;

                // 必须转为Int32，否则可能溢出
                rs += (Int32)((b & 0x7f) << n);
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 32) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        #endregion

        #region 数据流查找
        /// <summary>在字节数组中查找另一个字节数组的位置，不存在则返回-1</summary>
        /// <param name="source">字节数组</param>
        /// <param name="start">源数组起始位置</param>
        /// <param name="count">查找长度</param>
        /// <param name="buffer">另一个字节数组</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">查找长度</param>
        /// <returns></returns>
        public static Int64 IndexOf(this Byte[] source, Int64 start, Int64 count, Byte[] buffer, Int64 offset = 0, Int64 length = -1)
        {
            if (start < 0) start = 0;
            if (count <= 0 || count > source.Length - start) count = source.Length;
            if (length <= 0 || length > buffer.Length - offset) length = buffer.Length - offset;

            // 已匹配字节数
            Int64 win = 0;
            for (var i = start; i + length - win <= count; i++)
            {
                if (source[i] == buffer[offset + win])
                {
                    win++;

                    // 全部匹配，退出
                    if (win >= length) return i - length + 1 - start;
                }
                else
                {
                    //win = 0; // 只要有一个不匹配，马上清零
                    // 不能直接清零，那样会导致数据丢失，需要逐位探测，窗口一个个字节滑动
                    i = i - win;
                    win = 0;
                }
            }

            return -1;
        }
        /// <summary>一个数组是否以另一个数组开头</summary>
        /// <param name="source"></param>
        /// <param name="buffer">缓冲区</param>
        /// <returns></returns>
        public static Boolean StartsWith(this Byte[] source, Byte[] buffer)
        {
            if (source.Length < buffer.Length) return false;

            for (var i = 0; i < buffer.Length; i++)
            {
                if (source[i] != buffer[i]) return false;
            }
            return true;
        }
        #endregion

        #region 十六进制编码
        /// <summary>把字节数组编码为十六进制字符串</summary>
        /// <param name="data">字节数组</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量。超过实际数量时，使用实际数量</param>
        /// <returns></returns>
        public static String ToHex(this Byte[] data, Int32 offset = 0, Int32 count = -1)
        {
            if (data == null || data.Length < 1) return "";

            if (count < 0)
                count = data.Length - offset;
            else if (offset + count > data.Length)
                count = data.Length - offset;
            if (count == 0) return "";

            //return BitConverter.ToString(data).Replace("-", null);
            // 上面的方法要替换-，效率太低
            var cs = new Char[count * 2];
            // 两个索引一起用，避免乘除带来的性能损耗
            for (Int32 i = 0, j = 0; i < count; i++, j += 2)
            {
                var b = data[offset + i];
                cs[j] = GetHexValue(b / 0x10);
                cs[j + 1] = GetHexValue(b % 0x10);
            }
            return new String(cs);
        }

        /// <summary>把字节数组编码为十六进制字符串，带有分隔符和分组功能</summary>
        /// <param name="data">字节数组</param>
        /// <param name="separate">分隔符</param>
        /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
        /// <param name="maxLength">最大显示多少个字节。默认-1显示全部</param>
        /// <returns></returns>
        public static String ToHex(this Byte[] data, String separate, Int32 groupSize = 0, Int32 maxLength = -1)
        {
            if (data == null || data.Length < 1) return "";

            if (groupSize < 0) groupSize = 0;

            var count = data.Length;
            if (maxLength > 0 && maxLength < count) count = maxLength;

            if (groupSize == 0 && count == data.Length)
            {
                // 没有分隔符
                if (String.IsNullOrEmpty(separate)) return data.ToHex();

                // 特殊处理
                if (separate == "-") return BitConverter.ToString(data, 0, count);
            }

            var len = count * 2;
            if (!String.IsNullOrEmpty(separate)) len += (count - 1) * separate.Length;
            if (groupSize > 0)
            {
                // 计算分组个数
                var g = (count - 1) / groupSize;
                len += g * 2;
                // 扣除间隔
                if (!String.IsNullOrEmpty(separate)) len -= g * separate.Length;
            }
            var sb = new StringBuilder(len);
            for (var i = 0; i < count; i++)
            {
                if (sb.Length > 0)
                {
                    if (groupSize > 0 && i % groupSize == 0)
                        sb.AppendLine();
                    else
                        sb.Append(separate);
                }

                var b = data[i];
                sb.Append(GetHexValue(b / 0x10));
                sb.Append(GetHexValue(b % 0x10));
            }

            return sb.ToString();
        }

        private static Char GetHexValue(Int32 i)
        {
            if (i < 10)
                return (Char)(i + 0x30);
            else
                return (Char)(i - 10 + 0x41);
        }

        /// <summary>解密</summary>
        /// <param name="data">Hex编码的字符串</param>
        /// <param name="startIndex">起始位置</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public static Byte[] ToHex(this String data, Int32 startIndex = 0, Int32 length = -1)
        {
            if (String.IsNullOrEmpty(data)) return new Byte[0];

            // 过滤特殊字符
            data = data.Trim()
                .Replace("-", null)
                .Replace("0x", null)
                .Replace("0X", null)
                .Replace(" ", null)
                .Replace("\r", null)
                .Replace("\n", null)
                .Replace(",", null);

            if (length <= 0) length = data.Length - startIndex;

            var bts = new Byte[length / 2];
            for (var i = 0; i < bts.Length; i++)
            {
                bts[i] = Byte.Parse(data.Substring(startIndex + 2 * i, 2), NumberStyles.HexNumber);
            }
            return bts;
        }
        #endregion

        #region BASE64编码

        /// <summary>Base64字符串转为字节数组</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Byte[] ToBase64(this String data)
        {
            if (String.IsNullOrEmpty(data)) return new Byte[0];

            return Convert.FromBase64String(data);
        }
        #endregion
    }
}
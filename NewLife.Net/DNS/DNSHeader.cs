﻿using System;

namespace NewLife.Net.DNS
{
    /// <summary>DNS头部</summary>
    public class DNSHeader
    {
        #region 属性
        /// <summary>全局编号，不断累加</summary>
        static Int16 gid = 1;

        private Int16 _ID;
        /// <summary>长度为16位，是一个用户发送查询的时候定义的随机数，当服务器返回结果的时候，返回包的ID与用户发送的一致。</summary>
        public Int16 ID { get { return _ID; } set { _ID = value; } }

        /// <summary>默认RecursionDesired</summary>
        private D1 _D1 = D1.RecursionDesired;

        /// <summary>是否响应</summary>
        public Boolean Response { get { return _D1.Has(D1.Response); } set { _D1 = _D1.Set<D1>(D1.Response, value); } }

        /// <summary>长度4位，值0是标准查询，1是反向查询，2是服务器状态查询。</summary>
        public DNSOpcodeType Opcode { get { return (DNSOpcodeType)(((UInt16)_D1 >> 3) & 0xFF); } set { _D1 = (D1)((UInt16)_D1 | ((Byte)value << 3)); } }

        /// <summary>长度1位，授权应答(Authoritative Answer) - 这个比特位在应答的时候才有意义，指出给出应答的服务器是查询域名的授权解析服务器。</summary>
        public Boolean AuthoritativeAnswer { get { return _D1.Has(D1.AuthoritativeAnswer); } set { _D1 = _D1.Set<D1>(D1.AuthoritativeAnswer, value); } }

        /// <summary>长度1位，截断(TrunCation) - 用来指出报文比允许的长度还要长，导致被截断。</summary>
        public Boolean TrunCation { get { return _D1.Has(D1.TrunCation); } set { _D1 = _D1.Set<D1>(D1.TrunCation, value); } }

        /// <summary>长度1位，期望递归(Recursion Desired) - 这个比特位被请求设置，应答的时候使用的相同的值返回。如果设置了RD，就建议域名服务器进行递归解析，递归查询的支持是可选的。</summary>
        public Boolean RecursionDesired { get { return _D1.Has(D1.RecursionDesired); } set { _D1 = _D1.Set<D1>(D1.RecursionDesired, value); } }

        // 一个字节，因为Z是保留3位，这里干脆占用了它的位置
        private Byte _D2;

        /// <summary>长度1位，支持递归(Recursion Available) - 这个比特位在应答中设置或取消，用来代表服务器是否支持递归查询。</summary>
        public Boolean RecursionAvailable { get { return (_D2 & 0x80) == 0x80; } set { _D2 = (Byte)(value ? _D2 | 0x80 : _D2 & 0x7F); } }

        internal UInt16 Reserved
        {
            get { return ((UInt16)_D2).GetBits(4, 3); }
            set { ((UInt16)_D2).SetBits(4, 3, value); }
        }

        /// <summary>Broadcast Flag</summary>
        public Boolean Broadcast
        {
            get { return _D2.GetBit(4); }
            set { _D2.SetBit(4, value); }
        }

        //private Byte _ResponseCode;
        /// <summary>长度4位，应答码，类似http的stateCode一样，值0没有错误、1格式错误、2服务器错误、3名字错误、4服务器不支持、5拒绝。</summary>
        public DNSRcodeType ResponseCode { get { return (DNSRcodeType)(_D2 & 0x0F); } set { _D2 = (Byte)(_D2 | ((Byte)value & 0x0F)); } }

        private Int16 _Questions;
        /// <summary>报文请求段中的问题记录数</summary>
        public Int16 Questions { get { return _Questions; } set { _Questions = value; } }

        private Int16 _Answers;
        /// <summary>报文回答段中的回答记录数</summary>
        public Int16 Answers { get { return _Answers; } set { _Answers = value; } }

        private Int16 _Authorities;
        /// <summary>报文授权段中的授权记录数</summary>
        public Int16 Authoritis { get { return _Authorities; } set { _Authorities = value; } }

        private Int16 _Additionals;
        /// <summary>报文附加段中的附加记录数</summary>
        public Int16 Additionals { get { return _Additionals; } set { _Additionals = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个DNS头部</summary>
        public DNSHeader() { ID = gid++; }
        #endregion

        #region 枚举
        [Flags]
        enum D1 : Byte
        {
            RecursionDesired = 1,

            TrunCation = 2,

            AuthoritativeAnswer = 4,

            //Opcode = 8 + 16,
            Opcode1 = 8,
            Opcode2 = 16,
            Opcode3 = 32,
            Opcode4 = 64,

            Response = 128,
        }
        #endregion
    }
}
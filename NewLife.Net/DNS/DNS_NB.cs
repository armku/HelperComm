﻿using System;
using System.Net;
using NewLife.Serialization;

namespace NewLife.Net.DNS
{
    /// <summary>NetBIOS记录</summary>
    /// <remarks>
    /// 查询的时候只需要设置Question.Name。
    /// </remarks>
    public class DNS_NB : DNSRecord
    {
        #region 属性
        private UInt16 _flags = 0;

        /// <summary></summary>
        public Boolean G { get { return BitHelper.GetBit(_flags, 15); } }

        /// <summary></summary>
        public UInt16 ONT { get { return BitHelper.GetBits(_flags, 13, 2); } }

        [FieldSize("_Length", -2)]
        private IPAddress _Address;
        /// <summary>地址</summary>
        public IPAddress Address { get { return _Address; } set { _Address = value; } }

        /// <summary>文本信息</summary>
        public override String Text { get { return Address + ""; } set { Address = String.IsNullOrEmpty(value) ? null : IPAddress.Parse(value); } }
        #endregion

        #region 构造
        /// <summary>构造一个NetBIOS记录实例</summary>
        public DNS_NB()
        {
            Type = DNSQueryType.NB;
            Class = DNSQueryClass.IN;
        }
        #endregion

        ///// <summary>已重载。</summary>
        ///// <returns></returns>
        //public override string ToString()
        //{
        //    //// Domain为空，可能是请求
        //    //if (String.IsNullOrEmpty(Domain))
        //    return String.Format("{0} {1}", Type, Name);
        //    //else
        //    //    return String.Format("{0} {1}", Type, Domain);
        //}
    }
}
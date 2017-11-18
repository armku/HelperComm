﻿using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>连接字符串构造器</summary>
    /// <remarks>未稳定，仅供XCode内部使用，不建议外部使用</remarks>
    class XDbConnectionStringBuilder : Dictionary<String, String>
    {
        #region 构造
        /// <summary>实例化不区分大小写的哈希集合</summary>
        public XDbConnectionStringBuilder() : base(StringComparer.OrdinalIgnoreCase) { }
        #endregion

        #region 连接字符串
        /// <summary>连接字符串</summary>
        public String ConnectionString
        {
            get
            {
                if (Count <= 0) return null;

                var sb = new StringBuilder();
                foreach (var item in this)
                {
                    if (sb.Length > 0) sb.Append(";");
                    sb.AppendFormat("{0}={1}", item.Key, item.Value);
                }

                return sb.ToString();
            }
            set
            {
                Clear();
                if (String.IsNullOrEmpty(value)) return;

                var kvs = value.Split(new String[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (kvs == null || kvs.Length <= 0) return;
                foreach (var item in kvs)
                {
                    Int32 p = item.IndexOf("=");
                    // 没有等号，或者等号在第一位，都不合法
                    if (p <= 0) continue;

                    var name = item.Substring(0, p);
                    var val = "";
                    if (p < item.Length - 1) val = item.Substring(p + 1);
                    this[name.Trim()] = val.Trim();
                }
            }
        }
        #endregion

        #region 方法
        /// <summary>获取并删除连接字符串中的项</summary>
        /// <param name="key"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Boolean TryGetAndRemove(String key, out String value)
        {
            value = null;

            if (!ContainsKey(key)) return false;

            value = this[key];
            Remove(key);
            return true;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() { return ConnectionString; }
        #endregion
    }
}
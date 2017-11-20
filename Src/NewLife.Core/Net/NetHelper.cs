using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NewLife.Collections;
using NewLife.Model;
using NewLife.Net;
#if !__MOBILE__ && !__CORE__
using Microsoft.Win32;
using System.Management;
using System.Security.AccessControl;
using NewLife.Log;
#endif

namespace System
{
    /// <summary>网络工具类</summary>
    public static class NetHelper
    {
        #region 辅助函数
        
        private static DictionaryCache<String, IPAddress> _dnsCache = new DictionaryCache<String, IPAddress>(StringComparer.OrdinalIgnoreCase) { Expire = 60, Asynchronous = true };
        /// <summary>分析地址，根据IP或者域名得到IP地址，缓存60秒，异步更新</summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static IPAddress ParseAddress(this String hostname)
        {
            if (String.IsNullOrEmpty(hostname)) return null;

            return _dnsCache.GetItem(hostname, NetUri.ParseAddress);
        }
        
        #endregion
             
    }
}
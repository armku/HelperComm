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
using NewLife.Http;
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
        
        /// <summary>针对IPv4和IPv6获取合适的Any地址</summary>
        /// <remarks>除了Any地址以为，其它地址不具备等效性</remarks>
        /// <param name="address"></param>
        /// <param name="family"></param>
        /// <returns></returns>
        public static IPAddress GetRightAny(this IPAddress address, AddressFamily family)
        {
            if (address.AddressFamily == family) return address;

            switch (family)
            {
                case AddressFamily.InterNetwork:
                    if (address == IPAddress.IPv6Any) return IPAddress.Any;
                    break;
                case AddressFamily.InterNetworkV6:
                    if (address == IPAddress.Any) return IPAddress.IPv6Any;
                    break;
                default:
                    break;
            }
            return null;
        }

        /// <summary>是否Any地址，同时处理IPv4和IPv6</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Boolean IsAny(this IPAddress address) { return IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address); }

        /// <summary>是否Any结点</summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static Boolean IsAny(this EndPoint endpoint) { return (endpoint as IPEndPoint).Address.IsAny() || (endpoint as IPEndPoint).Port == 0; }

        /// <summary>是否IPv4地址</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Boolean IsIPv4(this IPAddress address) { return address.AddressFamily == AddressFamily.InterNetwork; }
        
        #endregion

        #region 本机信息
        /// <summary>获取活动的接口信息</summary>
        /// <returns></returns>
        public static IEnumerable<IPInterfaceProperties> GetActiveInterfaces()
        {
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.OperationalStatus != OperationalStatus.Up) continue;

                var ip = item.GetIPProperties();
                if (ip != null) yield return ip;
            }
        }        
        /// <summary>获取可用的IP地址</summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetIPs()
        {
#if __ANDROID__
            return Dns.GetHostAddresses(Dns.GetHostName());
#endif
#if __IOS__
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                {
                    if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        yield return addrInfo.Address;
                    }
                }
            }
#endif
#if !__MOBILE__

            var list = new List<IPAddress>();
            foreach (var item in GetActiveInterfaces())
            {
                if (item != null && item.UnicastAddresses.Count > 0)
                {
                    foreach (var elm in item.UnicastAddresses)
                    {
                        if (list.Contains(elm.Address)) continue;
                        list.Add(elm.Address);

                        yield return elm.Address;
                    }
                }
            }
#endif
        }

        private static DictionaryCache<Int32, IPAddress[]> _ips = new DictionaryCache<Int32, IPAddress[]> { Expire = 60, Asynchronous = true };
        /// <summary>获取本机可用IP地址，缓存60秒，异步更新</summary>
        /// <returns></returns>
        public static IPAddress[] GetIPsWithCache()
        {
            return _ips.GetItem(1, k => GetIPs().ToArray());
        }
        
        #endregion
     

        #region IP地理位置
        static IpProvider _IpProvider;
                
        /// <summary>IP地址提供者接口</summary>
        public interface IpProvider
        {
            /// <summary>获取IP地址的物理地址位置</summary>
            /// <param name="addr"></param>
            /// <returns></returns>
            String GetAddress(IPAddress addr);
        }        
        #endregion                
        #region 创建客户端和会话
        

        internal static Socket CreateTcp(Boolean ipv4 = true)
        {
            return new Socket(ipv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        }
        
        #endregion
    }
}
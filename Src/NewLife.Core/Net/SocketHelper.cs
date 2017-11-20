using System;
using NewLife.Reflection;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace NewLife.Net
{
    /// <summary>Socket扩展</summary>
    public static class SocketHelper
    {    
        /// <summary>根据异步事件获取可输出异常，屏蔽常见异常</summary>
        /// <param name="se"></param>
        /// <returns></returns>
        internal static Exception GetException(this SocketAsyncEventArgs se)
        {
            if (se == null) return null;

            if (se.SocketError == SocketError.ConnectionReset ||
                se.SocketError == SocketError.OperationAborted ||
                se.SocketError == SocketError.Interrupted ||
                se.SocketError == SocketError.NotSocket)
                return null;

            var ex = se.ConnectByNameError;
            if (ex == null) ex = new SocketException((Int32)se.SocketError);
            return ex;
        }
    }
}
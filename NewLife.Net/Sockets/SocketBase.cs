﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NewLife.Exceptions;
using NewLife.Log;
using NewLife.Net.Common;
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Net.Sockets
{
    /// <summary>Socket基类</summary>
    /// <remarks>
    /// 主要是对Socket封装一层，把所有异步操作结果转移到<see cref="RaiseComplete"/>去。
    /// 
    /// 网络模型处理流程
    /// 1，实例化对象，Socket属性为空，可以由外部赋值。此时可以设置协议和监听地址端口等。
    /// 2，如果外部没有给Socket赋值，EnsureCreate构造Socket。此时可以给Socket设置各种参数。
    /// 3，使用Bind绑定本地监听地址和端口。到此完成了初始化的所有工作。
    /// 
    /// 异步处理流程:
    /// AcceptAsync/ReceiveAsync    异步接受/异步接收
    ///     StartAsync             统一的开始异步方法，如果异步方法同步返回，则采用线程池调用回调方法，转为异步处理
    ///         EnsureCreate       确保已创建Socket
    ///         Bind               确保Socket已绑定到本地地址和端口
    ///         Pop                借出网络参数
    ///         =>RaiseComplete    异步回调方法，处理所有异步事件的起始点
    ///             ->Completed    完成事件，可以取消处理
    ///                 Push       取消处理时，归还网络参数
    ///                 return
    ///             OnComplete     子类通过重载来处理各种异步事件
    ///                 OnAccept/OnReceive
    ///                     Push    如果处理过程中，没有外部注册事件，则马上归还事件，然后开始新的异步操作
    ///                     AcceptAsync/ReceiveAsync
    ///                     return
    ///                 Process
    ///                     ->OnProcess   统一的事件处理核心
    ///                         AcceptAsync/ReceiveAsync
    ///                         OnError 如果异步处理失败，不是<see cref="SocketError.Success"/>，则触发错误事件，然后退出
    ///                             return
    ///                         ProcessAccept/ProcessReceive
    ///                         Push    每次用完都还，保证不出错丢失
    ///                         OnError 处理异常时触发错误事件
    ///                         AcceptAsync/ReceiveAsync
    ///                         OnError 重新开始异步处理出错，触发错误事件，但不干涉当前处理中的网络参数
    ///             OnError
    ///         Push   开始异步处理异常时，归还网络参数再抛出异常
    /// </remarks>
    public class SocketBase : Netbase, ISocket
    {
        #region 属性
        private Socket _Socket;
        /// <summary>套接字</summary>
        internal protected Socket Socket
        {
            get { return _Socket; }
            set
            {
                // 外部设置套接字时，除非是已绑定的，否则不清除本地Uri
                //if (value != null && value.IsBound) _LocalUri = _RemoteUri = null;
                if (value != null && value.IsBound) _LocalUri = null;

                _Socket = value;
            }
        }

        /// <summary>基础Socket对象</summary>
        Socket ISocket.Socket { get { return Socket; } set { Socket = value; } }

        #region 本地
        private NetUri _LocalUri;
        /// <summary>本地地址</summary>
        public NetUri LocalUri
        {
            get
            {
                if (_LocalUri != null) return _LocalUri;

                var uri = new NetUri(ProtocolType, new IPEndPoint(IPAddress.Any, 0));
                uri.ProtocolType = ProtocolType;
                var socket = Socket;
                try
                {
                    if (socket != null)
                    {
                        uri.EndPoint = socket.LocalEndPoint as IPEndPoint;
                        uri.ProtocolType = socket.ProtocolType;
                    }
                }
                catch (ObjectDisposedException) { }

                return _LocalUri = uri;
            }
        }

        /// <summary>协议类型</summary>
        public virtual ProtocolType ProtocolType { get { return _LocalUri == null ? 0 : _LocalUri.ProtocolType; } }

        /// <summary>监听本地地址</summary>
        public IPAddress Address { get { return LocalUri.Address; } set { LocalUri.Address = value; } }

        /// <summary>监听本地端口</summary>
        public Int32 Port { get { return LocalUri.Port; } set { LocalUri.Port = value; } }

        /// <summary>本地地址族</summary>
        public AddressFamily AddressFamily
        {
            get { return Address.AddressFamily; }
            set
            {
                // 根据地址族选择合适的本地地址
                LocalUri.Address = LocalUri.Address.GetRightAny(value);
            }
        }

        /// <summary>本地终结点</summary>
        public IPEndPoint LocalEndPoint { get { return LocalUri.EndPoint; } }
        #endregion

        //private Int32 _BufferSize = 10240;
        //! 注意：大于85K会进入LOH（大对象堆）
        //private Int32 _BufferSize = 80 * 1024;
        private Int32 _BufferSize = 8 * 1024;
        /// <summary>缓冲区大小</summary>
        public Int32 BufferSize { get { return _BufferSize; } set { _BufferSize = value; } }

        //private Boolean _NoDelay = true;
        ///// <summary>禁用接收延迟，收到数据后马上建立异步读取再处理本次数据</summary>
        //public Boolean NoDelay { get { return _NoDelay; } set { _NoDelay = value; } }

        //private Boolean _UseThreadPool;
        ///// <summary>是否使用线程池处理事件。建议仅在事件处理非常耗时时使用线程池来处理。</summary>
        //public Boolean UseThreadPool { get { return _UseThreadPool; } set { _UseThreadPool = value; } }

        /// <summary>不管客户端还是服务端，只允许用一个网络事件</summary>
        NetEventArgs _arg;
        #endregion

        #region 扩展属性
        private Boolean _ReuseAddress;
        /// <summary>允许将套接字绑定到已在使用中的地址。</summary>
        public Boolean ReuseAddress
        {
            get
            {
                if (Socket == null) return _ReuseAddress;

                Object value = Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress);
                return value != null && Convert.ToBoolean(value);
            }
            set
            {
                if (Socket != null) Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value);
                _ReuseAddress = value;
            }
        }

        private IDictionary _Items;
        /// <summary>数据字典</summary>
        public IDictionary Items { get { return _Items ?? (_Items = new Hashtable(StringComparer.OrdinalIgnoreCase)); } }

        private Int32 _AsyncCount;
        /// <summary>异步操作计数</summary>
        public virtual Int32 AsyncCount { get { return _AsyncCount; } /*set { _AsyncCount = value; }*/ }
        #endregion

        #region 构造
        static SocketBase()
        {
            // 注册接口实现
            NetService.Install();

            // 自动收缩内存，调试状态1分钟一次，非调试状态10分钟一次
            Int32 time = NetHelper.Debug ? 60000 : 600000;
            new TimerX(s => Runtime.ReleaseMemory(), null, time, time, false);

#if !NET4
            try
            {
                // 写在另外一个方法里面，保证不会在当前方法编译的时候就报错
                CheckNet21();

                // 回收一次，引发编译NetEventArgs的析构函数
                GC.Collect();
            }
            catch (TypeLoadException ex)
            {
                if (ex.TypeName.Contains("SocketAsyncEventArgs"))
                    throw new XException("NewLife.Net网络库需要.Net2.0 Sp1支持！", ex);
                else
                    throw ex;
            }
#endif
        }

#if !NET4
        static void CheckNet21()
        {
            var e = new NetEventArgs();
            //(e as IDisposable).Dispose();
            e.AcceptSocket = null;
        }
#endif

        /// <summary>实例化</summary>
        public SocketBase()
        {
            // 子类可能重载协议类型
            LocalUri.ProtocolType = ProtocolType;
            //RemoteUri.ProtocolType = ProtocolType;

            _arg = NetEventArgs.Pop();
            _arg.Completed += (s, e) => RaiseComplete(e as NetEventArgs);

            SetShowEventLog();
        }
        #endregion

        #region 释放资源
        /// <summary>关闭网络操作</summary>
        public void Close() { Dispose(); }

        /// <summary>子类重载实现资源释放逻辑</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            //  if (_arg != null) NetEventArgs.Push(_arg); //此处不能做此操作，会导致异常，并且连接无法正常断开，移动把网络清理干净后，再将参数还回池中

            var socket = Socket;
            if (socket != null)
            {
                //NetHelper.Close(socket, ReuseAddress);
                // 此时Socket可能已经销毁，不能进行Socket操作
                NetHelper.Close(socket, _ReuseAddress);
                Socket = null;
            }

            if (_Statistics != null)
            {
                var dp = _Statistics as IDisposable;
                if (dp != null) dp.Dispose();
                _Statistics = null;
            }
            if (_arg != null)
                NetEventArgs.Push(_arg);
        }
        #endregion

        #region 方法
        /// <summary>确保创建基础Socket对象</summary>
        protected virtual void EnsureCreate()
        {
            if (Socket != null || Disposed) return;

            Socket socket = null;
            var addrf = AddressFamily;
            var protocol = ProtocolType;
            switch (ProtocolType)
            {
                case ProtocolType.Tcp:
                    socket = new Socket(addrf, SocketType.Stream, protocol);
                    socket.SetTcpKeepAlive(true);
                    break;
                case ProtocolType.Udp:
                    socket = new Socket(addrf, SocketType.Dgram, protocol);
                    break;
                default:
                    socket = new Socket(addrf, SocketType.Unknown, protocol);
                    break;
            }

            // 设置超时时间
            socket.SendTimeout = 10000;
            socket.ReceiveTimeout = 10000;

            // 加大接收缓冲区，为BufferSize的10倍，但不超过80k
            var bufsize = BufferSize * 10;
            if (bufsize > 80 * 1024) bufsize = 80 * 1024;
            socket.ReceiveBufferSize = bufsize;

            Socket = socket;

            if (_ReuseAddress) ReuseAddress = true;
        }

        /// <summary>绑定本地终结点</summary>
        public virtual void Bind()
        {
            if (Disposed) return;

            EnsureCreate();
            var socket = Socket;
            if (socket != null && !socket.IsBound)
            {
                var ep = new IPEndPoint(Address, Port);
                socket.Bind(ep);

                //_LocalUri = _RemoteUri = null;
                _LocalUri = null;
            }
        }

        /// <summary>开始异步操作。</summary>
        /// <param name="callback"></param>
        internal protected void StartAsync(Func<NetEventArgs, Boolean> callback)
        {
            if (Disposed) return;

            EnsureCreate();
            var socket = Socket;
            if (socket == null) return;

            // Accept得到的socket不需要绑定
            if (!socket.IsBound) Bind();

            var e = _arg;
            // 如果立即返回，则异步处理完成事件
            if (!callback(e))
            {
                // 如果已销毁或取消，则不处理
                if (!e.Cancel) ThreadPool.QueueUserWorkItem(state => RaiseComplete(state as NetEventArgs), e);
            }
            else
                // 异步开始，增加一个计数
                Interlocked.Increment(ref _AsyncCount);
        }
        #endregion

        #region 完成事件
        /// <summary>触发完成事件。如果是异步返回，则在IO线程池中执行；如果是同步返回，则在用户线程池中执行。
        /// 可能由工作线程（事件触发）调用，也可能由用户线程通过线程池线程调用。
        /// 作为顶级，将会处理所有异常并调用OnError，其中OnError有能力回收参数e。
        /// </summary>
        /// <param name="e"></param>
        void RaiseComplete(NetEventArgs e)
        {
            // 异步完成，减少一个计数
            Interlocked.Decrement(ref _AsyncCount);

            if (ShowEventLog && Log.Level >= LogLevel.Debug) ShowEvent(e);

            try
            {
                // 这里直接处理操作取消
                if (e.SocketError != SocketError.OperationAborted)
                    OnComplete(e);
                else
                    OnError(e, null);
            }
            catch (Exception ex)
            {
                // 都是在线程池线程里面了，不要往外抛出异常
                OnError(e, ex);
            }
        }

        /// <summary>完成事件分发中心。
        /// 正常执行时OnComplete必须保证回收参数e，异常时RaiseComplete将能够代为回收
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnComplete(NetEventArgs e) { }
        #endregion

        #region 异步结果处理
        /// <summary>处理异步结果。</summary>
        /// <param name="e">事件参数</param>
        /// <param name="start">开始新异步操作的委托</param>
        /// <param name="process">处理结果的委托</param>
        protected virtual void Process(NetEventArgs e, Func start, Action<NetEventArgs> process)
        {
            // Socket错误由各个处理器来处理
            if (e.SocketError != SocketError.Success)
            {
                OnError(e, null);
                return;
            }

            try
            {
                // 业务处理的任何异常，都将引发Error事件，但不会影响重新建立新的异步操作
                process(e);
            }
            catch (Exception ex)
            {
                try
                {
                    OnError(e, ex);
                }
                catch { }

                return;
            }

            // 如果不是操作取消，在处理业务完成后再开始异步操作
            if (!Disposed) start();
        }
        #endregion

        #region 错误处理
        /// <summary>错误发生/断开连接时</summary>
        public event EventHandler<ExceptionEventArgs> Error;

        /// <summary>错误发生/断开连接时。拦截Error事件中的所有异常，不外抛，防止因为Error异常导致多次调用OnError</summary>
        /// <param name="e"></param>
        /// <param name="ex"></param>
        internal protected void ProcessError(NetEventArgs e, Exception ex)
        {
            if (Error != null)
            {
                try
                {
                    Error(this, new ExceptionEventArgs { Exception = ex });
                }
                catch (Exception ex2)
                {
                    WriteLog(ex2.ToString());
                }
            }
        }

        /// <summary>错误发生时。负责调用Error事件以及回收网络事件参数</summary>
        /// <remarks>OnError除了会调用ProcessError外，还会关闭Socket</remarks>
        /// <param name="e"></param>
        /// <param name="ex"></param>
        protected virtual void OnError(NetEventArgs e, Exception ex)
        {
            try
            {
                ProcessError(e, ex);
            }
            finally
            {
                Close();
            }
        }
        #endregion

        #region 辅助
        /// <summary>检查缓冲区大小</summary>
        /// <param name="e"></param>
        [Conditional("DEBUG")]
        internal protected void CheckBufferSize(NetEventArgs e)
        {
            //Int32 n = e.BytesTransferred;
            //if (n >= e.Buffer.Length || ProtocolType == ProtocolType.Tcp && n >= 1460 || ProtocolType == ProtocolType.Udp && n >= 1464)
            //{
            //    WriteLog("接收的实际数据大小{0}超过了缓冲区大小，需要根据真实MTU调整缓冲区大小以提高效率！", n);
            //}
        }

        private Boolean _ShowEventLog;
        /// <summary>是否显示事件日志</summary>
        public Boolean ShowEventLog { get { return _ShowEventLog; } set { _ShowEventLog = value; } }

        [Conditional("DEBUG")]
        void SetShowEventLog() { ShowEventLog = true; }

        void ShowEvent(NetEventArgs e)
        {
            //WriteLog("Completed[{4}] {0} {1} {2} [{3}]", this, e.LastOperation, e.SocketError, e.BytesTransferred, e.ID);
            var sb = new StringBuilder();
            sb.AppendFormat("[{0}] {1} {2}://{3}", e.ID, e.LastOperation, ProtocolType, LocalEndPoint);
            var ep = e.RemoteIPEndPoint;
            //if (ep == null || ep.Address.IsAny()) ep = RemoteEndPoint;
            if ((ep == null || ep.Address.IsAny()) && e.LastOperation == SocketAsyncOperation.Accept && e.AcceptSocket != null) ep = e.AcceptSocket.RemoteEndPoint as IPEndPoint;
            if (ep != null && !ep.Address.IsAny()) sb.AppendFormat("=>{0}", ep);
            //sb.AppendFormat(" {0}", e.LastOperation);
            if (e.SocketError != SocketError.Success) sb.AppendFormat(" {0}", e.SocketError);
            sb.AppendFormat(" [{0}]", e.BytesTransferred);
            WriteLog(sb.ToString());
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            ////if (RemoteEndPoint == null)
            //return String.Format("{0}://{1}", ProtocolType, LocalEndPoint);
            ////else
            ////    return String.Format("{0}://{1}=>{2}", ProtocolType, LocalEndPoint, RemoteEndPoint);
            return LocalUri.ToString();
        }
        #endregion

        #region 统计
        private IStatistics _Statistics;
        /// <summary>统计信息，默认关闭，通过<see cref="IStatistics.Enable"/>打开。</summary>
        public IStatistics Statistics { get { return _Statistics ?? (_Statistics = NetService.Container.Resolve<IStatistics>()); } }

        /// <summary>增加计数</summary>
        protected void IncCounter() { Statistics.Increment(); }
        #endregion
    }
}
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>基础Socket接口</summary>
    /// <remarks>
    /// 封装所有基础接口的共有特性！
    /// 
    /// 核心设计理念：事件驱动，接口统一，简单易用！
    /// 异常处理理念：确保主流程简单易用，特殊情况的异常通过事件处理！
    /// </remarks>
    public interface ISocket : IDisposable2
    {
        #region 属性
        /// <summary>名称。主要用于日志输出</summary>
        String Name { get; set; }

        /// <summary>基础Socket对象</summary>
        Socket Client { get; /*set;*/ }

        /// <summary>本地地址</summary>
        NetUri Local { get; set; }

        /// <summary>端口</summary>
        Int32 Port { get; set; }

        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        Boolean ThrowException { get; set; }

        /// <summary>异步处理接收到的数据。</summary>
        /// <remarks>异步处理有可能造成数据包乱序，特别是Tcp。true利于提升网络吞吐量。false避免拷贝，提升处理速度</remarks>
        Boolean ProcessAsync { get; set; }

        /// <summary>发送统计</summary>
        IStatistics StatSend { get; set; }

        /// <summary>接收统计</summary>
        IStatistics StatReceive { get; set; }

        /// <summary>日志提供者</summary>
        ILog Log { get; set; }

        /// <summary>是否输出发送日志。默认false</summary>
        Boolean LogSend { get; set; }

        /// <summary>是否输出接收日志。默认false</summary>
        Boolean LogReceive { get; set; }
        #endregion

        #region 方法
        /// <summary>已重载。日志加上前缀</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void WriteLog(String format, params Object[] args);
        #endregion

        #region 事件
        /// <summary>错误发生/断开连接时</summary>
        event EventHandler<ExceptionEventArgs> Error;
        #endregion
    }

    /// <summary>远程通信Socket，仅具有收发功能</summary>
    public interface ISocketRemote : ISocket
    {
        #region 属性
        /// <summary>远程地址</summary>
        NetUri Remote { get; set; }

        /// <summary>通信开始时间</summary>
        DateTime StartTime { get; }

        /// <summary>最后一次通信时间，主要表示会话活跃时间，包括收发</summary>
        DateTime LastTime { get; }

        /// <summary>缓冲区大小</summary>
        Int32 BufferSize { get; set; }
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="Remote"/>决定
        /// </remarks>
        /// <param name="pk">数据包</param>
        /// <returns>是否成功</returns>
        Boolean Send(Packet pk);
        #endregion

        #region 接收
        /// <summary>接收数据。阻塞当前线程等待返回</summary>
        /// <returns></returns>
        Packet Receive();

        /// <summary>数据到达事件</summary>
        event EventHandler<ReceivedEventArgs> Received;

        /// <summary>消息到达事件</summary>
        event EventHandler<MessageEventArgs> MessageReceived;
        #endregion

        #region 数据包处理
        /// <summary>粘包处理接口</summary>
        IPacket Packet { get; set; }

        /// <summary>异步发送数据并等待响应</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        Task<Packet> SendAsync(Packet pk);

        /// <summary>发送消息并等待响应</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        Task<IMessage> SendAsync(IMessage msg);
        #endregion
    }    
}
﻿using System.Reflection;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("新生命网络库")]
[assembly: AssemblyDescription("网络通讯基础框架及各种协议实现")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("NewLife.Net")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyCopyright("©2002-2017 新生命开发团队 http://www.NewLifeX.com")]
[assembly: AssemblyTrademark("四叶草")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("53d97538-79f2-49ac-890c-4cf4a0463946")]

// 程序集的版本信息由下面四个值组成:
//
//      主版本
//      次版本 
//      内部版本号
//      修订号
//
// 可以指定所有这些值，也可以使用“内部版本号”和“修订号”的默认值，
// 方法是按如下所示使用“*”:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("3.2.*")]
[assembly: AssemblyFileVersion("3.2.2016.0204")]

/*
 * v3.2.2016.0204   增加网络统计，包括会话、发送、接收
 * 
 * v3.1.2015.0511   增加日志服务器LogServer，用于接收网络日志
 * 
 * v3.0.2014.1202   第三代网络库完成，回归APM模型，以简单为核心理念。网络基础测试通过，Tcp压力测试2w通过
 * 
 * v2.3.2013.0917   Modbus数据存储增加不触发写入事件的索引器操作
 * 
 * v2.3.2013.0716   升级Modbus协议，不再使用二进制序列化，保持和MF兼容
 * 
 * v2.2.2012.0420   采用NetUri精简代理会话
 *                  HttpProxy增加403缓存和静态文件缓存
 * 
 * v2.1.2012.0411   精简核心结构，进一步完善网络模型
 * 
 * v2.0.2012.0229   完全重构网络模型。划分为四层：Socket层、封装层ISocket、数据会话层ISocketSession、网络会话INetSession
 * 
 * v1.7.2012.0209   完善串口服务器和DNS服务器
 * 
 * v1.7.2011.1228   抽象网络接口，方便编写应用服务
 *                  优化事件参数对象池，避免内存泄漏
 *                  增加DNS协议解析
 *                  设计代理服务器架构
 *                  **经过几天的Echo压力测试，服务端连接数达到143711，CPU在1%~10%，内存WorkSet 523M，PrivateBytes 591M
 *                  一系列测试表明，服务端似乎没有连接上限，每次测试都以客户端端口用完导致“队列已满”异常而告终。
 *                  MS承认网络异步操作有内存泄漏，大量OverlappedData会留在内存，官方建议做法是减少异步操作。
 *                  此外，360安全软件会导致网络性能变得很差，客户端很容易崩溃。
 * 
 * v1.6.2011.0626   修正一个严重错误，TcpServer开始异步Accept时，不需要设置缓冲区，否则客户端连接后不会马上得到Accept事件，
 *                  而必须等到第一个数据包的到来才触发，那时缓冲区里面同时带有第一个数据包的数据。
 * 
 * v1.6.2011.0624   全面支持IPV6，并实现了隐式探测支持，只要用到IPV6的地址（本地和远程），就自动采用IPV6通信
 * 
 * v1.5.2011.0413   改善NetServer，不再作为抽象基类，通过指定协议类型可直接创建Tcp/Udp服务器
 *                  重写Echo服务器和数据流服务器，把Tcp/Udp的实现统一起来
 *                  实现另外四个简单网络服务器
 * 
 * v1.4.2010.1201   增加Socket数据流SocketStream
 *                  增加Tcp和Udp数据流网络服务
 * 
 * v1.3.2010.1107   使用SocketAsyncEventArgs重建网络模型
 *                  2010-11-16 调整完成，基本上满足万级连接下工作的要求，一万以上连接需要服务器拥有更多的处理器核心，硬件越强，程序工作越强！
 * 
 * v1.2.2010.0915   优化读写内存流ReadWriteStream，增加读写阻塞和超时、超大时自动压缩缓冲区的功能
 * 
 * v1.1.2010.0816   增加增强的TCP客户端TcpClientEx，使用内存流解决粘包问题
 * 
 * v1.0.2010.0803   创建网络库
 *
**/
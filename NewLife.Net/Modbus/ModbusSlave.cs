﻿#region Modbus协议
/*
 * GB/T 19582.1-2008 基于Modbus协议的工业自动化网络规范
 * 请求响应：1字节功能码|n字节数据|2字节CRC校验
 * 异常响应：1字节功能码+0x80|1字节异常码
 * 
 * Modbus数据模型基本表
 * 基本表        对象类型   访问类型    注释
 * 离散量输入    单个位     只读        I/O系统可提供这种类型的数据
 * 线圈          单个位     读写        通过应用程序可改变这种类型的数据
 * 输入寄存器    16位字     只读        I/O系统可提供这种类型的数据
 * 保持寄存器    16位字     读写        通过应用程序可改变这种类型的数据
 * 
 */
#endregion

using System;

namespace NewLife.Net.Modbus
{
    /// <summary>Modbus从站</summary>
    /// <example>
    /// <code>
    /// var slave = new ModbusSlave();
    /// slave.Transport = new UdpTransport(502);
    /// slave.Listen();
    /// </code>
    /// </example>
    public class ModbusSlave : IDisposable
    {
        #region 属性
        private Byte _Host;
        /// <summary>主站ID</summary>
        public Byte Host { get { return _Host; } set { _Host = value; } }

        private IDataStore _DataStore;
        /// <summary>数据存储</summary>
        public IDataStore DataStore { get { return _DataStore ?? (_DataStore = new DataStore()); } set { _DataStore = value; } }

        private ITransport[] _Transports = new ITransport[4];
        /// <summary>传输口</summary>
        public ITransport[] Transports { get { return _Transports; } /*set { _Transport = value; }*/ }

        private Boolean _EnableDebug;
        /// <summary>启用调试</summary>
        public Boolean EnableDebug { get { return _EnableDebug; } set { _EnableDebug = value; } }

        //private Boolean inited;
        #endregion

        #region 构造
        /// <summary>析构</summary>
        ~ModbusSlave() { Dispose(false); }

        /// <summary>销毁</summary>
        public void Dispose() { Dispose(true); }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing) GC.SuppressFinalize(this);

            //if (Transport != null) Transport.Dispose();
            for (var i = 0; i < Transports.Length; i++)
            {
                if (Transports[i] != null) Transports[i].Dispose();
            }
        }
        #endregion

        #region Modbus功能
        /// <summary>开始监听</summary>
        /// <returns></returns>
        public virtual ModbusSlave Listen(ITransport transport)
        {
            if (transport == null) throw new ArgumentNullException("transport");

            // 找到一个空位，放入数组
            for (var i = 0; i < Transports.Length; i++)
            {
                if (Transports[i] == null)
                {
                    Transports[i] = transport;
                    break;
                }
            }

            var name = transport.ToString();

#if !MF
            transport.Received += (s, e) => { e.Data = Process(e.Data); };
#else
            transport.Received += (ts, data) => { return Process(data); };
#endif
            transport.Open();

            WriteLine(GetType().Name + "在" + name + "上监听Host=" + Host);

            return this;
        }

        /// <summary>处理Modbus消息</summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public virtual Byte[] Process(Byte[] buf)
        {
#if DEBUG
            var str = "Request :";
            for (Int32 i = 0; i < buf.Length; i++)
            {
                str += " " + buf[i].ToString("X2");
            }
            WriteLine(str);
#endif

            // 处理
            var entity = new ModbusEntity().Parse(buf);
            // 检查主机
            if (entity.Host != 0 && entity.Host != Host) return null;
            // 检查Crc校验
            var crc = buf.Crc(0, buf.Length - 2);
            if (crc != entity.Crc)
                entity.SetError(Errors.CrcError);
            else
                entity = Process(entity);
            buf = entity.ToArray();

#if DEBUG
            str = "Response:";
            for (Int32 i = 0; i < buf.Length; i++)
            {
                str += " " + buf[i].ToString("X2");
            }
            WriteLine(str);
            WriteLine("");
#endif
            return buf;
        }

        /// <summary>处理Modbus消息</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual ModbusEntity Process(ModbusEntity entity)
        {
            // 如果是广播消息，则设置主站ID，便于其他人知道我的主站ID
            if (entity.Host == 0) entity.Host = Host;
            try
            {
                switch (entity.Function)
                {
                    case MBFunction.ReadCoils:
                    case MBFunction.ReadInputs:
                        entity = ReadCoils(entity);
                        break;
                    case MBFunction.ReadHoldingRegisters:
                    case MBFunction.ReadInputRegisters:
                        entity = ReadRegisters(entity);
                        break;
                    case MBFunction.WriteSingleCoil:
                        entity = WriteSingleCoil(entity);
                        break;
                    case MBFunction.WriteSingleRegister:
                        entity = WriteSingleRegister(entity);
                        break;
                    case MBFunction.WriteMultipleCoils:
                        entity = WriteMultipleCoils(entity);
                        break;
                    case MBFunction.WriteMultipleRegisters:
                        entity = WriteMultipleRegisters(entity);
                        break;
                    case MBFunction.Diagnostics:
                        entity = Diagnostics(entity);
                        break;
                    case MBFunction.ReportIdentity:
                        entity = ReportIdentity(entity);
                        break;
                    default:
                        // 不支持的功能码
                        return entity.SetError(Errors.FunctionCode);
                }

                return entity;
            }
            catch (Exception ex)
            {
                //WriteLine(ex.Message);
#if MF
                Microsoft.SPOT.Debug.Print(ex.Message);
#else
                NewLife.Log.XTrace.WriteLine(ex.ToString());
#endif

                // 执行错误
                return entity.SetError(Errors.ProcessError);
            }
        }
        #endregion

        #region 线圈
        /// <summary>读状态 离散量输入/线圈</summary>
        /// <remarks>
        /// 线圈
        /// 请求：0x01|2字节起始地址|2字节线圈数量(1~2000)
        /// 响应：0x01|1字节字节计数|n字节线圈状态（n=输出数量/8，如果余数不为0，n=n+1）
        /// 
        /// 离散量输入
        /// 请求：0x02|2字节起始地址|2字节输入数量(1~2000)
        /// 响应：0x02|1字节字节计数|n字节输入状态（n=输入数量/8，如果余数不为0，n=n+1）
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        ModbusEntity ReadCoils(ModbusEntity entity)
        {
            var data = entity.Data;
            // 无效功能指令
            if (data == null || data.Length != 4) return entity.SetError(Errors.MessageLength);

            var addr = data.ReadUInt16(0);
            var count = data.ReadUInt16(2);
            // 输出数量不正确 count <= 0x07D0=2000
            if (count == 0 || count > 0x07D0) return entity.SetError(Errors.Count);

            IBitStore store = null;
#if DEBUG
            var func = "";
#endif
            switch (entity.Function)
            {
                case MBFunction.ReadCoils:
                    store = DataStore.Coils;
#if DEBUG
                    func = "ReadCoils";
#endif
                    break;
                case MBFunction.ReadInputs:
                    store = DataStore.Inputs;
#if DEBUG
                    func = "ReadInputs";
#endif
                    break;
                default:
                    break;
            }

            // 起始地址+数量 不正确
            if (addr + count >= store.Count) return entity.SetError(Errors.Address);
#if DEBUG
            WriteLine(func + "(0x" + addr.ToString("X2") + ", 0x" + count.ToString("X2") + ")");
#endif
            if (OnReadCoil != null) OnReadCoil(entity, addr, count);

            // 返回的时候，用字节存储每一个线圈的状态
            var n = count >> 3;
            if ((count & 0x07) != 0) n++;
            var buf = new Byte[1 + n];
            // 字节数
            buf[0] = (Byte)n;
            // 元素存放于m字节n位
            var m = n = 0;
            for (var i = 0; i < count; i++)
            {
                var p = store.Read(addr + i);

                // 存放在m个字节的n位，注意前面预留一个字节
                if (p) buf[1 + m] |= (Byte)(1 << n);
                if (++n >= 8)
                {
                    m++;
                    n = 0;
                }
            }

            entity.Data = buf;

            return entity;
        }

        /// <summary>写单个线圈</summary>
        /// <remarks>
        /// 请求：0x05|2字节输出地址|2字节输出值（0x0000/0xFF00）
        /// 响应：0x05|2字节输出地址|2字节输出值（0x0000/0xFF00）
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        ModbusEntity WriteSingleCoil(ModbusEntity entity)
        {
            var data = entity.Data;
            // 无效功能指令
            if (data == null || data.Length < 4) return entity.SetError(Errors.MessageLength);

            var addr = data.ReadUInt16(0);
            var val = data.ReadUInt16(2);
            // 输出值 False=0 True=0xFF00
            if (val != 0 && val != 0xFF00) return entity.SetError(Errors.Value);

            var store = DataStore.Coils;
            // 输出地址
            if (addr >= store.Count) return entity.SetError(Errors.Address);

            var flag = val != 0;

#if DEBUG
            WriteLine("WriteSingleCoil(0x" + addr.ToString("X2") + ", " + flag + ")");
#endif

            //store.Write(addr, flag);

            var count = 0;
            // 支持一下连续写入
            for (var i = 2; i + 1 < data.Length; i += 2, count++)
            {
                store.Write(addr + count, data.ReadUInt16(i) != 0);

            }

            if (OnWriteCoil != null) OnWriteCoil(entity, addr, count);

            // 读出来
            for (var i = 2; i + 1 < data.Length; i += 2)
            {
                data.WriteUInt16(i, (UInt16)(store.Read(addr + i - 2) ? 0xFF00 : 0));
            }

            //// 读出来
            //data.WriteUInt16(2, (UInt16)(store.Read(addr) ? 0xFF00 : 0));

            return entity;
        }

        /// <summary>写多个线圈</summary>
        /// <remarks>
        /// 请求：0x0F|2字节起始地址|2字节输出数量（1~1698）|1字节字节计数|n字节输出值（n=输出数量/8，如果余数不为0，n=n+1）
        /// 响应：0x0F|2字节起始地址|2字节输出数量
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        ModbusEntity WriteMultipleCoils(ModbusEntity entity)
        {
            var data = entity.Data;
            // 2字节地址，2字节数量，1字节计数，至少1字节的数据字节
            if (data == null || data.Length < 2 + 2 + 1 + 1) return entity.SetError(Errors.MessageLength);

            var addr = data.ReadUInt16(0);
            var size = data.ReadUInt16(2);
            var count = data[4];

            // 输出数量
            if (size > 0x07B0 || count + 5 != data.Length) return entity.SetError(Errors.Count);

            var store = DataStore.Coils;
            // 起始地址+输出数量
            if (addr + size >= store.Count) return entity.SetError(Errors.Address);

#if DEBUG
            WriteLine("WriteMultipleCoils(0x" + addr.ToString("X2") + ", 0x" + size.ToString("X2") + ")");
#endif

            // 元素存放于m字节n位
            Int32 m = 0, n = 0;
            for (var i = 0; i < size; i++)
            {
                // 数据位于5+m字节的n位
                var flag = ((data[5 + m] >> n) & 0x01) == 0x01;

                store.Write(addr + i, flag);

                if (++n >= 8)
                {
                    m++;
                    n = 0;
                }
            }

            if (OnWriteCoil != null) OnWriteCoil(entity, addr, size);

            // 响应只要这么一点点
            entity.Data = data.ReadBytes(0, 4);

            return entity;
        }

        /// <summary>读取线圈前触发</summary>
        public event ModbusHandler OnReadCoil;

        /// <summary>写入线圈后触发</summary>
        public event ModbusHandler OnWriteCoil;
        #endregion

        #region 寄存器
        /// <summary>读取寄存器 输入寄存器/保持寄存器</summary>
        /// <remarks>
        /// 保持寄存器
        /// 请求：0x03|2字节起始地址|2字节寄存器数量（1~2000）
        /// 响应：0x03|1字节字节数|n*2字节寄存器值
        /// 
        /// 输入寄存器
        /// 请求：0x04|2字节起始地址|2字节输入寄存器数量（1~2000）
        /// 响应：0x04|1字节字节数|n*2字节输入寄存器
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        ModbusEntity ReadRegisters(ModbusEntity entity)
        {
            var data = entity.Data;
            // 无效功能指令
            if (data == null || data.Length != 4) return entity.SetError(Errors.MessageLength);

            var addr = data.ReadUInt16(0);
            var count = data.ReadUInt16(2);
            // 输出数量不正确 count <= 0x07D0=2000
            //if (count == 0 || count > 0x07D0) return entity.SetError(3);
            if (count == 0) return entity.SetError(Errors.Count);

            IWordStore store = null;
#if DEBUG
            var func = "";
#endif
            switch (entity.Function)
            {
                case MBFunction.ReadHoldingRegisters:
                    store = DataStore.HoldingRegisters;
#if DEBUG
                    func = "ReadHoldingRegisters";
#endif
                    break;
                case MBFunction.ReadInputRegisters:
                    store = DataStore.InputRegisters;
#if DEBUG
                    func = "ReadInputRegisters";
#endif
                    break;
                default:
                    break;
            }
            if (count > store.Count) return entity.SetError(Errors.Count);
            // 起始地址+数量 不正确
            if (addr + count > 0xFFFF) return entity.SetError(Errors.Address);

#if DEBUG
            WriteLine(func + "(0x" + addr.ToString("X2") + ", 0x" + count.ToString("X2") + ")");
#endif
            if (OnReadRegister != null) OnReadRegister(entity, addr, count);

            var buf = new Byte[1 + count * 2];
            buf[0] = (Byte)(count * 2);

            for (var i = 0; i < count; i++)
            {
                buf.WriteUInt16(1 + i * 2, store.Read(addr + i));
            }

            // 读出来
            entity.Data = buf;

            return entity;
        }

        /// <summary>写单个寄存器</summary>
        /// <remarks>
        /// 请求：0x06|2字节寄存器地址|2字节寄存器值
        /// 响应：0x06|2字节寄存器地址|2字节寄存器值
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        ModbusEntity WriteSingleRegister(ModbusEntity entity)
        {
            var data = entity.Data;
            // 无效功能指令
            if (data == null || data.Length < 4) return entity.SetError(Errors.MessageLength);

            var addr = data.ReadUInt16(0);
            var val = data.ReadUInt16(2);
            // 寄存器值 0<<val<<0xFFFF
            //if (val != 0 && val != 0xFF00) return entity.SetError(3);

            var store = DataStore.HoldingRegisters;
            // 寄存器地址
            if (addr >= store.Count) return entity.SetError(Errors.Address);

#if DEBUG
            WriteLine("WriteSingleRegister(0x" + addr.ToString("X2") + ", 0x" + val.ToString("X2") + ")");
#endif

            //store.Write(addr, val);
            var count = 0;
            // 支持多字连续写入
            for (var i = 2; i + 1 < data.Length; i += 2, count++)
            {
                store.Write(addr + count, data.ReadUInt16(i));
            }

            if (OnWriteRegister != null) OnWriteRegister(entity, addr, count);

            return entity;
        }

        /// <summary>写多个寄存器</summary>
        /// <remarks>
        /// 请求：0x10|2字节起始地址|2字节寄存器数量（1~123）|1字节字节计数|n*2寄存器值
        /// 响应：0x10|2字节起始地址|2字节寄存器数量
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        ModbusEntity WriteMultipleRegisters(ModbusEntity entity)
        {
            var data = entity.Data;
            // 2字节地址，2字节数量，1字节计数，至少1字节的数据字节
            if (data == null || data.Length < 2 + 2 + 1 + 2) return entity.SetError(Errors.MessageLength);

            var addr = data.ReadUInt16(0);
            var size = data.ReadUInt16(2);
            var count = data[4];

            // 输出数量
            if (size > 0x07B0 || data.Length - 5 != count) return entity.SetError(Errors.Count);

            var store = DataStore.HoldingRegisters;
            // 起始地址+输出数量
            if (addr + size >= store.Count) return entity.SetError(Errors.Address);

#if DEBUG
            WriteLine("WriteMultipleRegisters(0x" + addr.ToString("X2") + ", 0x" + size.ToString("X2") + ")");
#endif

            for (var i = 0; i < size; i++)
            {
                store.Write(addr + i, data.ReadUInt16(5 + i * 2));
            }

            if (OnWriteRegister != null) OnWriteRegister(entity, addr, size);

            // 响应只要这么一点点
            entity.Data = data.ReadBytes(0, 4);

            return entity;
        }

        /// <summary>读取寄存器前触发</summary>
        public event ModbusHandler OnReadRegister;

        /// <summary>写入寄存器后触发</summary>
        public event ModbusHandler OnWriteRegister;
        #endregion

        #region 诊断标识
        /// <summary>诊断</summary>
        /// <remarks>
        /// 请求：0x08|2字节子功能|n*2字节数据
        /// 响应：0x08|2字节子功能|n*2字节数据
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        ModbusEntity Diagnostics(ModbusEntity entity)
        {
            var data = entity.Data;
            // 无效功能指令。2字节子功能码，多字节的数据
            if (data == null || data.Length < 2) return entity.SetError(Errors.MessageLength);

            var sub = data.ReadUInt16(0);
#if DEBUG
            WriteLine("Diagnostics(0x" + sub.ToString("X2") + ")");
#endif

            // 默认原样返回，暂时没有什么有用的子功能码需要处理
            return entity;
        }

        /// <summary>报告从站ID</summary>
        /// <remarks>
        /// 请求：0x11
        /// 响应：0x11|1字节字节计数|从站ID|运行指示状态（0x00=OFF,0xFF=ON）|附加数据
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        ModbusEntity ReportIdentity(ModbusEntity entity)
        {
            var data = entity.Data;
            // 无效功能指令。
            if (data != null && data.Length > 0) return entity.SetError(Errors.MessageLength);

#if DEBUG
            WriteLine("ReportIdentity()");
#endif

            var hid = new Byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, };

            var buf = new Byte[1 + hid.Length];
            buf[0] = (Byte)hid.Length;
            Array.Copy(hid, 0, buf, 1, hid.Length);
            entity.Data = buf;

            return entity;
        }
        #endregion

        #region 日志
        void WriteLine(String msg)
        {
#if MF
            if (EnableDebug) Microsoft.SPOT.Debug.Print(msg);
#else
            if (EnableDebug) NewLife.Log.XTrace.WriteLine(msg);
#endif
        }
        #endregion
    }

    /// <summary>事件委托</summary>
    /// <param name="entity"></param>
    /// <param name="index"></param>
    /// <param name="count"></param>
    public delegate void ModbusHandler(ModbusEntity entity, Int32 index, Int32 count);

    ///// <summary>写线圈委托</summary>
    ///// <param name="i"></param>
    ///// <param name="value"></param>
    //public delegate void WriteCoilHandler(Int32 i, Int32 count);

    ///// <summary>写寄存器委托</summary>
    ///// <param name="i"></param>
    ///// <param name="value"></param>
    //public delegate void WriteRegisterHandler(Int32 i, Int32 count);
}
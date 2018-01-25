﻿using System;
using System.ComponentModel;
using System.Diagnostics;
#if !__CORE__
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
#endif
using System.Security;
#if !__MOBILE__ && !__CORE__
using System.Web;
using Microsoft.VisualBasic.Devices;
#endif

namespace NewLife
{
    /// <summary>运行时</summary>
    public static class Runtime
    {
        #region 控制台
#if !__MOBILE__ && !__CORE__
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
#endif

        private static Boolean? _IsConsole;
        /// <summary>是否控制台。用于判断是否可以执行一些控制台操作。</summary>
        public static Boolean IsConsole
        {
            get
            {
                if (_IsConsole != null) return _IsConsole.Value;

#if __MOBILE__ || __CORE__
                _IsConsole = false;
#else
                var ip = Win32Native.GetStdHandle(-11);
                if (ip == IntPtr.Zero || ip == INVALID_HANDLE_VALUE)
                    _IsConsole = false;
                else
                {
                    ip = Win32Native.GetStdHandle(-10);
                    if (ip == IntPtr.Zero || ip == INVALID_HANDLE_VALUE)
                        _IsConsole = false;
                    else
                        _IsConsole = true;
                }
#endif

                return _IsConsole.Value;
            }
        }
        #endregion

        #region Web环境
#if __MOBILE__ || __CORE__
        /// <summary>是否Web环境</summary>
        public static Boolean IsWeb { get { return false; } }
#else
        /// <summary>是否Web环境</summary>
        public static Boolean IsWeb { get { return !String.IsNullOrEmpty(HttpRuntime.AppDomainAppId); } }
#endif
        #endregion

        #region Mono
        private static Boolean? _Mono;
        /// <summary>是否Mono环境</summary>
        public static Boolean Mono
        {
            get
            {
                if (_Mono == null) _Mono = Type.GetType("Mono.Runtime") != null;

                return _Mono.Value;
            }
        }
        #endregion

        #region 64位系统
        /// <summary>确定当前操作系统是否为 64 位操作系统。</summary>
        /// <returns>如果操作系统为 64 位操作系统，则为 true；否则为 false。</returns>
        public static Boolean Is64BitOperatingSystem
        {
            [SecuritySafeCritical]
            get
            {
                if (Is64BitProcess) return true;

#if __MOBILE__ || NET4
                return Environment.Is64BitOperatingSystem;
#elif  __CORE__
                return Is64BitProcess;
#else
                Boolean flag;
                return Win32Native.DoesWin32MethodExist("kernel32.dll", "IsWow64Process") && Win32Native.IsWow64Process(Win32Native.GetCurrentProcess(), out flag) && flag;
#endif
            }
        }

        /// <summary>确定当前进程是否为 64 位进程。</summary>
        /// <returns>如果进程为 64 位进程，则为 true；否则为 false。</returns>
        public static Boolean Is64BitProcess { get { return IntPtr.Size == 8; } }
        #endregion

        #region 操作系统
#if __MOBILE__
        private static String _OSName;
        /// <summary>操作系统</summary>
        public static String OSName
        {
            get
            {
                if (_OSName != null) return _OSName;

                _OSName = Environment.OSVersion + "";

                return _OSName;
            }
        }
#elif __CORE__
#else
        private static String _OSName;
        /// <summary>操作系统</summary>
        public static String OSName
        {
            get
            {
                if (_OSName != null) return _OSName;

                var os = Environment.OSVersion;
                var vs = os.Version;
                var is64 = Is64BitOperatingSystem;
                var sys = "";

                #region Win32
                if (os.Platform == PlatformID.Win32Windows)
                {
                    // 非NT系统
                    switch (vs.Minor)
                    {
                        case 0:
                            sys = "95";
                            break;
                        case 10:
                            if (vs.Revision.ToString() == "2222A")
                                sys = "98SE";
                            else
                                sys = "98";
                            break;
                        case 90:
                            sys = "Me";
                            break;
                        default:
                            sys = vs.ToString();
                            break;
                    }
                    sys = "Windows " + sys;
                }
                #endregion
                else if (os.Platform == PlatformID.Win32NT)
                {
                    sys = GetNTName(vs);
                    if (sys.IsNullOrEmpty())
                        sys = os.ToString();
                    else
                        sys = "Windows " + sys;
                }

                if (sys.IsNullOrEmpty()) sys = os.ToString();

                // 补丁
                if (os.ServicePack != "") sys += " " + os.ServicePack;

                if (is64) sys += " x64";

                return _OSName = sys;
            }
        }

        static String GetNTName(Version vs)
        {
            if (Mono) return null;

            var ver = new Win32Native.OSVersionInfoEx();
            if (!Win32Native.GetVersionEx(ver)) ver = null;
            var isStation = ver == null || ver.ProductType == OSProductType.WorkStation;

            var is64 = Is64BitOperatingSystem;

            const Int32 SM_SERVERR2 = 89;
            var IsServerR2 = Win32Native.GetSystemMetrics(SM_SERVERR2) == 1;

            var sys = "";
            switch (vs.Major)
            {
                case 3:
                    sys = "NT 3.51";
                    break;
                case 4:
                    sys = "NT 4.0";
                    break;
                case 5:
                    if (vs.Minor == 0)
                    {
                        sys = "2000";
                        if (ver != null && ver.ProductType != OSProductType.WorkStation)
                        {
                            if (ver.SuiteMask == OSSuites.Enterprise)
                                sys += " Datacenter Server";
                            else if (ver.SuiteMask == OSSuites.Datacenter)
                                sys += " Advanced Server";
                            else
                                sys += " Server";
                        }
                    }
                    else if (vs.Minor == 1)
                    {
                        sys = "XP";
                        if (ver != null)
                        {
                            if (ver.SuiteMask == OSSuites.EmbeddedNT)
                                sys += " Embedded";
                            else if (ver.SuiteMask == OSSuites.Personal)
                                sys += " Home";
                            else
                                sys += " Professional";
                        }
                    }
                    else if (vs.Minor == 2)
                    {
                        // 64位XP也是5.2
                        if (is64 && ver != null && ver.ProductType == OSProductType.WorkStation)
                            sys = "XP Professional";
                        else if (ver != null && ver.SuiteMask == OSSuites.WHServer)
                            sys = "Home Server";
                        else
                        {
                            sys = "Server 2003";
                            if (IsServerR2) sys += " R2";
                            if (ver != null)
                            {
                                switch (ver.SuiteMask)
                                {
                                    case OSSuites.Enterprise:
                                        sys += " Enterprise";
                                        break;
                                    case OSSuites.Datacenter:
                                        sys += " Datacenter";
                                        break;
                                    case OSSuites.Blade:
                                        sys += " Web";
                                        break;
                                    default:
                                        sys += " Standard";
                                        break;
                                }
                            }
                        }
                    }
                    else
                        sys = String.Format("{0}.{1}", vs.Major, vs.Minor);
                    break;
                case 6:
                    if (vs.Minor == 0)
                        sys = isStation ? "Vista" : "Server 2008";
                    else if (vs.Minor == 1)
                        sys = isStation ? "7" : "Server 2008 R2";
                    else if (vs.Minor == 2)
                    {
                        if (vs.Build == 9200)
                            sys = "10.0";
                        else
                            sys = isStation ? "8" : "Server 2012";
                    }
                    else if (vs.Minor == 3)
                        sys = isStation ? "8.1" : "Server 2012 R2";
                    else
                        sys = String.Format("{0}.{1}", vs.Major, vs.Minor);
                    break;
                case 10:
                    //sys = "10.0";
                    sys = vs.ToString();
                    break;
                default:
                    sys = "NT " + vs.ToString();
                    break;
            }

            return sys;
        }
#endif
        #endregion

        #region 内存设置
#if __MOBILE__
#elif __CORE__
#else
        /// <summary>设置进程的程序集大小，将部分物理内存占用转移到虚拟内存</summary>
        /// <param name="pid">要设置的进程ID</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns></returns>
        public static Boolean SetProcessWorkingSetSize(Int32 pid, Int32 min, Int32 max)
        {
            var p = pid <= 0 ? Process.GetCurrentProcess() : Process.GetProcessById(pid);
            return Win32Native.SetProcessWorkingSetSize(p.Handle, min, max);
        }

        /// <summary>释放当前进程所占用的内存</summary>
        /// <returns></returns>
        public static Boolean ReleaseMemory()
        {
            GC.Collect();

            return SetProcessWorkingSetSize(0, -1, -1);
        }

        private static Int32? _PhysicalMemory;
        /// <summary>物理内存大小。单位MB</summary>
        public static Int32 PhysicalMemory
        {
            get
            {
                if (_PhysicalMemory == null) Refresh();
                return _PhysicalMemory.Value;
            }
        }

        private static Int32? _AvailableMemory;
        /// <summary>可用物理内存大小。单位MB</summary>
        public static Int32 AvailableMemory
        {
            get
            {
                if (_AvailableMemory == null) Refresh();
                return _AvailableMemory.Value;
            }
        }
        
        private static void Refresh()
        {
            if (Mono) return;

            //var ci = new ComputerInfo();
            //_PhysicalMemory = (Int32)(ci.TotalPhysicalMemory / 1024 / 1024);
            //_VirtualMemory = (Int32)(ci.TotalVirtualMemory / 1024 / 1024);

            var st = default(Win32Native.MEMORYSTATUSEX);
            st.Init();
            Win32Native.GlobalMemoryStatusEx(ref st);

            _PhysicalMemory = (Int32)(st.ullTotalPhys / 1024 / 1024);
            _AvailableMemory = (Int32)(st.ullAvailPhys / 1024 / 1024);
            //_VirtualMemory = (Int32)(st.ullTotalVirtual / 1024 / 1024);

        }
#endif
        #endregion
    }

#if __MOBILE__
#elif __CORE__
#else
    /// <summary>标识系统上的程序组</summary>
    [Flags]
    enum OSSuites : UInt16
    {
        //None = 0,
        SmallBusiness = 0x00000001,
        Enterprise = 0x00000002,
        BackOffice = 0x00000004,
        Communications = 0x00000008,
        Terminal = 0x00000010,
        SmallBusinessRestricted = 0x00000020,
        EmbeddedNT = 0x00000040,
        Datacenter = 0x00000080,
        SingleUserTS = 0x00000100,
        Personal = 0x00000200,
        Blade = 0x00000400,
        EmbeddedRestricted = 0x00000800,
        Appliance = 0x00001000,
        WHServer = 0x00008000
    }

    /// <summary>标识系统类型</summary>
    enum OSProductType : Byte
    {
        /// <summary>工作站</summary>
        [Description("工作站")]
        WorkStation = 1,

        /// <summary>域控制器</summary>
        [Description("域控制器")]
        DomainController = 2,

        /// <summary>服务器</summary>
        [Description("服务器")]
        Server = 3
    }

    class Win32Native
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(Int32 nStdHandle);

        [SecurityCritical]
        internal static Boolean DoesWin32MethodExist(String moduleName, String methodName)
        {
            var moduleHandle = GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero) return false;
            return GetProcAddress(moduleHandle, methodName) != IntPtr.Zero;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail), DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(String moduleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, String methodName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetCurrentProcess();

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean IsWow64Process([In] IntPtr hSourceProcessHandle, [MarshalAs(UnmanagedType.Bool)] out Boolean isWow64);

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetProcessWorkingSetSize(IntPtr proc, Int32 min, Int32 max);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern Boolean GetVersionEx([In, Out] OSVersionInfoEx ver);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class OSVersionInfoEx
        {
            public Int32 OSVersionInfoSize;
            public Int32 MajorVersion;        // 系统主版本号
            public Int32 MinorVersion;        // 系统次版本号
            public Int32 BuildNumber;         // 系统构建号
            public Int32 PlatformId;          // 系统支持的平台
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public String CSDVersion;       // 系统补丁包的名称
            public UInt16 ServicePackMajor; // 系统补丁包的主版本
            public UInt16 ServicePackMinor; // 系统补丁包的次版本
            public OSSuites SuiteMask;         // 标识系统上的程序组
            public OSProductType ProductType;        // 标识系统类型
            public Byte Reserved;           // 保留
            public OSVersionInfoEx()
            {
                OSVersionInfoSize = Marshal.SizeOf(this);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern Int32 GetSystemMetrics(Int32 nIndex);

        public struct MEMORYSTATUSEX
        {
            internal UInt32 dwLength;
            internal UInt32 dwMemoryLoad;
            internal UInt64 ullTotalPhys;
            internal UInt64 ullAvailPhys;
            internal UInt64 ullTotalPageFile;
            internal UInt64 ullAvailPageFile;
            internal UInt64 ullTotalVirtual;
            internal UInt64 ullAvailVirtual;
            internal UInt64 ullAvailExtendedVirtual;
            internal void Init()
            {
                dwLength = checked((UInt32)Marshal.SizeOf(typeof(MEMORYSTATUSEX)));
            }
        }

        [SecurityCritical]
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
    }
#endif
}
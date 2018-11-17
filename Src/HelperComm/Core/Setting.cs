using System;
using System.ComponentModel;
using System.IO;
using NewLife.Log;
using NewLife.Xml;

namespace NewLife
{
    /// <summary>核心设置</summary>
    [DisplayName("核心设置")]
#if !__MOBILE__
    [XmlConfigFile(@"Config\Core.config", 15000)]
#endif
    public class Setting : XmlConfig<Setting>
    {
        #region 属性
        
        /// <summary>日志等级，只输出大于等于该级别的日志</summary>
        [Description("日志等级。只输出大于等于该级别的日志")]
        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        /// <summary>文件日志目录</summary>
        [Description("文件日志目录")]
        public String LogPath { get; set; } = "";

        /// <summary>日志文件格式</summary>
        [Description("日志文件格式。默认{0:yyyy_MM_dd}.log")]
        public String LogFileFormat { get; set; } = "{0:yyyy_MM_dd}.log";

        /// <summary>临时目录</summary>
        [Description("临时目录")]
        public String TempPath { get; set; } = "";

        /// <summary>插件目录</summary>
        [Description("插件目录")]
        public String PluginPath { get; set; } = "Plugins";

        /// <summary>插件服务器。将从该网页上根据关键字分析链接并下载插件</summary>
        [Description("插件服务器。将从该网页上根据关键字分析链接并下载插件")]
        public String PluginServer { get; set; } = "http://x.newlifex.com/";

        /// <summary>插件缓存目录。默认位于系统盘的X\Cache</summary>
        [Description("插件缓存目录。默认位于系统盘的X\\Cache")]
        public String PluginCache { get; set; } = "";
        #endregion

        #region 方法
        /// <summary>加载完成后</summary>
        protected override void OnLoaded()
        {
            var web = Runtime.IsWeb;

            if (LogPath.IsNullOrEmpty()) LogPath = web ? "..\\Log" : "Log";
            if (TempPath.IsNullOrEmpty()) TempPath = web ? "..\\XTemp" : "XTemp";
            if (LogFileFormat.IsNullOrEmpty()) LogFileFormat = "{0:yyyy_MM_dd}.log";

#if !__MOBILE__
            if (PluginCache.IsNullOrWhiteSpace())
            {
                // 兼容Linux Mono
                var sys = Environment.SystemDirectory;
                if (sys.IsNullOrEmpty()) sys = "/";
                PluginCache = Path.GetPathRoot(sys).CombinePath("X", "Cache");
            }
#endif
            if (PluginServer.IsNullOrWhiteSpace() || PluginServer.StartsWithIgnoreCase("ftp://")) PluginServer = "http://x.newlifex.com/";

            base.OnLoaded();
        }

        /// <summary>获取插件目录</summary>
        /// <returns></returns>
        public String GetPluginPath()
        {
            return PluginPath.GetBasePath();
        }
        #endregion
    }
}
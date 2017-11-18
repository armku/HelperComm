﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
#if !__CORE__
using System.Windows.Forms;
#endif
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;

namespace NewLife.Net
{
    /// <summary>升级</summary>
    public class Upgrade
    {
        #region 属性
        /// <summary>服务器地址</summary>
        public String Server { get; set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>版本</summary>
        public Version Version { get; set; }

        /// <summary>本地编译时间</summary>
        public DateTime Compile { get; set; }

        /// <summary>更新完成以后自动启动主程序</summary>
        public Boolean AutoStart { get; set; }

        /// <summary>更新目录</summary>
        public String UpdatePath { get; set; }

        /// <summary>临时目录</summary>
        public String TempPath { get; set; }

        /// <summary>超链接信息，其中第一个为最佳匹配项</summary>
        public Link[] Links { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化一个升级对象实例，获取当前应用信息</summary>
        public Upgrade()
        {
            var asm = Assembly.GetEntryAssembly();
            var asmx = AssemblyX.Create(asm);

            Version = asm.GetName().Version;
            Name = asm.GetName().Name;
            Compile = asmx.Compile;

            AutoStart = true;
            UpdatePath = "Update";
            Server = NewLife.Setting.Current.PluginServer;

            TempPath = XTrace.TempPath;
            Links = new Link[0];
        }
        #endregion

        #region 方法
        /// <summary>获取版本信息，检查是否需要更新</summary>
        /// <returns></returns>
        public Boolean Check()
        {
            var url = Server;
            // 如果配置地址未指定参数，则附加参数
            if (url.StartsWithIgnoreCase("http://"))
            {
                if (!url.Contains("{0}"))
                {
                    if (!url.Contains("?"))
                        url += "?";
                    else
                        url += "&";

                    url += String.Format("Name={0}&Version={1}", Name, Version);
                }
                else
                {
                    url = String.Format(url, Name, Version);
                }
            }

            WriteLog("准备获取更新信息 {0}", url);

            var web = CreateClient();
            var html = web.GetHtml(url);
            var links = Link.Parse(html, url, item => item.Name.StartsWithIgnoreCase(Name) || item.Name.Contains(Name));
            if (links.Length < 1) return false;

            // 分析所有链接
            var list = new List<Link>();
            foreach (var link in links)
            {
                // 不是满足条件的name不要
                if (!link.Name.StartsWithIgnoreCase(Name) || !link.Name.Contains(Name)) continue;

                // 第一个时间命中
                if (link.Time.Year <= DateTime.Now.Year)
                {
                    list.Add(link);
                }
            }
            if (list.Count < 1) return false;

            // 按照时间降序
            Links = list.OrderByDescending(e => e.Time).ToArray();

            // 只有文件时间大于编译时间才更新，需要考虑文件编译后过一段时间才打包
            return Links[0].Time > Compile.AddMinutes(30);
        }

        /// <summary>开始更新</summary>
        public void Download()
        {
            if (Links.Length == 0) throw new Exception("没有可用新版本！");

            var link = Links[0];
            if (String.IsNullOrEmpty(link.Url)) throw new Exception("升级包地址无效！");

            // 如果更新包不存在，则下载
            var file = UpdatePath.CombinePath(link.Name).GetFullPath();
            if (!File.Exists(file))
            {
                WriteLog("准备下载 {0} 到 {1}", link.Url, file);

                var sw = Stopwatch.StartNew();

                var web = CreateClient();
                web.DownloadFileAsync(link.Url, file).Wait();

                sw.Stop();
                WriteLog("下载完成！大小{0:n0}字节，耗时{1:n0}ms", file.AsFile().Length, sw.ElapsedMilliseconds);
            }

            // 设置更新标记
            file += ".update";
            WriteLog("设置更新标记 {0}", file);
            File.CreateText(file).Close();
        }

        /// <summary>检查并执行更新操作</summary>
        public Boolean Update()
        {
            // 查找更新目录
            var fis = Directory.GetFiles(UpdatePath, "*.update");
            if (fis == null || fis.Length == 0) return false;

            var file = fis[0].GetFullPath().TrimEnd(".update");
            WriteLog("发现更新包 {0}，删除所有更新标记文件", file);
            foreach (var item in fis)
            {
                try
                {
                    File.Delete(item);
                }
                catch { }
            }

            if (!File.Exists(file)) return false;
            // 如果已经更新过，则也不再更新
            if (File.Exists(file + ".updated")) return false;

            // 解压更新程序包
            if (!file.EndsWithIgnoreCase(".zip")) return false;

            var dest = TempPath.CombinePath(Path.GetFileNameWithoutExtension(file)).GetFullPath();
            WriteLog("解压缩更新包到临时目录 {0}", dest);
            //ZipFile.ExtractToDirectory(file, p);
            file.AsFile().Extract(dest, true);

            var updatebat = UpdatePath.CombinePath("update.bat").GetFullPath();
            MakeBat(updatebat, dest, ".".GetFullPath());

            // 执行更新程序包
            var si = new ProcessStartInfo();
            si.FileName = updatebat;
            si.Arguments = ">>update.log";
            si.WorkingDirectory = Path.GetDirectoryName(si.FileName);
            // 必须创建无窗口进程，否则会等待目标进程完成
            //if (!XTrace.Debug)
            {
                si.CreateNoWindow = true;
                si.WindowStyle = ProcessWindowStyle.Hidden;
            }
            si.UseShellExecute = false;
            Process.Start(si);

            WriteLog("已启动更新程序来升级，升级脚本 {0}", updatebat);

            // 设置更新标记
            file += ".updated";
            WriteLog("设置已更新标记 {0}", file);
            File.CreateText(file).Close();

#if !__CORE__
            Application.Exit();
#endif

            return true;
        }
        #endregion

        #region 辅助
        private WebClientX _Client;
        private WebClientX CreateClient()
        {
            if (_Client != null) return _Client;

            var web = new WebClientX(true, true);
            web.UserAgent = "NewLife.Upgrade";
            return _Client = web;
        }

        void MakeBat(String updatebat, String tmpdir, String curdir)
        {
            var pid = Process.GetCurrentProcess().Id;

            var sb = new StringBuilder();

            sb.AppendLine("@echo off");
            sb.AppendLine("echo %time% 等待主程序[PID={0}]退出".F(pid));
            // 等待2秒(3-1)后，干掉当前进程
            sb.AppendFormat("ping -n 3 127.0.0.1 >nul");
            sb.AppendLine();
            sb.AppendFormat("taskkill /F /PID {0}", pid);
            sb.AppendLine();

            // 备份配置文件
            sb.AppendLine("echo %time% 备份配置文件");
            var cfgs = Directory.GetFiles(curdir);
            foreach (var item in cfgs)
            {
                if (item.EndsWithIgnoreCase(".config", ".xml"))
                {
                    sb.AppendFormat("move /Y \"{0}\" \"{0}.bak\"", item.GetFullPath());
                    sb.AppendLine();
                }
            }

            // 复制
            sb.AppendLine("echo %time% 复制更新文件");
            sb.AppendFormat("copy \"{0}\\*.*\" \"{1}\" /y", tmpdir, curdir);
            sb.AppendLine();
            sb.AppendLine("rd \"" + tmpdir + "\" /s /q");

            // 还原配置文件
            sb.AppendLine("echo %time% 还原配置文件");
            foreach (var item in cfgs)
            {
                if (item.EndsWithIgnoreCase(".config", ".xml"))
                {
                    sb.AppendFormat("move /Y \"{0}.bak\" \"{0}\"", item.GetFullPath());
                    sb.AppendLine();
                }
            }

#if !__CORE__
            // 启动
            if (AutoStart)
            {
                sb.AppendLine("echo %time% 启动主程序");
                var bin = Application.ExecutablePath;
                sb.AppendFormat("start /D \"{0}\" /I {1}", Path.GetDirectoryName(bin), bin);
                sb.AppendLine();
            }
#endif

#if !DEBUG
            //sb.AppendFormat("del \"{0}\" /f/q", updatebat);
            //sb.AppendLine();
#endif

            sb.AppendFormat("ping -n 3 127.0.0.1 >nul");
            sb.AppendLine();
            sb.AppendLine("exit");

            if (File.Exists(updatebat)) File.Delete(updatebat);
            // 批处理文件不能用UTF8编码保存，否则里面的中文会乱码，特别不能用带有BOM的编码输出
            File.WriteAllText(updatebat, sb.ToString(), Encoding.Default);
        }
        #endregion

        #region 日志
        /// <summary>日志对象</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            format = String.Format("[{0}]{1}", Name, format);
            Log?.Info(format, args);
        }
        #endregion
    }
}
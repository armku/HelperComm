using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using NewLife.Threading;
using NewLife.Xml;
using XCom;

namespace XCoder
{
    static class Program
    {
        /// <summary>应用程序的主入口点。</summary>
        [STAThread]
        static void Main()
        {
            XTrace.UseWinForm();
                                    
            try
            {
                //Update(true);

                if (!Runtime.Mono) new TimerX(s => Runtime.ReleaseMemory(), null, 5000, 10000);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());            
        }
    }
}
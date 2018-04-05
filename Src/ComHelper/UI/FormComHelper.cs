using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ComHelper
{
    public partial class FormComHelper : Form
    {
        public FormComHelper()
        {
            InitializeComponent();
        }

        private void FormComHelper_Load(object sender, EventArgs e)
        {
            LoadInfo();
        }
        public void LoadInfo()
        {
            ShowPorts();
            var bs = new List<Int32>(new Int32[] { 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200, 128000, 194000, 256000, 512000, 1024000, 2048000 });
            cbBaundrate.DataSource = bs;
        }
        String _ports = null;
        /// <summary>下拉框显示串口</summary>
        public void ShowPorts()
        {
            var ps = GetPortNames();
            var str = String.Join(",", ps);
            // 如果端口有所改变，则重新绑定
            if (_ports != str)
            {
                _ports = str;

                this.Invoke(() =>
                {
                    var old = cbName.SelectedItem + "";
                    cbName.DataSource = ps;
                    if (!String.IsNullOrEmpty(old) && Array.IndexOf(ps, old) >= 0) cbName.SelectedItem = old;
                });
            }
        }
        /// <summary>获取带有描述的串口名，没有时返回空数组</summary>
        /// <returns></returns>
        public static String[] GetPortNames()
        {
            var list = new List<String>();
            foreach (var item in GetNames())
            {
                list.Add(String.Format("{0}({1})", item.Key, item.Value));
            }
            return list.ToArray();
        }

        /// <summary>获取串口列表，名称和描述</summary>
        /// <returns></returns>
        public static Dictionary<String, String> GetNames()
        {
            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM", false))
            using (var usb = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB", false))
            {
                if (key != null)
                {
                    foreach (var item in key.GetValueNames())
                    {
                        var name = key.GetValue(item) + "";
                        var des = "";

                        // 尝试枚举USB串口
                        foreach (var vid in usb.GetSubKeyNames())
                        {
                            var usbvid = usb.OpenSubKey(vid);
                            foreach (var elm in usbvid.GetSubKeyNames())
                            {
                                var sub = usbvid.OpenSubKey(elm);
                                //if (sub.GetValue("Class") + "" == "Ports")
                                {
                                    var FriendlyName = sub.GetValue("FriendlyName") + "";
                                    if (FriendlyName.Contains("({0})".F(name)))
                                    {
                                        des = FriendlyName.TrimEnd("({0})".F(name)).Trim();
                                        break;
                                    }
                                }
                            }
                            if (!des.IsNullOrEmpty()) break;
                        }

                        // 最后选择设备映射的串口名
                        if (des.IsNullOrEmpty())
                        {
                            des = item;
                            var p = item.LastIndexOf('\\');
                            if (p >= 0) des = des.Substring(p + 1);
                        }

                        //dic.Add(name, des);
                        // 某台机器上发现，串口有重复
                        dic[name] = des;
                    }
                }
            }
            return dic;
        }        
    }
}

using ComHelper.Net;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ComHelper
{
    public partial class FormComHelper : Form
    {
        SerialPort sp = new SerialPort();
        public FormComHelper()
        {
            InitializeComponent();
        }

        private void FormComHelper_Load(object sender, EventArgs e)
        {
            LoadInfo();
            sp.DataReceived += Sp_DataReceived;           
        }

        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {            
            if (sp.BytesToRead > 0)
            {
                var buf = new Byte[sp.BytesToRead];

                var count = sp.Read(buf, 0, buf.Length);
                var str = Encoding.Default.GetString(buf,0,count);
                //txtReceive.Text += str;
                txtReceive.Append(str);
            }
            //InfoAdd("");            
            //if (txtReceive.Lines.Length > 1000)
            //    txtReceive.Clear();
            //txtReceive.Select(txtReceive.TextLength, 0);
            //txtReceive.ScrollToCaret();
        }        
        public void LoadInfo()
        {
            ShowPorts();
            var bs = new List<Int32>(new Int32[] { 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200, 128000, 194000, 256000, 512000, 1024000, 2048000 });
            cbBaundrate.DataSource = bs;
            cbBaundrate.SelectedIndex = 12;
        }
        String _ports = null;
        /// <summary>下拉框显示串口</summary>
        public void ShowPorts()
        {
            var ps =SerialTransport.GetPortNames();
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
        
        private void btnConnect_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn.Text == "打开")
                Connect();
            else
                Disconnect();
        }
        void Connect()
        {
            var name = cbName.SelectedItem + "";
            if (String.IsNullOrEmpty(name))
            {
                MessageBox.Show("请选择串口！", Text);
                cbName.Focus();
                return;
            }
            var p = name.IndexOf("(");
            if (p > 0) name = name.Substring(0, p);
            btnConnect.Text = "关闭";
            sp.PortName = name;
            sp.BaudRate = Convert.ToInt32(cbBaundrate.Text);
            sp.Open();
        }
        void Disconnect()
        {
            //if (spList.Enabled) return;
            if (btnConnect.Text == "打开") return;

            //var st = spList.Port;
            //if (st != null) st.Disconnected -= (s, e) => this.Invoke(Disconnect);
            //spList.Disconnect();

            btnConnect.Text = "打开";
            sp.Close();
        }

        private void btnRcvClear_Click(object sender, EventArgs e)
        {
            txtReceive.Clear();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            var str = txtSend.Text;
            if (String.IsNullOrEmpty(str))
            {
                MessageBox.Show("发送内容不能为空！", Text);
                txtSend.Focus();
                return;
            }
            // 多次发送
            var count = (Int32)numMutilSend.Value;
            var sleep = (Int32)numSleep.Value;
            if (count <= 0) count = 1;
            if (sleep <= 0) sleep = 100;

            // 处理换行
            str = str.Replace("\n", "\r\n");

            if (count == 1)
            {
                sp.Write(str);
                return;
            }
        }
    }
}

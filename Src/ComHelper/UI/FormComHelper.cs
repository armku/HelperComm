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
        SerialTransport sp = new SerialTransport();
        /// <summary>
        /// 接收数量
        /// </summary>
        public int RxCnt { get; set; }
        /// <summary>
        /// 发送数量
        /// </summary>
        public int TxCnt { get; set; }
        public FormComHelper()
        {
            InitializeComponent();
        }

        private void FormComHelper_Load(object sender, EventArgs e)
        {
            LoadInfo();
            timer1.Start();
            txtReceive.SetDefaultStyle(12);
            txtSend.SetDefaultStyle(12);
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
            var ps = SerialTransport.GetPortNames();
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
            sp.Received += Sp_Received;
            try
            {
                sp.Open();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }        
        }

        private void Sp_Received(object sender, ReceivedEventArgs e)
        {
            if (e.Data.Length > 0)
            {
                var str = Encoding.Default.GetString(e.Data, 0, e.Data.Length);
                RxCnt += e.Length;
                txtReceive.Append(str);
            }
        }

        void Disconnect()
        {
            //if (spList.Enabled) return;
            if (btnConnect.Text == "打开") return;

            //var st = spList.Port;
            //if (st != null) st.Disconnected -= (s, e) => this.Invoke(Disconnect);
            //spList.Disconnect();

            btnConnect.Text = "打开";
            sp.Serial.Close();
            sp.Received += Sp_Received;
        }

        private void btnRcvClear_Click(object sender, EventArgs e)
        {
            txtReceive.Clear();
            TxCnt = 0;
            RxCnt = 0;
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
            TxCnt += str.Length;
            if (count == 1)
            {
                sp.Serial.Write(str);
                return;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel3.Text = "Rx:" + RxCnt.ToString();
            toolStripStatusLabel5.Text = "Tx:" + TxCnt.ToString();
        }
    }
}

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
            var bs = new List<Int32>(new Int32[] { 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200, 128000, 194000, 256000, 512000, 1024000, 2048000 });
            cbBaundrate.DataSource = bs;
        }
    }
}

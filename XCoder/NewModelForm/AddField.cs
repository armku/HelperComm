﻿using System;
using System.Windows.Forms;
using XCode.DataAccessLayer;

namespace XCoder
{
    public partial class AddField : UserControl
    {
        public AddField()
        {
            InitializeComponent();
        }

        private IDataColumn DataColumn;
        //是否添加，默认是添加数据
        private Boolean IsNew = true;

        public AddField(IDataColumn dc, Boolean isNew)
        {
            InitializeComponent();
            IsNew = isNew;


            combRawType.DataSource = PrimitiveType.TypeList;
            //combRawType.DisplayMember = "Name";
            //combRawType.ValueMember = "DataType";

            DataColumn = dc;

            //修改的话，直接绑定数据到文本框
            if (!IsNew) BandText();
        }

        //绑定数据
        void BandText()
        {
            txtName.Text = DataColumn.Name;
            //txtDefault.Text = DataColumn.Default;
            txtDescription.Text = DataColumn.Description;
            txtLength.Text = DataColumn.Length.ToString();
            //txtNumOfByte.Text = DataColumn.NumOfByte.ToString();
            //txtPrecision.Text = DataColumn.Precision.ToString();
            if (DataColumn.DataType != null)
            {
                txtDataType.Text = DataColumn.DataType.Name;
            }
            if (DataColumn.RawType.Contains("nvarchar")) combRawType.SelectedIndex = 3;
            else combRawType.SelectedIndex = PrimitiveType.TypeList.FindIndex(n => n.Name.Contains(DataColumn.RawType));

            combRawType.Text = DataColumn.RawType;

            ckbIdentity.Checked = DataColumn.Identity;

            ckbNullable.Checked = DataColumn.Nullable;
            ckbPrimarykey.Checked = DataColumn.PrimaryKey;
        }

        //保存数据
        void SaveValue()
        {
            DataColumn.Name = txtName.Text.Trim();
            //DataColumn.Default = txtDefault.Text.Trim();
            DataColumn.Description = txtDescription.Text.Trim();
            DataColumn.Length = Convert.ToInt32(txtLength.Text.Trim());
            //DataColumn.NumOfByte = Convert.ToInt32(txtNumOfByte.Text.Trim());
            //DataColumn.Precision = Convert.ToInt32(txtPrecision.Text.Trim());
            DataColumn.DataType = Type.GetType(txtDataType.Text.Trim());

            if (combRawType.SelectedIndex != 3)
            {
                DataColumn.RawType = combRawType.Text.Trim();
            }
            else
            {
                DataColumn.RawType = string.Format(combRawType.Text.Trim() + "({0})", DataColumn.Length);
            }

            DataColumn.Identity = ckbIdentity.Checked;
            DataColumn.Nullable = ckbNullable.Checked;
            DataColumn.PrimaryKey = ckbPrimarykey.Checked;
            DataColumn.ColumnName = DataColumn.Name;
        }

        public static BaseForm CreateForm(IDataColumn column, Boolean isNew)
        {
            AddField frm = new AddField(column, isNew);
            frm.Dock = DockStyle.Fill;
            return WinFormHelper.CreateForm(frm, "编辑字段信息");
        }

        private void combRawType_SelectedIndexChanged(Object sender, EventArgs e)
        {
            if (combRawType.SelectedIndex == 3)
            {
                txtLength.Enabled = true;
                if (!IsNew) txtLength.Text = DataColumn.Length.ToString();
                else
                {
                    txtDataType.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].DataType;
                    txtLength.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].Length.ToString();
                    //txtNumOfByte.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].NumOfByte.ToString();
                    //txtPrecision.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].Precision.ToString();
                }
            }
            else
            {
                txtLength.Enabled = false;
                txtDataType.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].DataType;
                txtLength.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].Length.ToString();
                //txtNumOfByte.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].NumOfByte.ToString();
                //txtPrecision.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].Precision.ToString();
            }
        }

        private void btnSave_Click(Object sender, EventArgs e)
        {
            SaveValue();
            ParentForm.DialogResult = DialogResult.OK;
            ParentForm.Close();
        }

        private void btnCancle_Click(Object sender, EventArgs e)
        {
            ParentForm.DialogResult = DialogResult.Cancel;
            ParentForm.Close();
        }

        private void txtLength_TextChanged(Object sender, EventArgs e)
        {
            //if (combRawType.SelectedIndex == 3)
            //{
            //    txtPrecision.Text = txtLength.Text;
            //}
        }

        private void txtLength_KeyPress(Object sender, KeyPressEventArgs e)
        {
            WinFormHelper.SetControlOnlyZS(sender, e);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using bbbt.Utility;

namespace MbSecondEachPickupByLPNForm
{
    //投错格子时的信息
    public partial class ThrowErrorDia : Form
    {
        public List<string> SkuCodeAndEanList;

        //扫描枪输入字符串
        private string _inputCharBuff = "";

        //当前活动控件值备份
        private string _activeControlValue = "";

        //快捷动作列表
        private readonly Dictionary<string, Action> _lstQuickAction = new Dictionary<string, Action>();

        public ThrowErrorDia(List<string> skuCodeAndEanList, string skuCodeAndEan)
        {
            InitializeComponent();
            label1.Text = string.Format("装箱单/货物 {0} 投入了错误的格子,请重新投放", skuCodeAndEan);
            label1.Font = new System.Drawing.Font("Tahoma", 12.5F, System.Drawing.FontStyle.Bold);
            RegeditEvents();
            SkuCodeAndEanList = skuCodeAndEanList;
        }

        private void RegeditEvents()
        {
            txtSoCode.KeyPress += txtSoCode_KeyPress;
        }

        /// <summary>
        /// SO号
        /// </summary>
        void txtSoCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Tools.ScanShortcutCode(sender, e.KeyChar, ref _inputCharBuff, ref _activeControlValue, _lstQuickAction))
                return;

            if (e.KeyChar != '\r')
                return;

            if (string.IsNullOrEmpty(txtSoCode.Text.Trim()))
            {
                return;
            }
            if (!SkuCodeAndEanList.Contains(txtSoCode.Text.Trim().ToUpper()))
            {
                MsgHelper.ShowOKForWarning(string.Format("取出的货物{0}不是错投货物", txtSoCode.Text.Trim()));
                txtSoCode.Focus();
                txtSoCode.SelectAll();
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

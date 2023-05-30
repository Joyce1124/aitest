using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using bbbt.Dialog.BusinessObject.BarCodeRules;
using bbbt.Tool;
using bbbt.Utility;
using bbbt.Utility.BarDecodingRule;
using bbbt.WMS.Entities;

namespace MbSecondEachPickupByLPNForm
{
    public partial class ModifySkuQtyDialog : DevExpress.XtraEditors.XtraForm
    {
        //扫描枪输入字符串
        protected string _inputCharBuff = "";
        //当前活动控件值备份
        protected string _activeControlValue = "";
        //快捷动作列表
        protected Dictionary<string, Action> _lstQuickAction = new Dictionary<string, Action>();

        private readonly List<Control> _controlList = new List<Control>();

        private readonly double _totalPickingQty;
        private readonly MbBarCodeRules_SecondPickup _curBarRules;
        private readonly List<MbNewLpndSalesOrderInfo> _curSameCellWorkInfoList;
        private readonly Dictionary<long, double> _dicLpndAndDeductionQty = new Dictionary<long, double>();

        public Dictionary<long, double> DicLpndAndDeductionQty
        {
            get { return _dicLpndAndDeductionQty; }
        }

        public ModifySkuQtyDialog(List<MbNewLpndSalesOrderInfo> curSameCellWorkInfoList, double totalPickingQty, MbBarCodeRules_SecondPickup curBarRules)
        {
            InitializeComponent();
            _curBarRules = curBarRules;
            _curSameCellWorkInfoList = curSameCellWorkInfoList;
            _totalPickingQty = totalPickingQty;
            InitUiAndRegister();
        }

        private void InitUiAndRegister()
        {
            StartPosition = FormStartPosition.CenterScreen;
            _controlList.Add(txtSkuCode);
            _lstQuickAction.Add("F3", BtnConfirmClick);
            labTotalSkuQty.Text = string.Format("需投入{0} 件，", _totalPickingQty);

            btnConfirm.Click += btnConfirm_Click;
            txtSkuCode.KeyPress += txtSkuCode_KeyPress;
        }

        void txtSkuCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Tools.ScanShortcutCode(sender, e.KeyChar, ref _inputCharBuff, ref _activeControlValue, _lstQuickAction))
                return;
            if(e.KeyChar != '\r')
                return;
            ToolTipHelper.RestoreControl(_controlList);
            if (string.IsNullOrEmpty(txtSkuCode.Text))
            {
                ToolTipHelper.SetToolTip(txtSkuCode, "提示", "货物条码不能为空!");
                MediaHelper.PlaySoundErrorByBarCode();
                txtSkuCode.SelectAll();
                return;
            }
            if(Convert.ToInt32(labLackSkuQty.Text) >= _totalPickingQty)
            {
                ToolTipHelper.SetToolTip(txtSkuCode, "提示", "扣减数量已超出！");
                MediaHelper.PlaySoundErrorByBarCode();
                txtSkuCode.SelectAll();
                return;
            }
            if (!DeductionSku())
                return;
            AfterDeductionSuccess();
        }

        /// <summary>
        /// 提交
        /// </summary>
        private void BtnConfirmClick()
        {
            btnConfirm_Click(null, null);
        }

        void btnConfirm_Click(object sender, EventArgs e)
        {
            if(_dicLpndAndDeductionQty.Count == 0)
            {
                ToolTipHelper.SetToolTip(txtSkuCode, "提示", "请先扫描需要扣减的货物！");
                return;
            }
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// 扣减成功后
        /// </summary>
        private void AfterDeductionSuccess()
        {
            labLackSkuQty.Text = (Convert.ToInt32(labLackSkuQty.Text) + 1).ToString(CultureInfo.InvariantCulture);
            txtSkuCode.SelectAll();
        }

        /// <summary>
        /// 扣减数量
        /// </summary>
        private bool DeductionSku()
        {
            var skuCodeOrEanList = IsKboxingCustomer(_curSameCellWorkInfoList[0].manufacturerCode) ? _curBarRules.ParseSkuCodeOrEanListForJB(KBoxingRule.GetSkuListByRule(txtSkuCode.Text.Trim()))
                                                              : _curBarRules.ParseSkuCodeOrEanList(txtSkuCode.Text.Trim());
            if (skuCodeOrEanList.Count == 0)
            {
                MediaHelper.PlaySoundSysError();
                ToolTipHelper.SetToolTip(txtSkuCode, "提示", string.Format("货物条码不能解析，请检查"));
                return false;
            }
            bool isFindCurSku = false;
            foreach (var skuCodeOrEan in skuCodeOrEanList)
            {
                isFindCurSku = _curSameCellWorkInfoList.Any(workInfo => (IsEqual(workInfo.skuCode, skuCodeOrEan) || IsEqual(workInfo.skuEanCode, skuCodeOrEan)
                          || IsEqual(workInfo.skuEanCode2, skuCodeOrEan) || IsEqual(workInfo.skuEanCode3, skuCodeOrEan) || IsEqual(workInfo.skuEanCode4, skuCodeOrEan)
                          || IsEqual(workInfo.skuEanCode5, skuCodeOrEan) || IsEqual(workInfo.skuEanCode6, skuCodeOrEan)) && workInfo.id != 0);
                if (isFindCurSku)
                    break;
            }
            if (!isFindCurSku)
            {
                MediaHelper.PlaySoundSysError();
                ToolTipHelper.SetToolTip(txtSkuCode, "提示", string.Format("该货物不在扣减任务中"));
                return false;
            }
            foreach (var lpnd in _curSameCellWorkInfoList)
            {
                if (lpnd.qtyOutputUom <= 0) 
                    continue;
                lpnd.qtyOutputUom--;
                if (_dicLpndAndDeductionQty.ContainsKey(lpnd.id))
                {
                    _dicLpndAndDeductionQty[lpnd.id]++;
                }
                else
                {
                    _dicLpndAndDeductionQty.Add(lpnd.id, 1);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 判断客户是否是劲霸
        /// </summary>
        private bool IsKboxingCustomer(string manufactureCode)
        {
            if (KBoxingRule.IsKboxingManufactureCode(manufactureCode))
            {
                return true;
            }
            return false;
        }

        private bool IsEqual(string skuCode, string skuCodeOrEan)
        {
            if (string.IsNullOrEmpty(skuCode))
            {
                return false;
            }
            return skuCode.ToUpper() == skuCodeOrEan.ToUpper();
        }
    }
}

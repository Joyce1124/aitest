using System;
using System.Collections.Generic;
using System.Windows.Forms;
using bbbt.Tool;
using bbbt.Utility;
using bbbt.WMS.IService;
using bbbt.WMS.ServiceFactory;
using DevExpress.XtraEditors;

namespace MbSecondEachPickupByLPNForm
{
    public partial class ScanPickupCarDia : XtraForm
    {
        private readonly IMbNewWorkOrderBusinessServicecs _newWorkOrderBusinessService = ServiceFactory.GetMbNewWorkOrderBusinessService();
        private readonly List<Control> _controlList = new List<Control>();
        private readonly string _lpnCode;
        private readonly string _waveCode;
        private readonly bool _isScanSo;  //光栅扫描
        private readonly long _pkhId;

        public string LPNCode { get { return txtLPNCode.Text.Trim(); } }

        public ScanPickupCarDia(long pkhId, string lpnCode, string waveCode, bool isScanSo)
        {
            InitializeComponent();
            KeyPreview = true;
            _lpnCode = lpnCode;
            _waveCode = waveCode;
            _isScanSo = isScanSo;
            _pkhId = pkhId;
            txtLPNCode.KeyPress += txtLPNCode_KeyPress;
            txtLPNCode.Click += txtLPNCode_Click;
            btnConfirm.Click += btnConfirm_Click;
            _controlList.Add(txtLPNCode);
            StartPosition = FormStartPosition.CenterScreen;
        }

        /// <summary>
        /// 确认
        /// </summary>>
        void btnConfirm_Click(object sender, EventArgs e)
        {
            ToolTipHelper.RestoreControl(_controlList);
            if (string.IsNullOrEmpty(txtLPNCode.Text.Trim()))
            {
                ToolTipHelper.SetToolTip(txtLPNCode, "提示", "LPN不能为空!");
                MediaHelper.PlaySoundErrorByBarCode();
                txtLPNCode.SelectAll();
                return;
            }
            var br = _newWorkOrderBusinessService.RebinPrepareWhenRebin(_lpnCode, _waveCode, txtLPNCode.Text.Trim(), ClientService.CurrentUserInfo.code,
                HashTableHelper.CacheInstance.GetCodeInfoIdByCodeClassCodeAndCodeInfoCode(CodeConcentrationCamp.DIST_OPER_TYPE, _isScanSo ? CodeConcentrationCamp.LASER_DIST : CodeConcentrationCamp.COMMON_DIST));
            if (!br.Success)
            {
                ToolTipHelper.SetToolTip(txtLPNCode, "提示", br.hasMessage() ? br.MessageList[0].messageCN : "上墙领用失败!");
                MediaHelper.PlaySoundErrorByBarCode();
                txtLPNCode.SelectAll();
                return ;
            }

            BehaviorReturn lpnBr = _newWorkOrderBusinessService.CreateOnWallRelationOutbound(_pkhId, null, new string[] { txtLPNCode.Text.Trim() });
            if (!lpnBr.Success)
            {
                ToolTipHelper.SetToolTip(txtLPNCode, "提示", lpnBr.hasMessage() ? lpnBr.MessageList[0].messageCN : "保存LPN号失败");
                MediaHelper.PlaySoundErrorByBarCode();
                return;
            }
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// LPN号
        /// </summary>
        void txtLPNCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\r')
                return;
            btnConfirm_Click(null, null);
        }

        void txtLPNCode_Click(object sender, EventArgs e)
        {
            txtLPNCode.SelectAll();
        }
    }
}

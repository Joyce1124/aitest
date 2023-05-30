using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using bbbt.Tool;
using bbbt.Utility;
using bbbt.WMS.IService;
using bbbt.WMS.ServiceFactory;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace MbSecondEachPickupByLPNForm
{
    public partial class ChangeWallDia : XtraForm
    {
        private readonly IMbNewWorkOrderBusinessServicecs _service = ServiceFactory.GetMbNewWorkOrderBusinessService();
        private readonly List<Control> _controlList = new List<Control>();
        private long PkhId;

        private string _curWall = "";
        public string CurWall
        {
            get { return _curWall; }
        }

        public ChangeWallDia(ArrayList locationList,string pkhCode,long pkhId)
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            labelControl1.Text = string.Format("波次分组号:{0}", pkhCode);
            PkhId = pkhId;
            btnSure.Click += btnSure_Click;
            SetLookUpWalls(locationList);
            _controlList.Add(lookUpWalls);
        }

        private void SetLookUpWalls(object source)
        {
            lookUpWalls.Properties.DataSource = source;
            lookUpWalls.Properties.Columns.Clear();
            var coll = lookUpWalls.Properties.Columns;
            coll.Add(new LookUpColumnInfo("locationCode", "代码", 0));
            lookUpWalls.Properties.DisplayMember = "locationCode";
            lookUpWalls.Properties.ValueMember = "id";
            lookUpWalls.Properties.NullText = "";
            lookUpWalls.Properties.bbbtFit();
            lookUpWalls.Properties.Buttons[0].Visible = true;
            lookUpWalls.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            lookUpWalls.Properties.ImmediatePopup = true;
            lookUpWalls.Properties.SearchMode = SearchMode.AutoFilter;
        }

        private void btnSure_Click(object sender, EventArgs e)
        {
            ToolTipHelper.RestoreControl(_controlList);
            if (string.IsNullOrEmpty(lookUpWalls.Text))
            {
                ToolTipHelper.SetToolTip(lookUpWalls, "提示", "集货墙不能为空");
                MediaHelper.PlaySoundErrorByBarCode();
                return;
            }
            var br = _service.ChangeWall4Pkh(PkhId, (long)lookUpWalls.EditValue);
            if (!br.Success)
            {
                ToolTipHelper.SetToolTip(lookUpWalls, "提示", br.hasMessage()?br.MessageList[0].messageCN:"换墙失败");
                MediaHelper.PlaySoundErrorByBarCode();
                return;
            }
            _curWall = lookUpWalls.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using bbbt.Utility;
using bbbt.WMS.Entities;
using DevExpress.XtraEditors;

namespace MbSecondEachPickupByLPNForm
{
    public partial class SetupScanRate : XtraForm
    {
        private readonly IniFileHelper _iniFile = new IniFileHelper(GatedLaunchHelper.GetNormalDir(Application.StartupPath) + "\\Config\\SystemConfig.ini");
        public SetupScanRate()
        {
            InitializeComponent();
            IntiResgite();

            var setup = GetSetupFromConfig();
            if (setup.ScanType == (int)ScanTypeEnum.ScanSO)//扫描方式为光栅扫描的时候，只能逐渐扫描
            {
                _iniFile.IniWritevalue("MbSecondEachPickupByLPNForm" + ClientService.CurrentWareId, "PickingType", "PiecePicking");
                chkPiecePicking.Checked = true;
                chkBatchAllPicking.Checked = false;
                chkBatchBySoPicking.Checked = false;

                chkPiecePicking.Enabled = false;
                chkBatchAllPicking.Enabled = false;
                chkBatchBySoPicking.Enabled = false;
                return;
            }
                
            var pickingTypeValue = _iniFile.IniReadvalue("MbSecondEachPickupByLPNForm" + ClientService.CurrentWareId, "PickingType");
            SetCheckBoxStatus(pickingTypeValue);
        }

        private void IntiResgite()
        {
            btnCancel.Click += btnCancel_Click;
            btnSure.Click += btnSure_Click;
        }

        void btnSure_Click(object sender, System.EventArgs e)
        {
            string pickingType = null;
            if (chkPiecePicking.Checked)
            {
                pickingType = "PiecePicking";
            }else if (chkBatchBySoPicking.Checked)
            {
                pickingType = "BatchBySoPicking";
            }else if (chkBatchAllPicking.Checked)
            {
                pickingType = "BatchAllPicking";
            }
            _iniFile.IniWritevalue("MbSecondEachPickupByLPNForm" + ClientService.CurrentWareId, "PickingType", pickingType);
            DialogResult=DialogResult.OK;
        }

        void btnCancel_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        void SetCheckBoxStatus(string pickingType)
        {
            if (string.IsNullOrEmpty(pickingType) || StringHelper.EqualIgnoreCase(pickingType,"PiecePicking"))
            {
                chkPiecePicking.Checked = true;
                chkBatchAllPicking.Checked = false;
                chkBatchBySoPicking.Checked = false;
            }
            else if (StringHelper.EqualIgnoreCase(pickingType, "BatchBySoPicking"))
            {
                chkPiecePicking.Checked = false;
                chkBatchAllPicking.Checked = false;
                chkBatchBySoPicking.Checked = true;
            }
            else if (StringHelper.EqualIgnoreCase(pickingType, "BatchAllPicking"))
            {
                chkPiecePicking.Checked = false;
                chkBatchAllPicking.Checked = true;
                chkBatchBySoPicking.Checked = false;
            }
        }

        /// <summary>
        /// 读取设置配置文件
        /// </summary>
        private SetupInfo GetSetupFromConfig()
        {
            if (!File.Exists(SetUpConfig1.ConfigPath))
            {
                return GetDefaultSetupConfig();
            }

            var ds = new DataSet();
            ds.ReadXml(SetUpConfig1.ConfigPath);
            var dt = ds.Tables[SetUpConfig1.ConfigTableName];

            var setupList = EnumDataSourceConvert.SetDataTableToList<SetupInfo>(dt);
            if (setupList.Count == 0)
                return new SetupInfo()
                {
                    ScanType = (int)ScanTypeEnum.NoScanSo, //不需要扫描so
                };
            return setupList[0];
        }

        /// <summary>
        /// 默认设置
        /// </summary>
        private SetupInfo GetDefaultSetupConfig()
        {
            return new SetupInfo
            {
                ScanType = (int)ScanTypeEnum.NoScanSo, //不需要扫描so
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using bbbt.Tool;
using bbbt.Utility;
using bbbt.WMS.Entities;
using bbbt.WMS.IService;
using bbbt.WMS.ServiceFactory;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace MbSecondEachPickupByLPNForm
{
    public partial class ScanWallDia : XtraForm
    {
        private string _curWall = "";
        public string CurWall
        {
            get { return _curWall; }
        }
        private readonly IMbLocationService _locationService = ServiceFactory.GetMbLocationService(); 
        private readonly List<string> _wallsList = new List<string>();
        private List<SetUpInfo>  _configList = new List<SetUpInfo>();
        private readonly List<Control> _controlList = new List<Control>();

        public ScanWallDia()
        {
            InitializeComponent();
            _configList = GetSetupFromConfig();
            GetWallLocation();
            SetLookUpWalls();
            _controlList.Add(lookUpWalls);
        }

        private void GetWallLocation()
        {
            var br = _locationService.SearchWallLocation();
            if (!br.Success || !br.hasEntity())
            {
                _configList=new List<SetUpInfo>();
                return;
            }
            var locWallList = new List<string>();
            var allLoc = (from MbLocation location in br.ObjectList select location.locationCode).ToList();
            allLoc.Sort();
            if (_configList != null)
            {
                var newConfigs = new List<SetUpInfo>();
                foreach (var config in _configList)
                {
                    if (allLoc.Contains(config.Wall))
                    {
                        newConfigs.Add(config);
                    }
                }
                _configList = newConfigs;
                foreach (var setUpInfo in _configList)
                {
                    locWallList.Add(setUpInfo.Wall);
                    var newWallName = setUpInfo.Wall + "(最近使用)";
                    _wallsList.Add(newWallName);
                }
            }
            foreach (string locationCode in allLoc)
            {
                if (!locWallList.Contains(locationCode))
                    _wallsList.Add(locationCode);
            }
        }

        private void SetLookUpWalls()
        {
            var wallCodeList = _wallsList.Select(wall => new MbCodeInfo {code = wall}).ToList();
            lookUpWalls.Properties.DataSource = wallCodeList;
            lookUpWalls.Properties.Columns.Clear();
            var coll = lookUpWalls.Properties.Columns;
            coll.Add(new LookUpColumnInfo("code", "代码", 0));
            lookUpWalls.Properties.DisplayMember = "code";
            lookUpWalls.Properties.ValueMember = "code";
            lookUpWalls.Properties.NullText = @"请选择";
            lookUpWalls.Properties.bbbtFit();
            lookUpWalls.Properties.Buttons[0].Visible = true;
            lookUpWalls.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            lookUpWalls.Properties.ImmediatePopup = true;
            lookUpWalls.Properties.SearchMode = SearchMode.AutoFilter;
        }

        private void SaveConfig(string curWall)
        {
            if (!Directory.Exists(SetUpConfig.ConfigDirectory))
                Directory.CreateDirectory(SetUpConfig.ConfigDirectory);
            try
            {
                var setUpInfo = new List<SetUpInfo> { new SetUpInfo { Wall = curWall } };
                if (_configList != null)
                {
                    foreach (var config in _configList)
                    {
                        if (!config.Wall.Contains(curWall) && setUpInfo.Count < 3)
                        {
                            setUpInfo.Add(config);
                        }
                    }
                }
                var ds = new DataSet();
                var dataTable = Tools.GetObjectDataTable<SetUpInfo>(setUpInfo);
                dataTable.TableName = SetUpConfig.ConfigTableName;
                ds.Tables.Add(dataTable);
                ds.WriteXml(SetUpConfig.ConfigPath, XmlWriteMode.IgnoreSchema);
            }
            catch (Exception e)
            {
                MsgHelper.ShowOkForError(e.Message);
            }
        }

        /// <summary>
        /// 读取设置配置文件
        /// </summary>
        private List<SetUpInfo> GetSetupFromConfig()
        {
            if (!File.Exists(SetUpConfig.ConfigPath))
            {
                return null;
            }

            var ds = new DataSet();
            ds.ReadXml(SetUpConfig.ConfigPath);
            var dt = ds.Tables[SetUpConfig.ConfigTableName];

            var walls = EnumDataSourceConvert.SetDataTableToList<SetUpInfo>(dt);
            if (walls.Count == 0)
                return null;

            return walls;
        }

        private void btnSure_Click(object sender, EventArgs e)
        {
            ToolTipHelper.RestoreControl(_controlList);
            if (string.IsNullOrEmpty(@"请选择") || lookUpWalls.Text == @"请选择")
            {
                ToolTipHelper.SetToolTip(lookUpWalls, "提示", "集货墙不能为空");
                MediaHelper.PlaySoundErrorByBarCode();
                return;
            }
            var curWall = lookUpWalls.Text.Replace("(最近使用)", "");
            SaveConfig(curWall);
            _curWall = curWall;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal class SetUpConfig
    {
        internal static string ConfigDirectory = GatedLaunchHelper.GetNormalDir(Application.StartupPath) + @"\Config\MbSecondEachPickupByLPNForm\";
        internal static string ConfigPath = ConfigDirectory + "Config.xml";
        internal static string ConfigTableName = "ConfigTable";
    }

    internal class SetUpInfo
    {
        public string Wall { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using bbbt.Utility;

namespace MbSecondEachPickupByLPNForm
{
    public partial class SetupDialog : Form
    {
        public SetupDialog(SetupInfo defaultSetupInfo, bool isReadOnly)
        {
            InitializeComponent();
            RegeditEvents();
            InitData(defaultSetupInfo, isReadOnly);
        }

        private void RegeditEvents()
        {
            btnSave.Click += btnSave_Click; //保存
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitData(SetupInfo defaultSetupInfo, bool isReadOnly)
        {
            Selector_ScanType.Properties.ReadOnly = isReadOnly;
            if (!File.Exists(SetUpConfig1.ConfigPath))
            {
                bsSetupInfo.DataSource = new List<SetupInfo>
                                             {
                                                defaultSetupInfo
                                             };
                return;
            }

            var ds = new DataSet();
            ds.ReadXml(SetUpConfig1.ConfigPath);
            var dt = ds.Tables[SetUpConfig1.ConfigTableName];

            var setupList = EnumDataSourceConvert.SetDataTableToList<SetupInfo>(dt);
            if (setupList.Count == 0) return;

            bsSetupInfo.DataSource = setupList;
        }

        /// <summary>
        /// 保存
        /// </summary>
        void btnSave_Click(object sender, EventArgs e)
        {
            if (!IsCanSave())
                return;

            Save2ConfigXml();
        }


        /// <summary>
        /// 保存前判断
        /// </summary>
        private bool IsCanSave()
        {
            return true;
        }
        /// <summary>
        /// 保存到本地配置文件中
        /// </summary>
        private void Save2ConfigXml()
        {
            var current = bsSetupInfo.DataSource as List<SetupInfo>;
            if (current == null)
            {
                MsgHelper.ShowOkForError("保存失败！");
                return;
            }

            if (!Directory.Exists(SetUpConfig1.ConfigDirectory))
                Directory.CreateDirectory(SetUpConfig1.ConfigDirectory);

            try
            {
                var ds = new DataSet();
                var dataTable = Tools.GetObjectDataTable<SetupInfo>(current);
                dataTable.TableName = SetUpConfig1.ConfigTableName;
                ds.Tables.Add(dataTable);
                ds.WriteXml(SetUpConfig1.ConfigPath, XmlWriteMode.IgnoreSchema);

                Close();
            }
            catch (Exception e)
            {
                MsgHelper.ShowOkForError(e.Message);
            }
        }
    }
}

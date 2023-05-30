using System.ComponentModel;
using System.Windows.Forms;
using bbbt.WMS.Entities;

namespace MbSecondEachPickupByLPNForm
{
    internal class SetUpConfig1
    {
        internal static string ConfigDirectory = GatedLaunchHelper.GetNormalDir(Application.StartupPath) + @"\Config\MbSecondEachPickupByLPNForm\SetUp\";
        internal static string ConfigPath = ConfigDirectory + "SetupConfig.xml";
        internal static string ConfigTableName = "SetupConfigTable";
    }

    /// <summary>
    /// 扫描方式
    /// </summary>
    internal enum ScanTypeEnum
    {
        [Description("普通分拣")]
        NoScanSo = 0,

        [Description("光栅分拣")]
        ScanSO = 1,
    }

    /// <summary>
    /// 设置类
    /// </summary>
    public class SetupInfo
    {
        /// <summary>
        /// 扫描方式
        /// </summary>
        public int ScanType { get; set; }
    }
}

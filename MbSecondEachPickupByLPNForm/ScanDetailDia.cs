using System;
using System.Collections.Generic;
using System.Windows.Forms;
using bbbt.ControlLibrary.Winform;

namespace MbSecondEachPickupByLPNForm
{
    public partial class ScanDetailDia : BaseDialog
    {
        public ScanDetailDia(List<SkuScanDetail> skuScanDetails)
        {
            InitializeComponent();
            InitUi();
            LoadDatas(skuScanDetails);
            btnReturn.Click += btnReturn_Click; //返回
        }

        private void InitUi()
        {
            gvScanDetail.OptionsBehavior.Editable = true;
            gvScanDetail.OptionsSelection.EnableAppearanceFocusedCell = false;
            gvScanDetail.OptionsSelection.EnableAppearanceFocusedRow = true;
            gvScanDetail.OptionsSelection.EnableAppearanceHideSelection = true;
            barTools.Visible = false;
            StartPosition = FormStartPosition.CenterScreen;
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        private void LoadDatas(List<SkuScanDetail> skuScanDetails)
        {
            bsSkuScanDetail.DataSource = skuScanDetails;
            bsSkuScanDetail.ResetBindings(false);
            gvScanDetail.bbbtFitColumns();
        }

        /// <summary>
        /// 返回
        /// </summary>
        void btnReturn_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

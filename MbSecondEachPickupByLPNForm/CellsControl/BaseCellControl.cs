using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace MbSecondEachPickupByLPNForm
{
    public delegate void ShowDetailCell(Label label);
    public delegate void ShowSoListCell();

    public partial class BaseCellControl : XtraUserControl
    {
        public event ShowDetailCell ShowDatailCellEvent;
        public event ShowSoListCell ShowSoListCellEvent;

        public BaseCellControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化格子颜色
        /// </summary>
        public virtual void InitCellColor(List<MbCellInfo> cellInfoLis, List<long> outCancleCellList)
        {

        }

        /// <summary>
        /// 改变格子颜色
        /// </summary>
        public virtual void ChangeCellColor(string cellNo, Color color)
        {

        }

        public void labCell_Click(object sender, EventArgs e)
        {
            if (ShowDatailCellEvent != null)
            {
                var label = sender as Label;
                if (label == null)
                    return;
                ShowDatailCellEvent(label); //显示格子信息
            }
        }

        /// <summary>
        /// 边框加颜色
        /// </summary>
        public virtual void ChangeBorderColor(string cellNo)
        {

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (ShowSoListCellEvent != null)
            {
                ShowSoListCellEvent(); //显示格子装箱单投递状态
            }
        }

        /// <summary>
        /// 在格子上进行绘画
        /// </summary>
        protected void CreateCellFillRectangle(Graphics e)
        {
            // Create blueBrush.
            var blueBrush = new SolidBrush(Color.Black);

            // Create rectangle.
            var rect = new Rectangle(0, 0, 8, 14);

            // Fill rectangle to screen.
            e.FillRectangle(blueBrush, rect);
        }
    }
}

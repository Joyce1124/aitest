using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MbSecondEachPickupByLPNForm
{
    public partial class TwoHundredCellsControl : BaseCellControl
    {
        public TwoHundredCellsControl()
        {
            InitializeComponent();
            foreach (var coltrol in Controls)
            {
                var label = coltrol as Label;
                if (label != null && !label.Text.Contains("格子号"))
                {
                    label.Visible = false;
                    label.Click += labCell_Click;
                }
            }
        }

        /// <summary>
        /// 初始化格子颜色
        /// </summary>
        public override void InitCellColor(List<MbCellInfo> cellInfoLis, List<long> outCancleCellList)
        {
            MakeLabelHide();
            long preCellIndex = 0; //前一个格子号
            foreach (var cellInfo in cellInfoLis)
            {
                HandEmptyCell(preCellIndex, cellInfo.CellIndex, outCancleCellList);
                var labCell = (Controls.Find("labCell" + cellInfo.CellIndex, false)[0]) as Label;
                if (labCell != null)
                {
                    preCellIndex = cellInfo.CellIndex;
                    labCell.Text = string.Format("格{0}：{1}件", cellInfo.CellIndex, cellInfo.QtyAllocatedUom);
                    labCell.BackColor = GetColor(cellInfo.IsOutCancle, cellInfo.QtyAllocatedUom, cellInfo.QtyOutputboxUom);
                    labCell.Visible = true;
                }
            }
        }

        /// <summary>
        /// 处理空格子号
        /// </summary>
        private void HandEmptyCell(long preCellIndex, long curCellIndex, List<long> outCancleCellList)
        {
            if (curCellIndex - preCellIndex == 1) //说明格子号连续           
                return;
            for (var i = (int)preCellIndex + 1; i < curCellIndex; i++)
            {
                var labCell = (Controls.Find("labCell" + i, false)[0]) as Label;
                if (labCell != null)
                {
                    labCell.Text = string.Format("格{0}：完成", i);
                    labCell.BackColor = outCancleCellList.Contains(i) ? Color.Orange : Color.Green;
                    labCell.Visible = true;
                }
            }
        }

        private void MakeLabelHide()
        {
            foreach (var coltrol in Controls)
            {
                var label = coltrol as Label;
                if (label != null && !label.Text.Contains("格子号"))
                {
                    label.Visible = false;
                    label.BackColor = Color.White;
                }
            }
        }

        /// <summary>
        /// 改变格子颜色
        /// </summary>
        public override void ChangeCellColor(string cellNo, Color color)
        {
            var labCell = (Controls.Find("labCell" + cellNo, false)[0]) as Label;
            if (labCell != null && labCell.BackColor != Color.Orange)
            {
                labCell.BackColor = color;
                labCell.Refresh();
            }
        }


        private Color GetColor(bool isOutCancle, double qtyAllocatedUom, double qtyOutputboxUom)
        {
            if (isOutCancle)
            {
                return Color.Orange;
            }

            if (qtyOutputboxUom == 0)
            {
                return Color.White;
            }

            if (qtyOutputboxUom < qtyAllocatedUom)
            {
                return Color.Red;
            }

            return Color.Green;
        }

        /// <summary>
        /// 边框加颜色
        /// </summary>
        public override void ChangeBorderColor(string cellNo)
        {
            var labCell = (Controls.Find("labCell" + cellNo, false)[0]) as Label;
            if (labCell != null)
            {
                CreateCellFillRectangle(labCell.CreateGraphics());
            }
        }
    }
}

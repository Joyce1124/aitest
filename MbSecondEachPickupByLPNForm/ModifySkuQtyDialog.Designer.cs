namespace MbSecondEachPickupByLPNForm
{
    partial class ModifySkuQtyDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.labLackSkuQty = new DevExpress.XtraEditors.LabelControl();
            this.labTotalSkuQty = new DevExpress.XtraEditors.LabelControl();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl5 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl6 = new DevExpress.XtraEditors.LabelControl();
            this.txtSkuCode = new DevExpress.XtraEditors.TextEdit();
            this.btnConfirm = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.txtSkuCode.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl1.Location = new System.Drawing.Point(142, 15);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(16, 19);
            this.labelControl1.TabIndex = 0;
            this.labelControl1.Text = "缺";
            // 
            // labLackSkuQty
            // 
            this.labLackSkuQty.Appearance.Font = new System.Drawing.Font("Tahoma", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labLackSkuQty.Appearance.ForeColor = System.Drawing.Color.Red;
            this.labLackSkuQty.Location = new System.Drawing.Point(170, 11);
            this.labLackSkuQty.Name = "labLackSkuQty";
            this.labLackSkuQty.Size = new System.Drawing.Size(11, 25);
            this.labLackSkuQty.TabIndex = 1;
            this.labLackSkuQty.Text = "0";
            // 
            // labTotalSkuQty
            // 
            this.labTotalSkuQty.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labTotalSkuQty.Location = new System.Drawing.Point(31, 15);
            this.labTotalSkuQty.Name = "labTotalSkuQty";
            this.labTotalSkuQty.Size = new System.Drawing.Size(103, 19);
            this.labTotalSkuQty.TabIndex = 2;
            this.labTotalSkuQty.Text = "需投入15 件，";
            // 
            // labelControl4
            // 
            this.labelControl4.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl4.Location = new System.Drawing.Point(197, 15);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(16, 19);
            this.labelControl4.TabIndex = 3;
            this.labelControl4.Text = "件";
            // 
            // labelControl5
            // 
            this.labelControl5.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl5.Location = new System.Drawing.Point(31, 63);
            this.labelControl5.Name = "labelControl5";
            this.labelControl5.Size = new System.Drawing.Size(96, 19);
            this.labelControl5.TabIndex = 4;
            this.labelControl5.Text = "扫描货物条码";
            // 
            // labelControl6
            // 
            this.labelControl6.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl6.Location = new System.Drawing.Point(362, 62);
            this.labelControl6.Name = "labelControl6";
            this.labelControl6.Size = new System.Drawing.Size(96, 19);
            this.labelControl6.TabIndex = 5;
            this.labelControl6.Text = "更改缺少数量";
            // 
            // txtSkuCode
            // 
            this.txtSkuCode.Location = new System.Drawing.Point(133, 60);
            this.txtSkuCode.Name = "txtSkuCode";
            this.txtSkuCode.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSkuCode.Properties.Appearance.Options.UseFont = true;
            this.txtSkuCode.Size = new System.Drawing.Size(223, 26);
            this.txtSkuCode.TabIndex = 6;
            // 
            // btnConfirm
            // 
            this.btnConfirm.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnConfirm.Appearance.Options.UseFont = true;
            this.btnConfirm.Location = new System.Drawing.Point(362, 104);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(96, 28);
            this.btnConfirm.TabIndex = 13;
            this.btnConfirm.Text = "提交";
            // 
            // ModifySkuQtyDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(489, 157);
            this.Controls.Add(this.btnConfirm);
            this.Controls.Add(this.txtSkuCode);
            this.Controls.Add(this.labelControl6);
            this.Controls.Add(this.labelControl5);
            this.Controls.Add(this.labelControl4);
            this.Controls.Add(this.labTotalSkuQty);
            this.Controls.Add(this.labLackSkuQty);
            this.Controls.Add(this.labelControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ModifySkuQtyDialog";
            this.Text = "修改投货数量";
            ((System.ComponentModel.ISupportInitialize)(this.txtSkuCode.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labLackSkuQty;
        private DevExpress.XtraEditors.LabelControl labTotalSkuQty;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraEditors.TextEdit txtSkuCode;
        private DevExpress.XtraEditors.SimpleButton btnConfirm;
    }
}
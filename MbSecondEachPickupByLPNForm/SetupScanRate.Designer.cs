namespace MbSecondEachPickupByLPNForm
{
    partial class SetupScanRate
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
            this.label1 = new System.Windows.Forms.Label();
            this.chkPiecePicking = new System.Windows.Forms.RadioButton();
            this.chkBatchBySoPicking = new System.Windows.Forms.RadioButton();
            this.chkBatchAllPicking = new System.Windows.Forms.RadioButton();
            this.btnSure = new DevExpress.XtraEditors.SimpleButton();
            this.btnCancel = new DevExpress.XtraEditors.SimpleButton();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 14);
            this.label1.TabIndex = 0;
            this.label1.Text = "SKU扫描频率：";
            // 
            // chkPiecePicking
            // 
            this.chkPiecePicking.AutoSize = true;
            this.chkPiecePicking.Location = new System.Drawing.Point(148, 33);
            this.chkPiecePicking.Name = "chkPiecePicking";
            this.chkPiecePicking.Size = new System.Drawing.Size(73, 18);
            this.chkPiecePicking.TabIndex = 1;
            this.chkPiecePicking.Text = "逐件扫描";
            this.chkPiecePicking.UseVisualStyleBackColor = true;
            // 
            // chkBatchBySoPicking
            // 
            this.chkBatchBySoPicking.AutoSize = true;
            this.chkBatchBySoPicking.Location = new System.Drawing.Point(254, 26);
            this.chkBatchBySoPicking.Name = "chkBatchBySoPicking";
            this.chkBatchBySoPicking.Size = new System.Drawing.Size(111, 32);
            this.chkBatchBySoPicking.TabIndex = 2;
            this.chkBatchBySoPicking.Text = "每个格子每种SKU\r\n扫描一次";
            this.chkBatchBySoPicking.UseVisualStyleBackColor = true;
            // 
            // chkBatchAllPicking
            // 
            this.chkBatchAllPicking.AutoSize = true;
            this.chkBatchAllPicking.Location = new System.Drawing.Point(406, 33);
            this.chkBatchAllPicking.Name = "chkBatchAllPicking";
            this.chkBatchAllPicking.Size = new System.Drawing.Size(119, 18);
            this.chkBatchAllPicking.TabIndex = 3;
            this.chkBatchAllPicking.Text = "每种SKU扫描一次";
            this.chkBatchAllPicking.UseVisualStyleBackColor = true;
            // 
            // btnSure
            // 
            this.btnSure.Location = new System.Drawing.Point(358, 140);
            this.btnSure.Name = "btnSure";
            this.btnSure.Size = new System.Drawing.Size(87, 31);
            this.btnSure.TabIndex = 4;
            this.btnSure.Text = "确认";
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(463, 140);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(87, 31);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "取消";
            // 
            // SetupScanRate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(568, 185);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSure);
            this.Controls.Add(this.chkBatchAllPicking);
            this.Controls.Add(this.chkBatchBySoPicking);
            this.Controls.Add(this.chkPiecePicking);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupScanRate";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "设置扫描频率";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton chkPiecePicking;
        private System.Windows.Forms.RadioButton chkBatchBySoPicking;
        private System.Windows.Forms.RadioButton chkBatchAllPicking;
        private DevExpress.XtraEditors.SimpleButton btnSure;
        private DevExpress.XtraEditors.SimpleButton btnCancel;
    }
}
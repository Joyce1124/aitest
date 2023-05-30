namespace MbSecondEachPickupByLPNForm
{
    partial class ScanWallDia
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
            this.lookUpWalls = new DevExpress.XtraEditors.LookUpEdit();
            this.btnSure = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.lookUpWalls.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl1.Location = new System.Drawing.Point(12, 34);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(54, 19);
            this.labelControl1.TabIndex = 1;
            this.labelControl1.Text = "集货墙:";
            // 
            // lookUpWalls
            // 
            this.lookUpWalls.Location = new System.Drawing.Point(82, 31);
            this.lookUpWalls.Name = "lookUpWalls";
            this.lookUpWalls.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lookUpWalls.Properties.Appearance.Options.UseFont = true;
            this.lookUpWalls.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.lookUpWalls.Properties.ShowHeader = false;
            this.lookUpWalls.Size = new System.Drawing.Size(274, 26);
            this.lookUpWalls.TabIndex = 2;
            // 
            // btnSure
            // 
            this.btnSure.Location = new System.Drawing.Point(269, 117);
            this.btnSure.Name = "btnSure";
            this.btnSure.Size = new System.Drawing.Size(87, 27);
            this.btnSure.TabIndex = 3;
            this.btnSure.Text = "确定";
            this.btnSure.Click += new System.EventHandler(this.btnSure_Click);
            // 
            // ScanWallDia
            // 
            this.AcceptButton = this.btnSure;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(379, 162);
            this.Controls.Add(this.btnSure);
            this.Controls.Add(this.lookUpWalls);
            this.Controls.Add(this.labelControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ScanWallDia";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            ((System.ComponentModel.ISupportInitialize)(this.lookUpWalls.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LookUpEdit lookUpWalls;
        private DevExpress.XtraEditors.SimpleButton btnSure;
    }
}
namespace MbSecondEachPickupByLPNForm
{
    partial class ChangeWallDia
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
            this.lookUpWalls = new DevExpress.XtraEditors.LookUpEdit();
            this.btnSure = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.lookUpWalls.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // lookUpWalls
            // 
            this.lookUpWalls.Location = new System.Drawing.Point(96, 48);
            this.lookUpWalls.Name = "lookUpWalls";
            this.lookUpWalls.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lookUpWalls.Properties.Appearance.Options.UseFont = true;
            this.lookUpWalls.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.lookUpWalls.Properties.ShowHeader = false;
            this.lookUpWalls.Size = new System.Drawing.Size(274, 26);
            this.lookUpWalls.TabIndex = 3;
            // 
            // btnSure
            // 
            this.btnSure.Location = new System.Drawing.Point(312, 128);
            this.btnSure.Name = "btnSure";
            this.btnSure.Size = new System.Drawing.Size(87, 27);
            this.btnSure.TabIndex = 4;
            this.btnSure.Text = "确定";
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl1.Location = new System.Drawing.Point(24, 96);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(94, 19);
            this.labelControl1.TabIndex = 5;
            this.labelControl1.Text = "labelControl1";
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl2.Location = new System.Drawing.Point(24, 53);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(54, 19);
            this.labelControl2.TabIndex = 6;
            this.labelControl2.Text = "集货墙:";
            // 
            // labelControl3
            // 
            this.labelControl3.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl3.Location = new System.Drawing.Point(24, 16);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(166, 19);
            this.labelControl3.TabIndex = 7;
            this.labelControl3.Text = "请选择更换的集货墙号:";
            // 
            // ChangeWallDia
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 172);
            this.Controls.Add(this.labelControl3);
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.btnSure);
            this.Controls.Add(this.lookUpWalls);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ChangeWallDia";
            this.ShowIcon = false;
            this.Text = "换墙";
            ((System.ComponentModel.ISupportInitialize)(this.lookUpWalls.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.LookUpEdit lookUpWalls;
        private DevExpress.XtraEditors.SimpleButton btnSure;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl3;
    }
}
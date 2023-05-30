namespace MbSecondEachPickupByLPNForm
{
    partial class ExceptionRecordDiaForSecondPickupLPN
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
            this.components = new System.ComponentModel.Container();
            this.bsExceptionInfo = new System.Windows.Forms.BindingSource(this.components);
            this.layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            this.btnCancle = new DevExpress.XtraEditors.SimpleButton();
            this.btnOK = new DevExpress.XtraEditors.SimpleButton();
            this.txtSkuCode = new DevExpress.XtraEditors.TextEdit();
            this.gcExceptionInfo = new bbbt.ControlLibrary.Winform.WMSGridControl();
            this.gvExceptionInfo = new bbbt.ControlLibrary.Winform.WMSGridView();
            this.colqtyUom1 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colskuCode1 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colskuName1 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colexceptionType1 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colfixStatusId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumn1 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.txtOperationType = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.coloperatePerson = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumn3 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumn5 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumn2 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.txtRecordSteup = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.gridColumn4 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.txtStatusName = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem3 = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem2 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlItem4 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.bsExceptionInfo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtSkuCode.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcExceptionInfo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvExceptionInfo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtOperationType)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRecordSteup)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtStatusName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).BeginInit();
            this.SuspendLayout();
            // 
            // bsExceptionInfo
            // 
            this.bsExceptionInfo.DataSource = typeof(bbbt.WMS.Entities.MbOperateExceptionLogInfo);
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.btnCancle);
            this.layoutControl1.Controls.Add(this.btnOK);
            this.layoutControl1.Controls.Add(this.txtSkuCode);
            this.layoutControl1.Controls.Add(this.gcExceptionInfo);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.Root = this.layoutControlGroup1;
            this.layoutControl1.Size = new System.Drawing.Size(856, 479);
            this.layoutControl1.TabIndex = 12;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // btnCancle
            // 
            this.btnCancle.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancle.Appearance.Options.UseFont = true;
            this.btnCancle.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancle.Location = new System.Drawing.Point(730, 441);
            this.btnCancle.MaximumSize = new System.Drawing.Size(0, 50);
            this.btnCancle.Name = "btnCancle";
            this.btnCancle.Size = new System.Drawing.Size(114, 26);
            this.btnCancle.StyleController = this.layoutControl1;
            this.btnCancle.TabIndex = 13;
            this.btnCancle.Text = "取消";
            // 
            // btnOK
            // 
            this.btnOK.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOK.Appearance.Options.UseFont = true;
            this.btnOK.Location = new System.Drawing.Point(625, 441);
            this.btnOK.MaximumSize = new System.Drawing.Size(0, 50);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(101, 26);
            this.btnOK.StyleController = this.layoutControl1;
            this.btnOK.TabIndex = 13;
            this.btnOK.Text = "确认";
            // 
            // txtSkuCode
            // 
            this.txtSkuCode.Location = new System.Drawing.Point(79, 12);
            this.txtSkuCode.MaximumSize = new System.Drawing.Size(500, 35);
            this.txtSkuCode.MinimumSize = new System.Drawing.Size(500, 35);
            this.txtSkuCode.Name = "txtSkuCode";
            this.txtSkuCode.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSkuCode.Properties.Appearance.Options.UseFont = true;
            this.txtSkuCode.Size = new System.Drawing.Size(500, 30);
            this.txtSkuCode.StyleController = this.layoutControl1;
            this.txtSkuCode.TabIndex = 14;
            // 
            // gcExceptionInfo
            // 
            this.gcExceptionInfo.DataSource = this.bsExceptionInfo;
            this.gcExceptionInfo.Location = new System.Drawing.Point(12, 51);
            this.gcExceptionInfo.MainView = this.gvExceptionInfo;
            this.gcExceptionInfo.Name = "gcExceptionInfo";
            this.gcExceptionInfo.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.txtRecordSteup,
            this.txtStatusName,
            this.txtOperationType});
            this.gcExceptionInfo.Size = new System.Drawing.Size(832, 386);
            this.gcExceptionInfo.TabIndex = 13;
            this.gcExceptionInfo.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvExceptionInfo});
            // 
            // gvExceptionInfo
            // 
            this.gvExceptionInfo.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colqtyUom1,
            this.colskuCode1,
            this.colskuName1,
            this.colexceptionType1,
            this.colfixStatusId,
            this.gridColumn1,
            this.coloperatePerson,
            this.gridColumn3,
            this.gridColumn5,
            this.gridColumn2,
            this.gridColumn4});
            this.gvExceptionInfo.GridControl = this.gcExceptionInfo;
            this.gvExceptionInfo.Name = "gvExceptionInfo";
            this.gvExceptionInfo.OptionsBehavior.ReadOnly = true;
            this.gvExceptionInfo.OptionsView.ShowGroupPanel = false;
            // 
            // colqtyUom1
            // 
            this.colqtyUom1.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.colqtyUom1.AppearanceCell.ForeColor = System.Drawing.Color.Red;
            this.colqtyUom1.AppearanceCell.Options.UseFont = true;
            this.colqtyUom1.AppearanceCell.Options.UseForeColor = true;
            this.colqtyUom1.Caption = "数量";
            this.colqtyUom1.FieldName = "qtyUom";
            this.colqtyUom1.Name = "colqtyUom1";
            this.colqtyUom1.Visible = true;
            this.colqtyUom1.VisibleIndex = 2;
            // 
            // colskuCode1
            // 
            this.colskuCode1.Caption = "货物代码";
            this.colskuCode1.FieldName = "skuCode";
            this.colskuCode1.Name = "colskuCode1";
            this.colskuCode1.Visible = true;
            this.colskuCode1.VisibleIndex = 0;
            // 
            // colskuName1
            // 
            this.colskuName1.Caption = "货物名称";
            this.colskuName1.FieldName = "skuName";
            this.colskuName1.Name = "colskuName1";
            this.colskuName1.Visible = true;
            this.colskuName1.VisibleIndex = 1;
            // 
            // colexceptionType1
            // 
            this.colexceptionType1.Caption = "异常类型";
            this.colexceptionType1.FieldName = "ExceptionTypeName";
            this.colexceptionType1.Name = "colexceptionType1";
            this.colexceptionType1.Visible = true;
            this.colexceptionType1.VisibleIndex = 6;
            // 
            // colfixStatusId
            // 
            this.colfixStatusId.Caption = "货物状态";
            this.colfixStatusId.FieldName = "fixStatusForDisp";
            this.colfixStatusId.Name = "colfixStatusId";
            this.colfixStatusId.Visible = true;
            this.colfixStatusId.VisibleIndex = 3;
            // 
            // gridColumn1
            // 
            this.gridColumn1.Caption = "作业类型";
            this.gridColumn1.ColumnEdit = this.txtOperationType;
            this.gridColumn1.Name = "gridColumn1";
            // 
            // txtOperationType
            // 
            this.txtOperationType.AutoHeight = false;
            this.txtOperationType.Name = "txtOperationType";
            this.txtOperationType.NullText = "拣货";
            // 
            // coloperatePerson
            // 
            this.coloperatePerson.Caption = "作业人";
            this.coloperatePerson.FieldName = "operatePerson";
            this.coloperatePerson.Name = "coloperatePerson";
            this.coloperatePerson.Visible = true;
            this.coloperatePerson.VisibleIndex = 4;
            // 
            // gridColumn3
            // 
            this.gridColumn3.Caption = "订单号";
            this.gridColumn3.FieldName = "sohCodeOrCodeList";
            this.gridColumn3.Name = "gridColumn3";
            this.gridColumn3.Visible = true;
            this.gridColumn3.VisibleIndex = 5;
            // 
            // gridColumn5
            // 
            this.gridColumn5.Caption = "分组号";
            this.gridColumn5.FieldName = "pkhCode";
            this.gridColumn5.Name = "gridColumn5";
            // 
            // gridColumn2
            // 
            this.gridColumn2.Caption = "登记环节";
            this.gridColumn2.ColumnEdit = this.txtRecordSteup;
            this.gridColumn2.Name = "gridColumn2";
            // 
            // txtRecordSteup
            // 
            this.txtRecordSteup.AutoHeight = false;
            this.txtRecordSteup.Name = "txtRecordSteup";
            this.txtRecordSteup.NullText = "二次分拣";
            // 
            // gridColumn4
            // 
            this.gridColumn4.Caption = "状态";
            this.gridColumn4.ColumnEdit = this.txtStatusName;
            this.gridColumn4.Name = "gridColumn4";
            // 
            // txtStatusName
            // 
            this.txtStatusName.AutoHeight = false;
            this.txtStatusName.Name = "txtStatusName";
            this.txtStatusName.NullText = "待处理";
            // 
            // layoutControlGroup1
            // 
            this.layoutControlGroup1.CustomizationFormText = "layoutControlGroup1";
            this.layoutControlGroup1.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.layoutControlGroup1.GroupBordersVisible = false;
            this.layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.emptySpaceItem1,
            this.layoutControlItem1,
            this.layoutControlItem2,
            this.layoutControlItem3,
            this.emptySpaceItem2,
            this.layoutControlItem4});
            this.layoutControlGroup1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlGroup1.Name = "layoutControlGroup1";
            this.layoutControlGroup1.Size = new System.Drawing.Size(856, 479);
            this.layoutControlGroup1.Text = "layoutControlGroup1";
            this.layoutControlGroup1.TextVisible = false;
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.CustomizationFormText = "emptySpaceItem1";
            this.emptySpaceItem1.Location = new System.Drawing.Point(571, 0);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(265, 39);
            this.emptySpaceItem1.Text = "emptySpaceItem1";
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.gcExceptionInfo;
            this.layoutControlItem1.CustomizationFormText = "layoutControlItem1";
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 39);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Size = new System.Drawing.Size(836, 390);
            this.layoutControlItem1.Text = "layoutControlItem1";
            this.layoutControlItem1.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem1.TextToControlDistance = 0;
            this.layoutControlItem1.TextVisible = false;
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.AppearanceItemCaption.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.layoutControlItem2.AppearanceItemCaption.Options.UseFont = true;
            this.layoutControlItem2.Control = this.txtSkuCode;
            this.layoutControlItem2.CustomizationFormText = "异常货物";
            this.layoutControlItem2.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Size = new System.Drawing.Size(571, 39);
            this.layoutControlItem2.Text = "货物条码";
            this.layoutControlItem2.TextSize = new System.Drawing.Size(64, 19);
            // 
            // layoutControlItem3
            // 
            this.layoutControlItem3.Control = this.btnOK;
            this.layoutControlItem3.CustomizationFormText = "layoutControlItem3";
            this.layoutControlItem3.Location = new System.Drawing.Point(613, 429);
            this.layoutControlItem3.Name = "layoutControlItem3";
            this.layoutControlItem3.Size = new System.Drawing.Size(105, 30);
            this.layoutControlItem3.Text = "layoutControlItem3";
            this.layoutControlItem3.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem3.TextToControlDistance = 0;
            this.layoutControlItem3.TextVisible = false;
            // 
            // emptySpaceItem2
            // 
            this.emptySpaceItem2.AllowHotTrack = false;
            this.emptySpaceItem2.CustomizationFormText = "emptySpaceItem2";
            this.emptySpaceItem2.Location = new System.Drawing.Point(0, 429);
            this.emptySpaceItem2.Name = "emptySpaceItem2";
            this.emptySpaceItem2.Size = new System.Drawing.Size(613, 30);
            this.emptySpaceItem2.Text = "emptySpaceItem2";
            this.emptySpaceItem2.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlItem4
            // 
            this.layoutControlItem4.Control = this.btnCancle;
            this.layoutControlItem4.CustomizationFormText = "layoutControlItem4";
            this.layoutControlItem4.Location = new System.Drawing.Point(718, 429);
            this.layoutControlItem4.Name = "layoutControlItem4";
            this.layoutControlItem4.Size = new System.Drawing.Size(118, 30);
            this.layoutControlItem4.Text = "layoutControlItem4";
            this.layoutControlItem4.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem4.TextToControlDistance = 0;
            this.layoutControlItem4.TextVisible = false;
            // 
            // ExceptionRecordDiaForSecondPickup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(856, 479);
            this.Controls.Add(this.layoutControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExceptionRecordDiaForSecondPickup";
            this.Text = "异常登记";
            ((System.ComponentModel.ISupportInitialize)(this.bsExceptionInfo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtSkuCode.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcExceptionInfo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvExceptionInfo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtOperationType)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRecordSteup)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtStatusName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.BindingSource bsExceptionInfo;
        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private bbbt.ControlLibrary.Winform.WMSGridControl gcExceptionInfo;
        private bbbt.ControlLibrary.Winform.WMSGridView gvExceptionInfo;
        private DevExpress.XtraGrid.Columns.GridColumn colqtyUom1;
        private DevExpress.XtraGrid.Columns.GridColumn colskuCode1;
        private DevExpress.XtraGrid.Columns.GridColumn colskuName1;
        private DevExpress.XtraGrid.Columns.GridColumn colexceptionType1;
        private DevExpress.XtraGrid.Columns.GridColumn colfixStatusId;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraEditors.TextEdit txtSkuCode;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraEditors.SimpleButton btnOK;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem3;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem2;
        private DevExpress.XtraEditors.SimpleButton btnCancle;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem4;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn1;
        private DevExpress.XtraGrid.Columns.GridColumn coloperatePerson;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn3;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn4;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn5;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn2;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit txtOperationType;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit txtRecordSteup;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit txtStatusName;
    }
}
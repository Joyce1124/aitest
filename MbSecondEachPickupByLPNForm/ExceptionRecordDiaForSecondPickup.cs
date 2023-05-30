using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using bbbt.Dialog;
using bbbt.Dialog.BusinessObject.BarCodeRules;
using bbbt.Tool;
using bbbt.Utility;
using bbbt.Utility.BarDecodingRule;
using bbbt.WMS.Entities;
using bbbt.WMS.IService;
using bbbt.WMS.ServiceFactory;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;

namespace MbSecondEachPickupByLPNForm
{
    /// <summary>
    /// 异常登记(二次分拣)
    /// </summary>
    public partial class ExceptionRecordDiaForSecondPickupLPN : XtraForm
    {
        protected string _inputCharBuff = "";//扫描枪输入字符串
        protected string _activeControlValue = "";//当前活动控件值备份
        protected Dictionary<string, Action> _lstQuickAction = new Dictionary<string, Action>();//快捷动作列表

        private readonly MbBarCodeRules_SecondPickup _curBarRules;
        private readonly List<MbNewLpndSalesOrderInfo> _lpnDNewSoInfoList;
        private readonly List<MbWorkOrderItemInfo> _woiInfoList;
        private readonly Hashtable _ht = HashTableHelper.CacheInstance.htGlobalCodeInfoCache;
        private readonly List<Control> _controlList = new List<Control>();

        private readonly IMbSkuService _skuService = ServiceFactory.GetMbSkuService();
        private readonly IMbOperateExceptionLogService _exceptionService = ServiceFactory.GetMbOperateExceptionLogService();
        private readonly IMbWorkOrderItemService _workOrderItemService = ServiceFactory.GetMbWorkOrderItemService();
        public bool IsOb056 = HashTableHelper.CacheInstance.GetServerConfigValueByConfigCode("OB056") == "Y";  //是否使用多EAN码? Y= 使用;N=不使用

        public ExceptionRecordDiaForSecondPickupLPN(MbBarCodeRules_SecondPickup curBarRule, List<MbNewLpndSalesOrderInfo> lpnDNewSoInfoList)
        {
            InitializeComponent();
            InitUi();
            _curBarRules = curBarRule;
            _lpnDNewSoInfoList = lpnDNewSoInfoList;
            _woiInfoList = GetWoiList();
            _controlList.Add(txtSkuCode);
            _lstQuickAction.Add("F2", BtnOkClick); //F2提交
        }

        private void InitUi()
        {
            StartPosition = FormStartPosition.CenterParent;
            txtSkuCode.KeyPress += txtSkuCode_KeyPress;
            txtSkuCode.Click += txtSkuCode_Click;
            btnOK.Click += btnOK_Click;
            btnCancle.Click += btnCancle_Click;
        }

        void txtSkuCode_Click(object sender, EventArgs e)
        {
            txtSkuCode.SelectAll();
        }

        /// <summary>
        /// 条码扫描
        /// </summary>
        void txtSkuCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Tools.ScanShortcutCode(sender, e.KeyChar, ref _inputCharBuff, ref _activeControlValue, _lstQuickAction))            
                return;
            
            if(e.KeyChar != '\r')
                return;
            ToolTipHelper.RestoreControl(_controlList);
            if(string.IsNullOrEmpty(txtSkuCode.Text))
            {
                ToolTipHelper.SetToolTip(txtSkuCode, "提示", "货物条码不能为空!");
                MediaHelper.PlaySoundErrorByBarCode();
                return;
            }
            if (_woiInfoList.Count == 0 || _lpnDNewSoInfoList.Count == 0 )
            {
                ToolTipHelper.SetToolTip(txtSkuCode, "提示", "没有工单项信息!");
                MediaHelper.PlaySoundErrorByBarCode();
                txtSkuCode.SelectAll();
                return;
            }
            var skuCodeOrEanList = IsKboxingCustomer(_lpnDNewSoInfoList[0].manufacturerCode) ? _curBarRules.ParseSkuCodeOrEanListForJB(KBoxingRule.GetSkuListByRule(txtSkuCode.Text.Trim()))
                                                              : _curBarRules.ParseSkuCodeOrEanList(txtSkuCode.Text.Trim());
            var selectSkuInfo = GetSkuInfo(skuCodeOrEanList);
            if (selectSkuInfo == null)
            {
                txtSkuCode.SelectAll();
                return;
            }
            var moreSkuLpnD = MoreSkuLpnD(selectSkuInfo);
            if (moreSkuLpnD != null)
            {
                //说明是多拣
                RecordSkuWhenMore(moreSkuLpnD);
                txtSkuCode.SelectAll();
                return;
            }
            RecordSkuWhenError(selectSkuInfo);
            txtSkuCode.SelectAll();
        }

        /// <summary>
        /// 判断选择的sku是否在列表中存在，存在即为多拣
        /// </summary>
        private MbNewLpndSalesOrderInfo MoreSkuLpnD(MbSkuInfo selectSkuInfo)
        {
            foreach (var lpndSoInfo in _lpnDNewSoInfoList)
            {
                if (lpndSoInfo.skuId == selectSkuInfo.id)
                    return lpndSoInfo;
            }
            return null;
        }

        /// <summary>
        /// 判断客户是否是劲霸
        /// </summary>
        private bool IsKboxingCustomer(string manufactureCode)
        {
            if (KBoxingRule.IsKboxingManufactureCode(manufactureCode))
            {
                return true;
            }
            return false;
        }

        private void BtnOkClick()
        {
            btnOK_Click(null, null);
        }

        /// <summary>
        /// 确认
        /// </summary>
        void btnOK_Click(object sender, EventArgs e)
        {
            if (bsExceptionInfo.List.Count == 0)
            {
                ToolTipHelper.SetToolTip(txtSkuCode, "提示", "请先扫描货物");
                return;
            }
            var pkhId = _lpnDNewSoInfoList[0].pkhId;
            var voList = (from object obj in bsExceptionInfo.List select obj as MbOperateExceptionLogInfo).ToList();
            BehaviorReturn br = _exceptionService.CreateMbOperateExceptionLogByPkhId(voList, pkhId, 1);
            if(!br.Success)
            {
                ToolTipHelper.SetToolTip(txtSkuCode, "提示", br.hasMessage() ? br.MessageList[0].messageCN : "保存数据出错");
                MediaHelper.PlaySoundErrorByBarCode();
                return;
            }
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// 取消
        /// </summary>
        void btnCancle_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        # region 多拣
        /// <summary>
        /// 登记多拣(新增),单位后台处理
        /// </summary>
        private void RecordSkuWhenMore(MbNewLpndSalesOrderInfo lpnD)
        {
            int position = 0;
            foreach (MbOperateExceptionLogInfo exception in bsExceptionInfo.List)
            {
                if (exception.skuId != lpnD.skuId)
                {
                    position++;
                    continue;
                }
                exception.qtyUom++;
                bsExceptionInfo.Position = position;
                bsExceptionInfo.ResetBindings(false);
                MediaHelper.PlaySoundSysSuccess();
                return;
            }
            var exceptionInfo = new MbOperateExceptionLogInfo
            {
                skuId = lpnD.skuId,
                operateTypeId = GetOperateTypeId(CodeConcentrationCamp.DOC_TYP_PK), //作业类型
                operateTime = GetNotNullTime(),
                operatePerson = GetSkuOperatePersons(lpnD.skuCode),
                sohCodeOrCodeList = GetSkuSaleOrderNos(lpnD.skuCode),
                customerId = _woiInfoList[0].customerId,
                pkhCode = lpnD.pkhCode,
                pkhId = lpnD.pkhId,
                skuCode = lpnD.skuCode,
                skuName = lpnD.skuName,
                qtyUom = 1,
                ExceptionTypeName = "多拣",
                exceptionTypeId = GetException(CodeConcentrationCamp.EXCEP_PK_MORE),
                creatorCode = ClientService.CurrentUserInfo.code,
                logFlowStageId = GetOperateTypeId(CodeConcentrationCamp.DOC_TYP_ONWALL), //登记环节,固定值“二次分拣”
                whId = ClientService.CurrentWareId,
                orgId = ClientService.CurrentOrgId
            };
            GetSkuFixStatusId(GetLotHeaderId(lpnD.skuId), exceptionInfo);
            bsExceptionInfo.Insert(0, exceptionInfo);
            bsExceptionInfo.MoveFirst();
            bsExceptionInfo.ResetBindings(false);
            gvExceptionInfo.bbbtFitColumns();
            MediaHelper.PlaySoundSysSuccess();
        }

        private DateTime GetNotNullTime()
        {
            foreach (var woi in _woiInfoList)
            {
                if (woi.completeTime != DateTime.MinValue)
                {
                    return woi.completeTime;
                }
            }
            return DateTime.MinValue;
        }

        /// <summary>
        /// 老版LPND不维护批次，所以需要掉服务去查sku
        /// </summary>
        private long GetLotHeaderId(long skuId)
        {
            BehaviorReturn skuBr = _skuService.GetSkuByID(skuId);
            if (!skuBr.Success || !skuBr.hasEntity())
                return 0;          
            var skuInfo = skuBr.ObjectList[0] as MbSkuInfo;
            return skuInfo != null ? skuInfo.lotId : 0;
        }

        /// <summary>
        /// 得到异常类型
        /// </summary>
        private long GetException(string code)
        {
            if (!_ht.ContainsKey("EXCEPTION_TYP"))
            {
                return 0;
            }
            return (from object obj in (ArrayList) _ht["EXCEPTION_TYP"] select obj as MbCodeInfo into codeInfo where codeInfo != null && codeInfo.code == code select codeInfo.id).FirstOrDefault();
        }

        /// <summary>
        /// 得到交易类型
        /// </summary>
        private long GetOperateTypeId(string code)
        {
            if (!_ht.ContainsKey("TAN_TYP"))
            {
                return 0;
            }
            return (from object obj in (ArrayList) _ht["TAN_TYP"] select obj as MbCodeInfo into codeInfo where codeInfo != null && codeInfo.code == code select codeInfo.id).FirstOrDefault();
        }

        /// <summary>
        /// 获取分组内/LPN内对应SKU所在所有woi的拣货人
        /// </summary>
        private string GetSkuOperatePersons(string skuCode)
        {
            string operatePersons = null;
            var operatePersonList = new List<string>();
            foreach(var woi in _woiInfoList)
            {
                if (!string.IsNullOrEmpty(woi.skuCode) && woi.skuCode.ToUpper() == skuCode.ToUpper().Trim() && !operatePersonList.Contains(woi.operatorName))
                {
                    operatePersons += woi.operatorName +",";
                    operatePersonList.Add(woi.operatorName);
                }
            }
            return RemoveLastDot(operatePersons);
        }

        /// <summary>
        /// 获取分组内/LPN内对应SKU所在所有woi的订单号
        /// </summary>
        private string GetSkuSaleOrderNos(string skuCode)
        {
            string saleOrders = null;
            var saleOrderList = new List<string>();
            foreach (var woi in _woiInfoList)
            {
                if (!string.IsNullOrEmpty(woi.skuCode) && woi.skuCode.ToUpper() == skuCode.ToUpper().Trim() && !saleOrderList.Contains(woi.docNo)
                    && (woi.statusCode == CodeConcentrationCamp.WO_FIN || woi.statusCode == CodeConcentrationCamp.WO_PART))
                {
                    saleOrders += woi.docNo + ",";
                    saleOrderList.Add(woi.docNo);
                }
            }
            return RemoveLastDot(saleOrders);
        }

        /// <summary>
        /// 去除字符末尾的 逗号
        /// </summary>
        private string RemoveLastDot(string str)
        {
            if(string.IsNullOrEmpty(str))
                return null;
            return str.Substring(0, str.Length -1);
        }

        /// <summary>
        /// 获取分组内/LPN内对应SKU所在任一woi的单位
        /// </summary>
        private void GetSkuUomId(string skuCode, MbOperateExceptionLogInfo exceptionInfo)
        {
            foreach (var woi in _woiInfoList)
            {
                if (!string.IsNullOrEmpty(woi.skuCode) && woi.skuCode.ToUpper() == skuCode.ToUpper().Trim())
                {
                    exceptionInfo.uomId = woi.uomId;
                    return;
                }
            }
        }

        /// <summary>
        /// 货物状态等于批次所对应的codeinfro类型，代码定义里面的默认项
        /// </summary>
        private void GetSkuFixStatusId(long lotheaderId, MbOperateExceptionLogInfo exceptionInfo)
        {
            if (lotheaderId == 0)
                return;
            var lotInfoArrage = GetLotInfoByLotCode(lotheaderId);
            if (lotInfoArrage == null)
                return;
            exceptionInfo.lotHeaderId = lotheaderId;
            MbBasLotDetailInfo lotInfoForCodeInfo = lotInfoArrage.Cast<MbBasLotDetailInfo>().FirstOrDefault(lotInfo => lotInfo.lotAttItemCode != null && lotInfo.lotAttLabel == "货物状态");
            if (lotInfoForCodeInfo == null)
                return;
            var codeInfoList = GetCodeInfoList(lotInfoForCodeInfo.lotAttItemCode);
            foreach (MbCodeInfo codeinfo in codeInfoList)
            {
                if (codeinfo != null && codeinfo.defaultCode)
                {
                    exceptionInfo.fixStatusId = codeinfo.id;
                    exceptionInfo.fixStatusForDisp = codeinfo.cnCodeName;
                    return;
                }
            }
        }

        /// <summary>
        /// 获取批次属性列表
        /// </summary>
        public ArrayList GetLotInfoByLotCode(long lotheaderId)
        {
            if (!HashTableHelper.CacheInstance.htUserControlAtriLotCodeCache.Contains(lotheaderId))
            {
                if (lotheaderId != 0)
                {
                    IMbBasLotHeaderService lotservice = ServiceFactory.GetMbBasLotService();
                    BehaviorReturn br = lotservice.GetBasLotDetailsByHeaderId(lotheaderId);
                    if (br.hasEntity())
                    {
                        HashTableHelper.CacheInstance.htUserControlAtriLotCodeCache.Add(lotheaderId, br.ObjectList);
                    }
                }
            }
            if (HashTableHelper.CacheInstance.htUserControlAtriLotCodeCache.Contains(lotheaderId))
            {
                var lotInfoArrage = HashTableHelper.CacheInstance.htUserControlAtriLotCodeCache[lotheaderId] as ArrayList;
                return lotInfoArrage;
            }
            return null;
        }

        /// <summary>
        /// 获取货物状态列表
        /// </summary>
        public static ArrayList GetCodeInfoList(string codeClassCode)
        {
            if (!string.IsNullOrEmpty(codeClassCode))
            {
                if (!HashTableHelper.CacheInstance.htUserControlAtriCodeInfoCache.Contains(codeClassCode))
                {
                    IMbCodeInfoService codeservice = ServiceFactory.GetMbCodeInfoService();
                    var brSel = codeservice.GetCodeInfoByCodeClassCode(codeClassCode);
                    if (brSel.hasEntity())
                    {
                        HashTableHelper.CacheInstance.htUserControlAtriCodeInfoCache.Add(codeClassCode, brSel.ObjectList);
                    }
                }
                if (HashTableHelper.CacheInstance.htUserControlAtriCodeInfoCache.Contains(codeClassCode))
                {
                    var alCodeInfoList = HashTableHelper.CacheInstance.htUserControlAtriCodeInfoCache[codeClassCode] as ArrayList;
                    return alCodeInfoList;
                }
            }
            return null;
        }

        # endregion

        #region 错拣
        /// <summary>
        /// 获取错拣的sku, 单位后台处理
        /// </summary>
        private MbSkuInfo GetSkuInfo(List<string> skuCodeOrEanList)
        {
            var manufacturerIdList = new List<long>();
            foreach (var obj in _lpnDNewSoInfoList.Where(obj => !manufacturerIdList.Contains(obj.manufacturerId)))
            {
                manufacturerIdList.Add(obj.manufacturerId);
            }
            BehaviorReturn br = _skuService.GetSkuByMfrIdListAndCodeOrEanList(manufacturerIdList.ToArray(), skuCodeOrEanList.ToArray());
            if(!br.Success || !br.hasEntity())
            {
                ToolTipHelper.SetToolTip(txtSkuCode, "提示", br.hasMessage() ? br.MessageList[0].messageCN : "找不到该货物");
                MediaHelper.PlaySoundErrorByBarCode();
                return null;
            }
            var skuInfolist = br.ObjectList.Cast<MbSkuInfo>().ToList();
            if (skuInfolist.Count == 1)
            {
                return skuInfolist[0];
            }
            var cc = EditHelper.GetVirtualControlByBarButtonItem(this, new BarButtonItem { Name = "MbSelectSkuDialog" });
            var selectSkuDialog = new MbSelectSkuDialog(skuInfolist);
            selectSkuDialog.CurrentOwnerControl = this;
            selectSkuDialog.CurrentOwnerControlForLayout = new ControlForLayout
            {
                CtlParent = new ControlForLayout(this),
                CtlSelf = cc
            };//布局相关
            selectSkuDialog.ShowDialog();
            if (selectSkuDialog.DialogResult != DialogResult.OK)
                return null;
            return selectSkuDialog.MbSelectedSku;
        }

        /// <summary>
        /// 登记错拣
        /// </summary>
        private void RecordSkuWhenError(MbSkuInfo skuInfo)
        {
            int position = 0;
            foreach (MbOperateExceptionLogInfo exception in bsExceptionInfo.List)
            {
                if (exception.skuId != skuInfo.id)
                {
                    position++;
                    continue;
                }
                exception.qtyUom++;
                bsExceptionInfo.Position = position;
                bsExceptionInfo.ResetBindings(false);
                MediaHelper.PlaySoundSysSuccess();
                return;
            }
            var exceptionInfo = new MbOperateExceptionLogInfo
                {
                    skuId = skuInfo.id,
                    operateTypeId = GetOperateTypeId(CodeConcentrationCamp.DOC_TYP_PK), //作业类型
                    operateTime = GetLatestPickupTime(), //
                    operatePerson = GetAllOperatePersons(),
                    sohCodeOrCodeList = null,
                    pkhCode =  _lpnDNewSoInfoList[0].pkhCode,
                    pkhId =  _lpnDNewSoInfoList[0].pkhId,
                    skuCode = skuInfo.skuCode,
                    skuName = skuInfo.descrCn,
                    qtyUom = 1,
                    ExceptionTypeName = "错拣",
                    exceptionTypeId = GetException(CodeConcentrationCamp.EXCEP_PK_WRONG),
                    creatorCode = ClientService.CurrentUserInfo.code,
                    lotHeaderId = skuInfo.lotId,
                    logFlowStageId = GetOperateTypeId(CodeConcentrationCamp.DOC_TYP_ONWALL), //登记环节,固定值“二次分拣”
                    whId = ClientService.CurrentWareId,
                    orgId = ClientService.CurrentOrgId
                };
            GetSkuFixStatusId(skuInfo.lotId, exceptionInfo);
            bsExceptionInfo.Insert(0, exceptionInfo);
            bsExceptionInfo.MoveFirst();
            bsExceptionInfo.ResetBindings(false);
            gvExceptionInfo.bbbtFitColumns();
            MediaHelper.PlaySoundSysSuccess();            
        }

        /// <summary>
        /// 获取分组内/LPN内对应SKU所在所有woi的拣货人
        /// </summary>
        private string GetAllOperatePersons()
        {
            string operatePersons = null;
            var operatePersonList = new List<string>();
            foreach (var woi in _woiInfoList)
            {
                if (!string.IsNullOrEmpty(woi.operatorName) && !operatePersonList.Contains(woi.operatorName))
                {
                    operatePersons += woi.operatorName + ",";
                    operatePersonList.Add(woi.operatorName);
                }
            }
            return RemoveLastDot(operatePersons);
        }

        /// <summary>
        /// 获取分组内/LPN内最晚的拣货确认时间
        /// </summary>
        private DateTime GetLatestPickupTime()
        {
            var maxPickupTime = DateTime.MinValue;
            foreach (var woi in _woiInfoList)
            {
                if (DateTime.Compare(woi.completeTime, maxPickupTime) > 0)
                {
                    maxPickupTime = woi.completeTime;
                }
            }
            return maxPickupTime;
        }

        #endregion

        /// <summary>
        /// 判断是多拣还是错拣（返回值不为Nul说明是多拣）,任意一个skuCode相等就是多拣
        /// </summary>
        private MbNewLpndSalesOrderInfo IsErrorOrMoreSku(List<string> skuCodeOrEanList)
        {
            if (IsOb056)
            {
                foreach (var ean in skuCodeOrEanList)
                {
                    foreach (var lpnD in _lpnDNewSoInfoList)
                    {
                        var skuMulEanCode = lpnD.skuEanCode ?? "";
                        var skuEanCodeList = skuMulEanCode.Split(new char[] { ';', '；' });
                        if (IsEqual(lpnD.skuCode, ean))
                        {
                            return lpnD;
                        }
                        foreach (var skuEanCode in skuEanCodeList)
                        {
                            if (IsEqual(skuEanCode, ean))
                            {
                                return lpnD;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var ean in skuCodeOrEanList)
                {
                    foreach (var lpnD in _lpnDNewSoInfoList)
                    {
                        if ((IsEqual(lpnD.skuCode, ean) || IsEqual(lpnD.skuEanCode, ean)
                                              || IsEqual(lpnD.skuEanCode2, ean) || IsEqual(lpnD.skuEanCode3, ean) || IsEqual(lpnD.skuEanCode4, ean)
                                              || IsEqual(lpnD.skuEanCode5, ean) || IsEqual(lpnD.skuEanCode6, ean)) && lpnD.id != 0)
                        {
                            return lpnD;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 根据LPND获取woi
        /// </summary>
        private List<MbWorkOrderItemInfo> GetWoiList()
        {
            var woiIdList = _lpnDNewSoInfoList.Select(lpnd => lpnd.woiId).ToList();
            var woiInfoList = new List<MbWorkOrderItemInfo>();
            BehaviorReturn br = _workOrderItemService.GetWorkOrderItems(woiIdList.ToArray());
            if(br.hasEntity())
            {
                woiInfoList.AddRange(br.ObjectList.Cast<MbWorkOrderItemInfo>());
            }
            return woiInfoList;
        }

        private bool IsEqual(string skuCode, string skuCodeOrEan)
        {
            if (string.IsNullOrEmpty(skuCode))
            {
                return false;
            }
            return skuCode.ToUpper() == skuCodeOrEan.ToUpper();
        }
    }
}

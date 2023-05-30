using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using bbbt.Base;
using bbbt.Dialog;
using bbbt.Dialog.BusinessObject.BarCodeRules;
using bbbt.Tool;
using bbbt.Utility;
using bbbt.Utility.BarDecodingRule;
using bbbt.Utility.bbbtLog;
using bbbt.Utility.WinForm;
using bbbt.WMS.Entities;
using bbbt.WMS.IControllerService;
using bbbt.WMS.IService;
using bbbt.WMS.ServiceFactory;
using DevExpress.Data.PLinq.Helpers;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Alerter;
using DevExpress.XtraEditors;
using Timer = System.Windows.Forms.Timer;

namespace MbSecondEachPickupByLPNForm
{
    public enum LoadType
    {
        None,

        LPNCode,

        WaveCode
    }

    public enum PickingType
    {
        None,

        [Description("逐件分拣")]
        PiecePicking,

        [Description("每种SO批量SKU分拣")]
        BatchBySoPicking,
        
        [Description("批量SKU分拣")]
        BatchAllPicking
    }

    /// <summary>
    /// Orange橘黄色：Vo. udf3 = YELLOW   packVo. udf3  = cellIndex    外部取消的单子
    /// White白色: qtyOutputUom = 0   量等于0的， 还没开始上墙的
    /// Red红色 : qtyUom > qtyOutputUom    未完全上墙的
    /// Green绿色 ： 上墙量 = 拣货量 、缺格子号的情况默认都是绿色完成
    /// 一个格子只会对应一个订单
    /// 配置项为2时，一定要先扫描全部的LPN号
    /// </summary>
    public partial class SecondEachPickupByLPNForm : BaseForm
    {
        private List<long> _sohIdList = new List<long>();   //当前LPN、分组中需要分拣结束的订单IdList 
        private LoadType _curType;
        private PickingType _curPickingType;
        private MbBarCodeRules_SecondPickup _curBarRules;
        private double _curCellTotalPickingQty;
        private int _curCellIndex;
        private CellSkuInfo _curCellSkuInfo;
        private List<MbNewLpndSalesOrderInfo> _curSameCellWorkInfoList;
        private List<MbCellInfo> _cellInfoLis = new List<MbCellInfo>();
        private readonly Dictionary<string, SkuScanDetail> _dicScanDetail = new Dictionary<string, SkuScanDetail>();  //当前扫描明细,key:cellIndex+skuCode
        private List<MbNewLpndSalesOrderInfo> _allDWorkInfoList = new List<MbNewLpndSalesOrderInfo>(); //分组号所对应的全部 lpnd
        private readonly List<MbNewLpndSalesOrderInfo> _curLPNDWorkInfoList = new List<MbNewLpndSalesOrderInfo>(); //当前LPN号所对应的 lpnd
        private readonly Dictionary<string, List<long>> _curAllCellAndSohId = new Dictionary<string, List<long>>();//当前分组下所有的对应关系，去除状态为 关闭和取消的 lpnd
        private readonly Dictionary<string, List<long>> _dicExistCellAndSohId = new Dictionary<string, List<long>>(); //当配置为一个分货格一个LPN时,存在后台的数据，key:格子号，value:所对应的订单号
        private readonly Dictionary<long, Double> _dicSameCellWorkIdAndqtyUom = new Dictionary<long, double>();//当扫描频率为批量扫描的时候，记录每个符合的obf的扫描量

        private BaseCellControl _curCellsControl;
        private readonly TwoHundredCellsControl _twoHundredCellsControl = new TwoHundredCellsControl();
        private readonly OneoHundredFiftyCellsControl _oneoHundredFiftyCellsControl = new OneoHundredFiftyCellsControl();
        private readonly OneoHundredCellsControl _oneoHundredCellsControl = new OneoHundredCellsControl();
        private readonly IMbAssemblerMoveWorkOrderItemService _assemblerMoveWorkOrderItemService = ServiceFactory.GetMbAssemblerMoveWorkOrderItemService();
        private readonly IMbNewWorkOrderBusinessServicecs _newWorkOrderBusinessService = ServiceFactory.GetMbNewWorkOrderBusinessService();
        private readonly IMbNewLpnHService _wsNewLpnhService = ServiceFactory.GetMbNewLpnHService();
        private readonly IMbLocationService _locationService = ServiceFactory.GetMbLocationService();
        private readonly IMbNumberSetService _numberSetService = ServiceFactory.GetMbNumberSetService();
        private readonly IMbShipmentExceptionRecordService _exceptionRecordService = ServiceFactory.GetShipmentExceptionRecordService();
        private readonly IMbConfigInfoService _configurationService = ServiceFactory.GetMbConfigInfoService();//获取配置项的服务

        private string _strRegex = "";
        private readonly Timer _jobTimer = new Timer(); //作业排名推送
        private readonly AlertInfo _alertInfo = new AlertInfo(null, null);
        private readonly Timer _showTimer = new Timer(); //作业排名推送
        private readonly Timer _searchPickUpTimer = new Timer(); //串口查询推送
        private long _jobSerialNum;
        private long _curJobKey;
        private readonly Dictionary<long, List<MbJobRankingInfo>> _dicJobInfo = new Dictionary<long, List<MbJobRankingInfo>>(); //key:序号，从1开始自增，value:3个一组的jobInfo
        private readonly IniFileHelper _iniFile = new IniFileHelper(GatedLaunchHelper.GetNormalDir(Application.StartupPath) + "\\Config\\SystemConfig.ini");
        private CommonProgressBarHelper _progressBarHelper;

        private Dictionary<string, string> _dicLpnCellNo; //当配置为一个分货格一个LPN时,最新获取到的格子号和LPN号对应关系 key:LPN,value:cell
        private readonly SystemConfigurationCollection _sysConfige = new SystemConfigurationCollection();

        private string _preGroupCode; //记录前一个分组号
        private string _curWaveLpn; //当配置一个分组一个LPN时的LPN号

        private bool _isLpnFake = false;  //扫描LPN时是否均分
        private string _curWall = "";
        private bool _isFrist = true;

        public const string Yellow = "YELLOW";

        private MbNewLpndSalesOrderInfo _preLpndSo = new MbNewLpndSalesOrderInfo();   //上一枪明细
        private double _prePickNum = 0;   //上一枪扫描数量
        private bool _preIsSo = false;   //上一枪扫描是否为装箱单号
        private readonly SecondRichText _secondRichText = new SecondRichText();

        private SetupInfo _setupInfo; //页面设置

        private byte[] _preSendBytes;  //记录上次发送给串口的数据

        private readonly List<string> _preSkuCodeAndEan = new List<string>();  //记录上次扫描的全部Ean码

        private string _preScanSkuCode;  //记录上次扫描内容

        private bool _stopShowMsg;  //显示错误后先进行标志，避免重复显示错误 (遮挡)

        private bool _stopErrorMsg;  //显示错误后先进行标志，避免重复显示错误 (错投)

        private bool _stopReceiveMsg;  //显示错误后先进行标志，避免重复显示错误 (交互错误)

        private bool _stopShowSuccess;  //提示 完成后，避免重复提示成功

        private bool _continueSearch;  //前一个数据未返回,停止查询

        private bool _waitPutSign;  //是否等待投递结果，true：不等待；false:等待
        private string _scanSkuCode;//记录当前扫描的sku条码

        private MbCellInfo _writeCellInfo;  //扫描装箱单记录的信息
        private List<MbNewLpndSalesOrderInfo> _writeLpndSoList;  //扫描sku记录的信息

        private List<string> _lastScanSkuCode = new List<string>();//记录所有扫描的skucode

        private readonly long _shipTypeId = HashTableHelper.CacheInstance.GetCodeInfoIdByCodeClassCodeAndCodeInfoCodeWithOutError(CodeConcentrationCamp.CCC_SHIP_TYP, CodeConcentrationCamp.CIC_SHIP_TYP_AUTO);//发货报错类型，自动

        private MbRebinConfigInfo _checkConfigValueInfo = new MbRebinConfigInfo(); //从后台获取的配置项

        private OnWallUIConfigInfo _configInfoWhenOpen = new OnWallUIConfigInfo();//打开界面时的配置项
        private IConfigOutboundController configOutboundController = ServiceFactory.GetConfigOutboundController();  //配置项
        private OnWallBizConfigInfo _bizConfigInfo;//从后台获取的配置项

        #region  对接光栅的一些 指令
        private byte[] _searchVisionBytes = new byte[] { 0xF1, 0x01, 0x00, 0x01, 0xA0, 0x00, 0xF8 };  //查询控制器版本

        private byte[] _getAllStatusBytes = new byte[] { 0xF1, 0x01, 0x00, 0x01, 0xAB, 0x00, 0xF8 };  //获取所有格口状态

        private byte[] _setControlBytes = new byte[] { 0xF1, 0x01, 0x00, 0x02, 0xAC, 0x01, 0x00, 0xF8 };  //控制报警警报

        private readonly byte[] _searchMsgBytes = new byte[] { 0xF1, 0x01, 0x00, 0x01, 0xA4, 0x00, 0xF8 };  //查询信息

        private readonly byte[] _greenLightBytes = new byte[] { 0xF1, 0x01, 0x00, 0x06, 0xA9, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0xF8 };  //发送绿灯
        private readonly byte[] _redLightBytes = new byte[] { 0xF1, 0x01, 0x00, 0x06, 0xA9, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0xF8 };  //发送红灯
        private readonly byte[] _orangeLightBytes = new byte[] { 0xF1, 0x01, 0x00, 0x06, 0xA9, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0xF8 };  //发送黄灯

        private readonly byte[] _setPutOrderBytes = new byte[] { 0xF1, 0x01, 0x00, 0x05, 0xA3, 0x00, 0x00, 0x00, 0x05, 0x00, 0xF8 };  //设置投递命令      

        private readonly byte[] _clearOrderBytes = new byte[] { 0xF1, 0x01, 0x00, 0x01, 0xA5, 0x00, 0x00, 0x00, 0x00, 0xF8 };  //清除消息命令

        private readonly byte[] _clearStatusBytes = new byte[] { 0xF1, 0x01, 0x00, 0x01, 0xAA, 0x00, 0xF8 };  //清除所有状态命令

        private byte[] _setPortBytes = new byte[] { 0xF1, 0x01, 0x00, 0x05, 0xA2, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF8 };  //设置与扫描主机间通信波特率

        private readonly byte[] _checkSelfBytes = new byte[] { 0xF1, 0x01, 0x00, 0x01, 0xA7, 0x00, 0xF8 };  //自检
        #endregion

        public SecondEachPickupByLPNForm()
        {
            InitializeComponent();
            IntiResgite();
            InitTimer();
        }

        /// <summary>
        /// 事件注册
        /// </summary>
        private void IntiResgite()
        {
            txtLpnCode.Click += txtLpnCode_Click; //Lpn号
            txtWaveGroupCode.Click += txtWaveGroupCode_Click; //波次分组
            txtSkuCode.Click += txtSkuCode_Click; //货物条码
            barRestart.ItemClick += barRestart_ItemClick; //重置
            barScanDetail.ItemClick += barScanDetail_ItemClick; //扫描明细
            barSwitch.ItemClick += barSwitch_ItemClick; //分拣模式
            btnExceptionRecord.ItemClick += btnExceptionRecord_ItemClick; //异常登记
            lkLabModifySkuQty.Click += lkLabModifySkuQty_Click;
            barbtnStopPut.ItemClick += barbtnStopPut_ItemClick; //中止投递
            KeyDown += SecondEachPickupByLPNForm_KeyDown;
            FormClosed += SecondEachPickupByLPNForm_FormClosed;
            barConfirm.ItemClick += barConfirm_ItemClick; //确认下架
            _lstQuickAction.Add("F1", BarRestartClick); //F1重置
            _lstQuickAction.Add("F6", BarScanDetailClick); //F6扫描明细
            _lstQuickAction.Add("F2", BarConfirmClick); //F2确认下架
            _lstQuickAction.Add("F3", BtnExceptionRecordClick); //F3异常登记
            _lstQuickAction.Add("F8", StopPut); //F8中止投递
            jobMsgControl.BeforeFormShow += jobMsgControl_BeforeFormShow; //作业排名推送
            barbtnSet.ItemClick += barbtnSet_ItemClick; //设置
            barbtnChangeWall.ItemClick += barbtnChangeWall_ItemClick; //换墙
            btnChangeLpn.ItemClick += btnChangeLpn_ItemClick;//换箱
            txtSkuCodeAll.Click += txtSkuCodeAll_Click;

            MouseClick += SecondEachPickupByLPNForm_MouseClick;
            label1.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            label2.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            label3.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            label4.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            label5.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            label6.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            label7.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            label8.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            label9.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            label10.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labAlreadyOnWallQty.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labCompleteQty.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labCurCellNo.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labLackSkuCells.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labOutCancelCells.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labPickupOrder.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labSkuItem.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labSkuName.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labTotalPieceQty.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labTotalSoNo.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            labWaveGroupCode.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            txtCurPickingType.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            txtLpnCode.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            txtSkuCode.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            txtWaveGroupCode.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            panelControl1.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            panelControl2.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            tpOperator.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            panelControlShow.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            tabMain.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            tabMain.MouseLeave += tpOperator_Leave;
            MouseWheel += FormSample_MouseWheel;
            tabControl1.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            tabPage1.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            tabPage2.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            panelBatchAllPicking.MouseClick += SecondEachPickupByLPNForm_MouseClick;
        }

        void txtSkuCodeAll_Click(object sender, EventArgs e)
        {
            txtSkuCodeAll.SelectAll();
        }

        void btnChangeLpn_ItemClick(object sender, ItemClickEventArgs e)
        {
            BehaviorReturn lpnHBr = _wsNewLpnhService.SearchLpnHRelationOutbound(new MbNewLpnHRelationOutboundSo { whId = ClientService.CurrentWareId, lpnCode = txtLpnCode.Text.Trim(), pickupHeaderCode = txtWaveGroupCode.Text.Trim(), release = 0 });
            if (!lpnHBr.Success || !lpnHBr.hasEntity())
            {
                if (lpnHBr.hasMessage())
                {
                    SetSystemMessage(lpnHBr);
                }
                else
                {
                    MsgHelper.ShowOKForWarning("该单据不存在 或者 不能进行二次分拣");
                }
                return;
            }
            var curLpnHRelationOutbound = ((MbNewLpnHRelationOutboundInfo)lpnHBr.ObjectList[0]);
            var dicExistCellAndLpn = GetLPNAndCellInfo(curLpnHRelationOutbound.pickupHeaderId);

            var selectIndex = _curPickingType == PickingType.BatchAllPicking
                ? 0
                : _curAllCellAndSohId.Keys.TakeWhile(
                    key => _curCellIndex != 0 && key != _curCellIndex.ToString(CultureInfo.InvariantCulture)).Count();//获取当前扫描的格子在弹框中的行数
            var bindLpnAndCellMapDialog = new BindLPNAndCellMapDialog(curLpnHRelationOutbound.pickupHeaderId, dicExistCellAndLpn, _curAllCellAndSohId, true, selectIndex);
            bindLpnAndCellMapDialog.ShowDialog();
            if (bindLpnAndCellMapDialog.DialogResult == DialogResult.OK)
            {
                _dicLpnCellNo = bindLpnAndCellMapDialog.DicLpnCellNo;
            }
        }

        private void tpOperator_Leave(object sender, EventArgs e)
        {
            SecondEachPickupByLPNForm_MouseClick(null, null);
        }

        /// <summary>
        /// 鼠标点击后，将格子内容去掉
        /// </summary>
        private void SecondEachPickupByLPNForm_MouseClick(object sender, MouseEventArgs e)
        {
            panelControlShow.Controls.Clear();
            panelControlShow.Appearance.BackColor = Color.Transparent;
            panelControlShow.Visible = false;

            if (_curCellsControl != null)
            {
                _curCellsControl.Refresh();
            }
        }

        /// <summary>
        /// 显示格子信息
        /// </summary>
        private void _curCellsControl_ShowDatailCellEvent(Label label)
        {
            panelControlShow.Controls.Clear();
            panelControlShow.AutoScroll = false;
            PanelControl curPanelControl;
            if (_curPickingType == PickingType.BatchAllPicking)
            {
                curPanelControl = panelTwoTabPages;
            }
            else
            {
                curPanelControl = cellsControl;
            }
            #region 确定当前label是哪个格子
            if (label.Text.Length < 2)
                return;

            var cellArray = label.Text.Split(new char[] { ':', '：' });
            if (cellArray.Length < 2)
                return;
            var cellStr = cellArray[0].Replace("格", "");
            long cellIndex = 0;
            if (!long.TryParse(cellStr, out cellIndex))
                return;
            MbCellInfo cellInfo = null;
            foreach (var cell in _cellInfoLis)
            {
                if (cell.CellIndex == cellIndex)
                {
                    cellInfo = cell;
                    break;
                }
            }
            #endregion

            #region  确定该panel的 起始位置
            var leaveWidth = Width - (label.Location.X + curPanelControl.Location.X) - 10;
            var leftWidth = label.Location.X + curPanelControl.Location.X - 30;
            #endregion

            #region  根据label格子颜色来确定panel颜色
            if (label.BackColor == Color.Red)
            {
                panelControlShow.Appearance.BackColor = Color.FromArgb(255, 192, 192);  //Color.FromArgb(255, 255, 0, 0);
            }
            else if (label.BackColor == Color.Orange)
            {
                panelControlShow.Appearance.BackColor = Color.Wheat;//Color.FromArgb(255, 255, 165, 0);
            }
            else if (label.BackColor == Color.White)
            {
                panelControlShow.Appearance.BackColor = Color.FromArgb(224, 224, 224);
            }
            else if (label.BackColor == Color.Green)
            {
                panelControlShow.Appearance.BackColor = Color.FromArgb(192, 255, 192);
            }
            else
            {
                panelControlShow.Appearance.BackColor = Color.White;
            }
            panelControlShow.Visible = true;
            #endregion

            #region  增加显示格子号的控件
            UserControl preControl = null;  //记录最新增加的控件
            var cellNum = new SecondPickControl
            {
                label1 = { Text = "  格子" + cellIndex + "信息" },
                label2 = { Text = "" },
                Location = new Point(0, 0)
            };
            cellNum.label1.Font = new Font("宋体", 13F, FontStyle.Bold);
            cellNum.Size = new Size(cellNum.Width, cellNum.Height - 10);
            cellNum.Height = cellNum.Height - 10;
            preControl = cellNum;
            panelControlShow.Controls.Add(cellNum);
            #endregion

            #region  针对 只返回格子号 ，没有具体格子信息数据 的提示
            if (cellInfo == null)
            {
                var unPickNum = new SecondPickControl
                {
                    label1 = { Text = "   没有数据  " },
                    label2 = { Text = "" },
                    Location = new Point(preControl.Location.X, preControl.Location.Y + 40)
                };
                unPickNum.label1.Font = new Font("宋体", 13F, FontStyle.Regular);
                panelControlShow.Controls.Add(unPickNum);
                return;
            }
            #endregion

            #region  增加未投提示
            var unPick = new SecondPickControl
            {
                label1 = { Text = " 未投:" },
                label2 = { Text = "" },
                Location = new Point(preControl.Location.X, preControl.Location.Y + 30)
            };
            unPick.label1.Font = new Font("宋体", 11F, FontStyle.Regular);

            unPick.Size = new Size(unPick.Width, unPick.Height - 20);
            preControl = Tools.GetObject<SecondPickControl>(unPick);
            panelControlShow.Controls.Add(unPick);

            preControl.Location = new Point(preControl.Location.X, preControl.Location.Y - 7);

            #endregion

            var bigWidth = 0;
            int i = 0;
            var isFirstUnPick = true;
            #region  增加未投的格子信息
            foreach (CellSkuInfo skuInfo in cellInfo.CellAllSkuInfoList)
            {
                if (!(skuInfo.QtyAllocated > skuInfo.QtyAlreadyPickup))
                    continue;
                //未投数据
                var unPickCell = new SecondUnPickControl();
                if (!String.IsNullOrEmpty(skuInfo.SkuEanCode))
                {
                    unPickCell.label1.Text = "  货物条码";
                    unPickCell.label2.Text = skuInfo.SkuEanCode;
                }
                else
                {
                    unPickCell.label1.Text = "";
                    unPickCell.label2.Text = "";
                }

                unPickCell.label4.Text = skuInfo.SkuCode + "  ";
                unPickCell.label5.Text = (skuInfo.QtyAllocated - skuInfo.QtyAlreadyPickup) + "件";
                unPickCell.label6.Text = "  " + skuInfo.SkuName;

                unPickCell.label2.Location =
                        new Point(unPickCell.label1.Location.X + unPickCell.label1.Width - 7, 4);
                if (!String.IsNullOrEmpty(skuInfo.SkuEanCode))
                {
                    unPickCell.label3.Location = new Point(unPickCell.label2.Location.X + unPickCell.label2.Width - 5, 8);
                    unPickCell.label3.Text = "  货物代码 ";
                }
                else
                {
                    unPickCell.label3.Location = new Point(unPickCell.label2.Location.X + unPickCell.label2.Width + 6, 8);
                    unPickCell.label3.Text = "  货物代码 ";
                }
                unPickCell.label4.Location =
                    new Point(unPickCell.label3.Location.X + unPickCell.label3.Width - 13, 4);
                unPickCell.label5.Location =
                    new Point(unPickCell.label4.Location.X + unPickCell.label4.Width - 30, 4);
                unPickCell.Location = isFirstUnPick ? new Point(preControl.Location.X, preControl.Location.Y + 25) : new Point(preControl.Location.X, preControl.Location.Y + 40);
                isFirstUnPick = false;
                //unPickCell.label5.Text = "1件";
                var a = unPickCell.label5.Location.X + unPickCell.label5.Size.Width;

                if (a > bigWidth)
                {
                    bigWidth = a;
                }
                if (unPickCell.label6.Location.X + unPickCell.label6.Size.Width > bigWidth)
                {
                    bigWidth = unPickCell.label6.Location.X + unPickCell.label6.Size.Width;
                }
                //unPickCell.Size = new Size(a +5 > 460 ? a+5 : 460, unPick.Size.Height);
                //unPickCell.Width = bigWidth + 5 > Width / 3 + 100 ? bigWidth + 5 : Width / 3 + 100;
                unPickCell.Width = bigWidth + 50;
                //preControl = unPickCell;
                preControl = Tools.GetObject<SecondUnPickControl>(unPickCell);
                i++;
                panelControlShow.Controls.Add(unPickCell);
            }
            #endregion

            #region
            var pick = new SecondPickControl
            {
                label1 =
                {
                    Text = "———————————————————————————————————————————————————————————————————————————————"
                },
                label2 = { Text = " 已投:" },
                Location = new Point(preControl.Location.X, preControl.Location.Y + 40)
            };
            pick.label2.Font = new Font("宋体", 11F, FontStyle.Regular);
            preControl = pick;
            pick.Height = pick.Height - 5;
            panelControlShow.Controls.Add(pick);
            #endregion

            int num = 0;
            foreach (CellSkuInfo skuInfo in cellInfo.CellAllSkuInfoList)
            {
                if (!(skuInfo.QtyAllocated > skuInfo.QtyAlreadyPickup))
                    continue;
                num++;
            }
            bool isFristPcick = true;
            #region 增加已投格子信息
            foreach (CellSkuInfo skuInfo in cellInfo.CellAllSkuInfoList)
            {
                if (!(0 < skuInfo.QtyAlreadyPickup))
                    continue;
                //已投数据
                var pickCell = new SecondPickControl();
                string str = "";
                if (!String.IsNullOrEmpty(skuInfo.SkuEanCode))
                {
                    str = "   货物条码  " + skuInfo.SkuEanCode;
                }
                else
                {
                    str = "";
                }
                str += "   货物代码   " + skuInfo.SkuCode;
                str += "    " + skuInfo.QtyAlreadyPickup + " 件";
                pickCell.label1.Text = str;
                pickCell.label2.Text = "   " + skuInfo.SkuName;
                if (num == 1 || isFirstUnPick)
                {
                    pickCell.Location = isFristPcick ? new Point(preControl.Location.X, preControl.Location.Y + 30) : new Point(preControl.Location.X, preControl.Location.Y + 40);
                }
                else
                {
                    pickCell.Location = isFristPcick ? new Point(preControl.Location.X, preControl.Location.Y + 10) : new Point(preControl.Location.X, preControl.Location.Y + 40);
                }
                isFristPcick = false;
                var a = pickCell.label1.Location.X + pickCell.label1.Size.Width;
                if (a > bigWidth)
                {
                    bigWidth = a;
                }
                if (pickCell.label2.Location.X + pickCell.label2.Size.Width > bigWidth)
                {
                    bigWidth = pickCell.label2.Location.X + pickCell.label2.Size.Width;
                }
                //pickCell.Size = new Size(a + 5 > 460 ? a + 5 : 460, pickCell.Size.Height);
                //pickCell.Width = bigWidth + 5 > Width / 3 + 100 ? bigWidth + 5 : Width / 3 + 100;
                pickCell.Width = bigWidth + 50;
                //preControl = pickCell;
                preControl = Tools.GetObject<SecondPickControl>(pickCell);
                panelControlShow.Controls.Add(pickCell);
            }
            #endregion

            #region 修改panel位置

            var width = 0;
            bigWidth += 150;
            if (bigWidth > Width / 2)
            {
                width = bigWidth;
            }
            else if (bigWidth < 300)
            {
                width = 300;
            }
            else
            {
                width = bigWidth;
            }
            var leaveHeight = Height - (label.Location.Y + curPanelControl.Location.Y) - 100;
            panelControlShow.Size = new Size(width, preControl.Location.Y + 100);

            if (leaveWidth < width)
            {
                if (width < leftWidth)
                {
                    panelControlShow.Location = new Point(label.Location.X + label.Width - panelControlShow.Width + 30, label.Location.Y + curPanelControl.Location.Y + label.Height);
                }
                else
                {
                    panelControlShow.AutoScroll = true;
                    panelControlShow.Width = Width / 2;
                    if (panelControlShow.Width > leaveWidth)
                    {
                        panelControlShow.Location = new Point(label.Location.X + label.Width - panelControlShow.Width + 30, label.Location.Y + curPanelControl.Location.Y + label.Height);
                    }
                    else
                    {
                        panelControlShow.Location = new Point(label.Location.X + curPanelControl.Location.X,
                                                              label.Location.Y + curPanelControl.Location.Y + label.Height);
                    }
                }
            }
            else
            {
                panelControlShow.Location = new Point(label.Location.X + curPanelControl.Location.X, label.Location.Y + curPanelControl.Location.Y + label.Height);
            }
            if (_curPickingType == PickingType.BatchAllPicking)
            {
                panelControlShow.Location = new Point(panelControlShow.Location.X + 7, panelControlShow.Location.Y + 26);
            }
            var upHeight = label.Location.Y + curPanelControl.Location.Y;
            panelControlShow.Height = panelControlShow.Height + 10;
            if ((preControl.Location.Y + 100) >= leaveHeight)
            {
                if (upHeight >= preControl.Location.Y + 100)
                {
                    panelControlShow.Location = new Point(panelControlShow.Location.X, panelControlShow.Location.Y - panelControlShow.Size.Height - label.Height);
                }
                else
                {
                    panelControlShow.Height = leaveHeight;
                    panelControlShow.Width = panelControlShow.Width;
                    panelControlShow.AutoScroll = true;
                }
            }
            #endregion

            //A.SendToBack();置于底层
            //A.BringToFront(); 置于顶层
            panelControlShow.BringToFront();
            if (_curCellsControl != null)
            {
                _curCellsControl.Refresh();
            }
        }

        void FormSample_MouseWheel(object sender, MouseEventArgs e)
        {
            //滚动
            panelControlShow.AutoScrollPosition = new Point(0, panelControlShow.VerticalScroll.Value - e.Delta);
        }

        protected override void UIInitialize()
        {
            base.UIInitialize();
            HideMainTabPage();
            HideTopButton(true, true, true, true, true, true, true, true, true);
            barBtnSearch.Visibility = BarItemVisibility.Never;
            KeyPreview = true;
            barbtnSet.Enabled = true;
            barbtnStopPut.Enabled = true;
            IsControlEnableOrVisible();
            IsVisiableOutCancleCells(false);
            IsVisibleCurCellNo(true);
            txtSkuCode.Properties.ReadOnly = true;

            var pickingTypeValue = _iniFile.IniReadvalue("MbSecondEachPickupByLPNForm" + ClientService.CurrentWareId, "PickingType");
            SetPickingType(pickingTypeValue);

            _setupInfo = GetSetupFromConfig();
            ShowScanSkuInfo(_curPickingType != PickingType.BatchAllPicking);
            barbtnSet.Enabled = true;

            _progressBarHelper = new CommonProgressBarHelper(this);
            barbtnChangeWall.Enabled = false;
            btnChangeLpn.Enabled = false;
        }

        protected override void DataInitialize(string searchID)
        {
            _configInfoWhenOpen = GetOnWallUIConfigInfo();
            _preGroupCode = "";    //刷新的时候  需要清除信息
            _curWaveLpn = "";
            _curWall = "";
            _strRegex = _numberSetService.GetCheckSetRegx("LPN_CODE_NUMBER_MENU_CODE");
            if (curOperation == OperationType.Refresh)
            {
                ShowSacnWall();
                if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                {
                    SelfCheck();
                    Thread.Sleep(100);
                    ClearStatus();
                }
            }
            _cellInfoLis.Clear();
            _sohIdList.Clear();
            ClearInfo();
            barbtnSet.Enabled = true;
            txtLpnCode.Focus();
            _stopShowMsg = true;
            _stopErrorMsg = true;
            _stopReceiveMsg = true;
            _continueSearch = true;
            _waitPutSign = true;

            IsVisiableOutCancleCells(false);
        }

        private OnWallUIConfigInfo GetOnWallUIConfigInfo()
        {
            var br = configOutboundController.GetOnWallUIConfig();
            if (!br.hasEntity())
            {
                MsgHelper.ShowOKForWarning(br.hasMessage() ? br.MessageList[0].messageCN : "获取当前界面配置项失败");
                return new OnWallUIConfigInfo();
            }
            var config = br.ObjectList[0] as OnWallUIConfigInfo;
            return config;
        }

        protected override void AfterShown()
        {
            if (WMSUserConfig.Instance.STR_VideoCamera != "Y" && _configInfoWhenOpen.openVideoExtension)
            {
                var warningBtnDialog = new WarningBtnDialog("请在页面【客户习惯设置】，勾选“启用视频叠加功能”后再进行操作", "提示", "确定", true);
                warningBtnDialog.TopMost = true;
                warningBtnDialog.ShowDialog();
                IsShowConfirmWhenClose = false;
                Close();
                return;
            }
            if (WMSUserConfig.Instance.STR_VideoCamera == "Y")   //启用视频录制功能
            {
                VideoCameraHelper.Instance.Connect("二次分拣");
            }

            ShowSacnWall();
        }

        /// <summary>
        /// 计时器初始化
        /// </summary>
        private void InitTimer()
        {
            //if (_sysConfige.SysCon_WC011)
            //{
            //    _jobTimer.Tick += JobTimer_Tick;
            //    _jobTimer.Interval = 3 * 60 * 1000;
            //    _jobTimer.Start();
            //    JobTimer_Tick(null, null);

            //    _showTimer.Tick += ShowAlterTimer_Tick;
            //    _showTimer.Interval = 10 * 1000;
            //    _showTimer.Start();
            //}

            _searchPickUpTimer.Interval = 150;
            _searchPickUpTimer.Tick += SearchReturnData;


            //SecondEachPickUp.Instance.DataReceivedEvent += svc_DataReceived;
        }

        #region  设置
        /// <summary>
        /// 设置
        /// </summary>
        private void barbtnSet_ItemClick(object sender, ItemClickEventArgs e)
        {
            var defaultSetupInfo = GetDefaultSetupConfig();
            var setupDialog = new SetupDialog(defaultSetupInfo, false);

            setupDialog.ShowDialog(this);

            _setupInfo = GetSetupFromConfig();
            ShowScanSkuInfo(_curPickingType != PickingType.BatchAllPicking);
            if (_curCellsControl != null)
            {
                if (_curPickingType != PickingType.BatchAllPicking)
                {
                    panelTwoTabPages.Visible = false;
                    panelBatchAllPicking.Controls.Clear();
                    cellsControl.Controls.Add(_curCellsControl);
                    labCurCellNo.Visible = true;
                    labCompleteQty.Visible = true;
                    if (CollectionHelper.CollectionNotEmpty(_cellInfoLis))
                    {
                        SetPanelCell(_cellInfoLis);
                    }
                    txtSkuCodeAll.Text = null;
                    labSkuNameAll.Text = null;
                    lblCellUom.Text = null;
                    lblTatol.Text = null;
                    if (_curPickingType != PickingType.BatchBySoPicking)
                        panelControl1.Visible = true;

                }
                else
                {
                    panelTwoTabPages.Visible = true;
                    cellsControl.Controls.Clear();
                    panelBatchAllPicking.Controls.Add(_curCellsControl);
                    labCurCellNo.Visible = false;
                    labCompleteQty.Visible = false;
                    panelControl1.Visible = false;
                }
            }
        }

        /// <summary>
        /// 默认设置
        /// </summary>
        private SetupInfo GetDefaultSetupConfig()
        {
            barbtnStopPut.Enabled = false;
            return new SetupInfo
            {
                ScanType = (int)ScanTypeEnum.NoScanSo, //不需要扫描so
            };
        }

        /// <summary>
        /// 读取设置配置文件
        /// </summary>
        private SetupInfo GetSetupFromConfig()
        {
            if (!File.Exists(SetUpConfig1.ConfigPath))
            {
                ShowControl(false);
                return GetDefaultSetupConfig();
            }

            var ds = new DataSet();
            ds.ReadXml(SetUpConfig1.ConfigPath);
            var dt = ds.Tables[SetUpConfig1.ConfigTableName];

            var setupList = EnumDataSourceConvert.SetDataTableToList<SetupInfo>(dt);
            if (setupList.Count == 0)
                return new SetupInfo();
            if (setupList[0].ScanType == (int)ScanTypeEnum.ScanSO)
            {
                _curPickingType = PickingType.PiecePicking;
                txtCurPickingType.Text = @"当前分拣模式：逐件分拣";
                barbtnStopPut.Enabled = true;
                _iniFile.IniWritevalue("MbSecondEachPickupByLPNForm" + ClientService.CurrentWareId, "PickingType", "PiecePicking");
            }
            ShowControl(setupList[0].ScanType == (int)ScanTypeEnum.ScanSO);
            return setupList[0];
        }

        /// <summary>
        /// 是否支持扫描so装箱单 （光栅）
        /// </summary>
        private void ShowControl(bool isScanSo)
        {
            if (isScanSo)
            {
                labAlreadySo.Visible = true;
                labTotalSo.Visible = true;
                labelControl3.Visible = true;
                labelControl2.Visible = true;
                labelControl6.Location = new Point(659, 48);
                labelControl7.Location = new Point(882, 48);
                labAlreadyOnWallQty.Location = new Point(928, 0);
                labTotalPieceQty.Location = new Point(729, 0);
                panelControl1.Location = new Point(648, 176);
                label5.Text = "SO单号/货物条码:";

                label17.Visible = true;
                label18.Visible = true;
                TestConnect();
                _searchPickUpTimer.Start();
                SecondEachPickUp.Instance.ReceiveSuccess = true;
                SelfCheck();
                Thread.Sleep(100);
                ClearStatus();
            }
            else
            {
                labAlreadySo.Visible = false;
                labTotalSo.Visible = false;
                labelControl3.Visible = false;
                labelControl2.Visible = false;
                labelControl6.Location = new Point(659, 80);
                labelControl7.Location = new Point(882, 80);
                labAlreadyOnWallQty.Location = new Point(928, 30);
                labTotalPieceQty.Location = new Point(729, 30);
                panelControl1.Location = new Point(648, 156);
                label5.Text = "           货物条码:";

                label17.Visible = false;
                label18.Visible = false;
                _searchPickUpTimer.Stop();
                SecondEachPickUp.Instance.Dispose();
                if (_curCellsControl != null)
                {
                    _curCellsControl.ShowSoListCellEvent -= _curCellsControl_ShowSoListCellEvent;
                }
            }
            panelControl1.Refresh();
            txtLpnCode.Focus();
            txtLpnCode.SelectAll();
        }

        #endregion

        /// <summary>
        /// 换墙
        /// </summary>
        private void barbtnChangeWall_ItemClick(object sender, ItemClickEventArgs e)
        {
            ClearSystemMessage();
            if (string.IsNullOrEmpty(labWaveGroupCode.Text) || (!string.IsNullOrEmpty(labAlreadyOnWallQty.Text) && labAlreadyOnWallQty.Text.Trim() != "0"))
            {
                if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                {
                    if (!string.IsNullOrEmpty(labAlreadySo.Text) && labAlreadySo.Text.Trim() != "0")
                        return;
                }
                return;
            }
            var br = _locationService.GetUnBindedWall();
            if (!br.Success)
            {
                SetSystemMessage(br);
                return;
            }
            var changeWallDia = new ChangeWallDia(br.ObjectList, labWaveGroupCode.Text, _allDWorkInfoList[0].pkhId);
            if (changeWallDia.ShowDialog() == DialogResult.OK)
            {
                _curWall = changeWallDia.CurWall;
                _curWaveLpn = changeWallDia.CurWall;
                lbWallLoc.Text = _curWall;
            }
        }

        private void IsControlEnableOrVisible()
        {
            txtCurPickingType.Location = new Point(SystemInformation.WorkingArea.Width - 250, 3);
            palExplain.Location = new Point(SystemInformation.WorkingArea.Width - 150, SystemInformation.WorkingArea.Height - 250);
            label1.Location = new Point(32, 178);
            label2.Location = new Point(32, 233);
            label19.Location = new Point(32, 288);
            labOutCancelCells.Location = new Point(112, 178);
            labOutCancelCells.Width = 550;
            labLackSkuCells.Location = new Point(112, 233);
            labLackSkuCells.Width = 550;
            labLackSoCells.Location = new Point(112, 288);
            labLackSoCells.Width = 550;
            label1.Visible = false;
            label2.Visible = false;
            label19.Visible = false;
            labOutCancelCells.Visible = false;
            labLackSkuCells.Visible = false;
            labLackSoCells.Visible = false;
            lkLabModifySkuQty.Visible = false;
            txtLpnCode.Properties.ReadOnly = false;
            txtSkuCode.Properties.ReadOnly = false;
            txtWaveGroupCode.Properties.ReadOnly = false;
        }

        /// <summary>
        /// 显示墙角库位
        /// </summary>
        private bool ShowSacnWall()
        {
            var wallDia = new ScanWallDia();
            if (wallDia.ShowDialog() == DialogResult.OK)
            {
                _curWall = wallDia.CurWall;
                lbWallLoc.Text = _curWall;
                ClearSystemMessage();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 扫描
        /// </summary>
        protected override void Control_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Tools.ScanShortcutCode(sender, e.KeyChar, ref _inputCharBuff, ref _activeControlValue, _lstQuickAction))
            {
                return;
            }

            if (e.KeyChar == '\r')
            {
                SecondEachPickupByLPNForm_MouseClick(null, null);
                IsVisiableOutCancleCells(false);
                ClearSystemMessage();
                if (sender is TextEdit && (sender as TextEdit).Name == "txtLpnCode")
                {
                    if (string.IsNullOrEmpty(txtLpnCode.Text))
                    {
                        MsgHelper.ShowOKForWarning("LPN号不能为空");
                        return;
                    }
                    if (!CheckLpnCode(txtLpnCode.Text.Trim()))
                    {
                        MsgHelper.ShowOKForWarning("LPN号不符合规则，请重新扫!");
                        txtLpnCode.Text = null;
                        return;
                    }
                    ClearPreScan();

                    //初始化分拣柜 (各灯熄灭，检查光栅是否正常工作。)
                    if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                    {
                        SecondEachPickUp.Instance.ReceiveSuccess = true;
                        _stopShowMsg = true;
                        _waitPutSign = true;
                        ClearStatus();
                    }

                    if (!IsFalseLPNCode())
                    {
                        if (!LPNCodeEnter())
                            return;
                        SkuCodeFocus();
                        barbtnSet.Enabled = false;
                        return;
                    }
                    if (!WaveGroupInfoByLPNCode())
                        return;
                    SkuCodeFocus();
                    barbtnSet.Enabled = false;
                }

                if (sender is TextEdit && (sender as TextEdit).Name == "txtWaveGroupCode")
                {
                    _preLpndSo = new MbNewLpndSalesOrderInfo();
                    ClearPreScan();

                    //初始化分拣柜 (各灯熄灭，检查光栅是否正常工作。)
                    if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                    {
                        SecondEachPickUp.Instance.ReceiveSuccess = true;
                        _stopShowMsg = true;
                        _waitPutSign = true;
                        ClearStatus();
                    }
                    if (WaveGroupCodeEnter())
                    {
                        SkuCodeFocus();
                        barbtnSet.Enabled = false;
                    }
                }

                if (sender is TextEdit && ((sender as TextEdit).Name == "txtSkuCode" || (sender as TextEdit).Name == "txtSkuCodeAll"))
                {
                    var txt = sender as TextEdit;
                    _scanSkuCode = txt.Text.Trim();
                    if (txtSkuCode.Properties.ReadOnly)
                        return;

                    SkuCodeEnter(); //可能扫描SKUCode或SKUEan
                    SkuCodeFocus();
                }
            }
        }

        #region   清除上一枪扫描明细显示
        private void ClearPreScan()
        {
            _prePickNum = 0;
            _preIsSo = false;
            _preLpndSo = new MbNewLpndSalesOrderInfo();
            _lastScanSkuCode.Clear();
            _secondRichText.richTextBox1.Text = "";
            panelControl1.Controls.Clear();
        }
        #endregion

        #region 扫描LPN 或 分组号

        /// <summary>
        /// 扫描LPN号是否为假的。 _isLpnFake= true,说明LPN号为假的
        /// </summary>
        private bool IsFalseLPNCode()
        {
            txtWaveGroupCode.Text = null;
            var brIsLpnFake = _newWorkOrderBusinessService.IsLpnFake(txtLpnCode.Text.Trim());
            if (!brIsLpnFake.Success)
            {
                SetSystemMessage(brIsLpnFake);
                return false;
            }
            _isLpnFake = brIsLpnFake.udf2;
            return _isLpnFake;
        }

        /// <summary>
        /// 扫描LPN号，但实际上走的是扫分组号的流程
        /// </summary>
        private bool WaveGroupInfoByLPNCode()
        {
            if (GetServiceInfo(txtLpnCode.Text.Trim(), null, LoadType.WaveCode))
            {
                InitShowInfoByLPNOrWave(_allDWorkInfoList, LoadType.WaveCode);
                if (WMSUserConfig.Instance.STR_VideoCamera == "Y")
                {
                    VideoCameraHelper.Instance.VideoBegin(labWaveGroupCode.Text + ".avi", GetVideoExtensionTime());
                }
                IsEnableButton(true);
                _curType = LoadType.WaveCode;
                return true;
            }
            ClearInfo();
            return false;
        }

        /// <summary>
        /// 扫描波次分组号
        /// </summary>
        private bool WaveGroupCodeEnter()
        {
            txtLpnCode.Text = null;
            if (string.IsNullOrEmpty(txtWaveGroupCode.Text))
            {
                MsgHelper.ShowOKForWarning("波次分组不能为空");
                return false;
            }
            if (GetServiceInfo(null, txtWaveGroupCode.Text.Trim(), LoadType.WaveCode))
            {
                InitShowInfoByLPNOrWave(_allDWorkInfoList, LoadType.WaveCode);
                if (WMSUserConfig.Instance.STR_VideoCamera == "Y")
                {
                    VideoCameraHelper.Instance.VideoBegin(labWaveGroupCode.Text + ".avi", GetVideoExtensionTime());
                }
                IsEnableButton(true);
                _curType = LoadType.WaveCode;
                return true;
            }
            ClearInfo();
            return false;
        }

        /// <summary>
        /// 扫描LPN号
        /// </summary>
        private bool LPNCodeEnter()
        {
            txtWaveGroupCode.Text = null;
            if (string.IsNullOrEmpty(txtLpnCode.Text))
            {
                MsgHelper.ShowOKForWarning("LPN号不能为空");
                return false;
            }
            if (GetServiceInfo(txtLpnCode.Text.Trim(), null, LoadType.LPNCode))
            {
                InitShowInfoByLPNOrWave(_curLPNDWorkInfoList, LoadType.LPNCode);
                if (WMSUserConfig.Instance.STR_VideoCamera == "Y")
                {
                    VideoCameraHelper.Instance.VideoBegin(labWaveGroupCode.Text + ".avi", GetVideoExtensionTime());
                }
                IsEnableButton(true);
                _curType = LoadType.LPNCode;
                return true;
            }
            ClearInfo();
            return false;
        }

        private bool _isFirstWall;
        private bool GetServiceInfo(string lpnCode, string waveCode, LoadType loadType)
        {
            if (string.IsNullOrEmpty(_curWall))
            {
                MsgHelper.ShowOKForWarning("请先选择集货墙");
                return false;
            }
            MbNewLpnHRelationOutboundInfo curLpnHRelationOutbound = GetCurConfigAndLpnHRelationOutbound(lpnCode, waveCode);
            if (curLpnHRelationOutbound == null || _bizConfigInfo == null)
                return false;
            _isFirstWall = false;

            //使用一个LPN配置的
            if (StringHelper.EqualIgnoreCase(GetPickUpRetainLpnEnumStr(), PickUpRetainLpnEnum.ONE_LPN.ToString()))
            {
                if (!ScanLpnCodeWhenValueIs1(curLpnHRelationOutbound, lpnCode, waveCode))
                    return false;
            }
            if (!FetchPutonWall(curLpnHRelationOutbound.pickupHeaderCode, loadType))
                return false;
            if (_isFirstWall && !CreatWallLpnForFirst(curLpnHRelationOutbound.pickupHeaderId))
                return false;
            var br = _newWorkOrderBusinessService.GetMbLpnDPkdByLpnCodeAndPkhCode(lpnCode, waveCode); //返回所对应分组号下的全部 woi
            if (!br.Success || !br.hasEntity())
            {
                SetSystemMessage(br);
                return false;
            }

            _sohIdList = GetPutWallSohIdList(br);

            _checkConfigValueInfo = GetCheckConfigValueInfo();
            if (_checkConfigValueInfo == null)
                return false;

            _allDWorkInfoList = GetAllDworkInfoList(br);
            _cellInfoLis = GetCellInfo(br.udf3, loadType);
            var sortByCellIndex = new CellListSortByIndexm();
            _cellInfoLis.Sort(sortByCellIndex);
            if (_curPickingType == PickingType.BatchAllPicking)
            {
                if (_cellInfoLis.Count > 60)
                {
                    MsgHelper.ShowOKForWarning("当前扫描频率下格子号超过60个，无法集货");
                    return false;
                }
            }
            else
            {
                if (_cellInfoLis.Count > 200)
                {
                    MsgHelper.ShowOKForWarning("格子号超过200个，无法集货");
                    return false;
                }
            }
            //一个分货格一个LPN的
            if (StringHelper.EqualIgnoreCase(GetPickUpRetainLpnEnumStr(), PickUpRetainLpnEnum.ONE_SO_ONE_LPN.ToString()))
            {
                if (!ScanLpnCodeWhenValueIs2(curLpnHRelationOutbound.pickupHeaderId, curLpnHRelationOutbound.pickupHeaderCode))
                    return false;
            }
            InitBarCodeRule(_allDWorkInfoList);
            _curCellsControl = GetCurrentCellsControl(_cellInfoLis[_cellInfoLis.Count - 1].CellIndex);
            _curCellsControl.ShowDatailCellEvent += _curCellsControl_ShowDatailCellEvent;
            if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
            {
                _curCellsControl.ShowSoListCellEvent += _curCellsControl_ShowSoListCellEvent;
            }
            _curCellsControl.MouseClick += SecondEachPickupByLPNForm_MouseClick;
            _curCellsControl.InitCellColor(_cellInfoLis, GetCellOutCancleList(br.udf3));
            txtSkuCodeAll.Properties.ReadOnly = false;
            SetPanelCell(_cellInfoLis);
            if (_curPickingType == PickingType.BatchAllPicking)
            {
                ShowScanSkuInfo(false);
            }
            else
            {
                ShowScanSkuInfo(true);
            }
            GetScanDetails(br);   //不需要再获取一次服务，直接在上一次返回对象上处理

            return true;
        }

        private void ShowScanSkuInfo(bool isVisible)
        {
            label5.Visible = isVisible;
            label6.Visible = isVisible;
            txtSkuCode.Visible = isVisible;
            labSkuName.Visible = isVisible;

            panelControl1.Visible = isVisible;
        }

        private void SetPanelCell(IEnumerable<MbCellInfo> cellInfoLis)
        {
            MakePanelHide();
            foreach (var cellInfo in cellInfoLis)
            {
                var panel = (panelControl3.Controls.Find("panel" + cellInfo.CellIndex, false)[0]) as Panel;
                if (panel != null)
                {
                    panel.Controls.Clear();
                    var label1 = new Label
                    {
                        Text = @"格",
                        Location = new Point(3, 5),
                        Font = new Font("Tahoma", 12),
                        ForeColor = Color.FromArgb(161, 161, 161),
                        Size = new Size(20, 20)
                    };
                    var label2 = new Label
                    {
                        Text = cellInfo.CellIndex.ToString(CultureInfo.InvariantCulture),
                        Location = new Point(23, 5),
                        Font = new Font("Tahoma", 12, FontStyle.Bold),
                        ForeColor = Color.FromArgb(161, 161, 161),
                        Size = new Size(cellInfo.CellIndex.ToString(CultureInfo.InvariantCulture).Length * 20, 20)
                    };

                    panel.Controls.Add(label2);
                    panel.Controls.Add(label1);
                    panel.Visible = true;
                    panel.BackColor = Color.FromArgb(215, 215, 215);
                    panel.Tag = false;
                    panel.Paint += panel_Paint;
                }
            }
        }

        private void panel_Paint(object sender, PaintEventArgs e)
        {
            var panel = sender as Panel;
            if (panel != null)
            {
                if ((bool)panel.Tag)
                {
                    ControlPaint.DrawBorder(e.Graphics,
                        panel.ClientRectangle,
                        Color.FromArgb(22, 155, 213),
                        3,
                        ButtonBorderStyle.Solid,
                        Color.FromArgb(22, 155, 213),
                        3,
                        ButtonBorderStyle.Solid,
                        Color.FromArgb(22, 155, 213),
                        3,
                        ButtonBorderStyle.Solid,
                        Color.FromArgb(22, 155, 213),
                        3,
                        ButtonBorderStyle.Solid);
                }
                else
                {
                    ControlPaint.DrawBorder(e.Graphics,
                        panel.ClientRectangle,
                        Color.FromArgb(215, 215, 215),
                        3,
                        ButtonBorderStyle.Solid,
                        Color.FromArgb(215, 215, 215),
                        3,
                        ButtonBorderStyle.Solid,
                        Color.FromArgb(215, 215, 215),
                        3,
                        ButtonBorderStyle.Solid,
                        Color.FromArgb(215, 215, 215),
                        3,
                        ButtonBorderStyle.Solid);
                }
            }
        }

        private void MakePanelHide()
        {
            var controls = panelControl3.Controls;
            foreach (var coltrol in controls)
            {
                var panel = coltrol as Panel;
                if (panel != null)
                {
                    panel.Visible = false;
                }
            }
        }

        /// <summary>
        /// 初始化条码规则
        /// </summary>
        private void InitBarCodeRule(IEnumerable<MbNewLpndSalesOrderInfo> allDWorkInfoList)
        {
            var manufacturerIdList = new List<long>();
            foreach (var obj in allDWorkInfoList)
            {
                if (!manufacturerIdList.Contains(obj.manufacturerId))
                {
                    manufacturerIdList.Add(obj.manufacturerId);
                }
            }
            _curBarRules = new MbBarCodeRules_SecondPickup(manufacturerIdList);
        }

        /// <summary>
        /// 先获取配置项
        /// </summary>
        private MbNewLpnHRelationOutboundInfo GetCurConfigAndLpnHRelationOutbound(string lpnCode, string waveCode)
        {
            //releaseList = new long?[] { 0 } ,    先去掉，没有进行二次分拣的去验货后还是 希望在二次分拣页面加载出来
            BehaviorReturn lpnHBr = _wsNewLpnhService.SearchLpnHRelationOutbound(new MbNewLpnHRelationOutboundSo { whId = ClientService.CurrentWareId, lpnCode = lpnCode, pickupHeaderCode = waveCode, release = 0 });
            if (!lpnHBr.Success || !lpnHBr.hasEntity())
            {
                if (lpnHBr.hasMessage())
                {
                    SetSystemMessage(lpnHBr);
                }
                else
                {
                    MsgHelper.ShowOKForWarning("该单据不存在 或者 不能进行二次分拣");
                }
                return null;
            }
            var curLpnHRelationOutbound = ((MbNewLpnHRelationOutboundInfo)lpnHBr.ObjectList[0]);

            //获取配置项
            _bizConfigInfo = GetOnWallBizConfig(curLpnHRelationOutbound.pickupHeaderId);

            if (_bizConfigInfo == null)
                return null;

            return curLpnHRelationOutbound;
        }

        /// <summary>
        /// 配置一个分组一个LPN时 扫描LPN号
        /// </summary>
        private bool ScanLpnCodeWhenValueIs1(MbNewLpnHRelationOutboundInfo curLpnHRelationOutbound, string lpnCode, string waveCode)
        {
            if (string.IsNullOrEmpty(_curWaveLpn) && !string.IsNullOrEmpty(_curWall)) //如果_curWaveLpn没值，说明是第一次，第一次分组的LPN=墙角库位
            {
                _isFirstWall = true;
                _curWaveLpn = _curWall;
                return true;
            }
            if (_preGroupCode == curLpnHRelationOutbound.pickupHeaderCode) //如果下一个是相同的分组号，则LPN号不变
                return true;

            var scanPickupCarDia = new ScanPickupCarDia(curLpnHRelationOutbound.pickupHeaderId, lpnCode, waveCode, _setupInfo.ScanType == (int)ScanTypeEnum.ScanSO);
            scanPickupCarDia.ShowDialog();
            if (scanPickupCarDia.DialogResult == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(scanPickupCarDia.LPNCode))
                    return false;
                _curWaveLpn = scanPickupCarDia.LPNCode;
                _curWall = scanPickupCarDia.LPNCode; //配置一个LPN时,如果不等于前一次的分组，弹出toLPN输入框，扫描1个可用LPN（同时更新墙角库位的值等于该LPN号）
                lbWallLoc.Text = _curWall;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 配置一个分货格一个LPN时 扫描LPN号
        /// </summary>
        private bool ScanLpnCodeWhenValueIs2(long pickupHeaderId, string pickupHeaderCode)
        {
            if (_preGroupCode == pickupHeaderCode) //如果下一个是相同的分组号，则不需要弹出绑定框
                return true;

            var dicExistCellAndLpn = GetLPNAndCellInfo(pickupHeaderId);
            if (dicExistCellAndLpn != null && dicExistCellAndLpn.Count == _curAllCellAndSohId.Count && !_isFrist)
                return true;

            var bindLPNAndCellMapDialog = new BindLPNAndCellMapDialog(pickupHeaderId, dicExistCellAndLpn, _curAllCellAndSohId);
            bindLPNAndCellMapDialog.ShowDialog();
            if (bindLPNAndCellMapDialog.DialogResult == DialogResult.OK)
            {
                _dicLpnCellNo = bindLPNAndCellMapDialog.DicLpnCellNo;
                _isFrist = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 上墙领用
        /// </summary>
        private bool FetchPutonWall(string pkhCode, LoadType loadType)
        {
            //是否使用一个LPN
            var locationCode = StringHelper.EqualIgnoreCase(GetPickUpRetainLpnEnumStr(), PickUpRetainLpnEnum.ONE_LPN.ToString()) ? _curWaveLpn : _curWall;
            return RebinPrepareWhenRebin(txtLpnCode.Text.Trim(), pkhCode, locationCode, ClientService.CurrentUserInfo.code);
        }

        /// <summary>
        /// 第一次使用绑定集货墙
        /// 第一次分组的LPN=墙角库位
        /// </summary>
        private bool CreatWallLpnForFirst(long pickupHeaderId)
        {
            BehaviorReturn br = _newWorkOrderBusinessService.CreateOnWallRelationOutbound(pickupHeaderId, null, new[] { _curWaveLpn.Trim() }); //创建绑定关系
            if (!br.Success)
            {
                SetSystemMessage(br);
                return false;
            }
            return true;
        }

        /// <summary>
        /// lpn、分组，进行上墙领用
        /// </summary>
        private bool RebinPrepareWhenRebin(string lpnCode, string pkhCode, string locationCode, string fetchPersons)
        {
            BehaviorReturn br = _newWorkOrderBusinessService.RebinPrepareWhenRebin(lpnCode, pkhCode, locationCode, fetchPersons,
                HashTableHelper.CacheInstance.GetCodeInfoIdByCodeClassCodeAndCodeInfoCode(CodeConcentrationCamp.DIST_OPER_TYPE, _setupInfo.ScanType == (int)ScanTypeEnum.ScanSO ? CodeConcentrationCamp.LASER_DIST : CodeConcentrationCamp.COMMON_DIST));
            if (br.Success || br.hasMessage() && (br.MessageList[0].code == "ERR_PKH_NO_SKU_CONSUM" || br.MessageList[0].code == "ERR_LPN_FETCH_YET"
                                                  || br.MessageList[0].code == "ERR_PKH_FETCH_YET" || br.MessageList[0].code == "ERR_PKH_PUTONWALL_FETCH_YET"))
            {
                return true;
            }
            SetSystemMessage(br);
            return false;
        }

        /// <summary>
        /// 获取存在后台 LPN号和对应的格子信息
        /// </summary>
        private Dictionary<string, string> GetLPNAndCellInfo(long pkhId)
        {
            _dicExistCellAndSohId.Clear();
            var dicExistCellAndLpn = new Dictionary<string, string>();
            var newLpnHRelationOutboundSo = new MbNewLpnHRelationOutboundSo
            {
                releaseList = new long?[] { 0, 1 },  //现在改为0和1
                bizTypeCode = "LPN_BIZ_ONWALL",
                pickupHeaderId = pkhId,
                onCell = true
            };
            BehaviorReturn br = _wsNewLpnhService.SearchLpnHRelationOutbound(newLpnHRelationOutboundSo);
            if (!br.Success)
                return null;
            foreach (MbNewLpnHRelationOutboundInfo vo in br.ObjectList)
            {
                var cellNo = (from lpnd in _allDWorkInfoList where lpnd.sohId == vo.sohId select lpnd.cellIndex.ToString(CultureInfo.InvariantCulture)).FirstOrDefault();
                if (cellNo != null && !dicExistCellAndLpn.ContainsKey(cellNo) && _curAllCellAndSohId.ContainsKey(cellNo)) //剔除掉保存在后台(状态为已完成)，但不在需要显示的格子号
                {
                    dicExistCellAndLpn.Add(cellNo, vo.lpnCode);
                    if (!_dicExistCellAndSohId.ContainsKey(cellNo))
                    {
                        _dicExistCellAndSohId.Add(cellNo, new List<long> { vo.sohId });
                    }
                    else if (!_dicExistCellAndSohId[cellNo].Contains(vo.sohId))
                    {
                        _dicExistCellAndSohId[cellNo].Add(vo.sohId);
                    }
                }
            }
            return dicExistCellAndLpn;
        }

        /// <summary>
        /// 获取每个格子的信息
        /// </summary>
        private List<MbCellInfo> GetCellInfo(string strOutCancle, LoadType loadType)
        {
            var outCancleCellList = GetCellOutCancleList(strOutCancle);
            var dicIdCellInfoList = new Dictionary<long, MbCellInfo>();
            _curLPNDWorkInfoList.Clear();
            _curAllCellAndSohId.Clear();
            foreach (var lpnDWorkOrderInfo in _allDWorkInfoList)
            {
                if (!string.IsNullOrEmpty(lpnDWorkOrderInfo.lpnCode) && !string.IsNullOrEmpty(txtLpnCode.Text.Trim())
                    && lpnDWorkOrderInfo.lpnCode.ToUpper() == txtLpnCode.Text.Trim().ToUpper() && loadType == LoadType.LPNCode && !lpnDWorkOrderInfo.fake)
                {
                    _curLPNDWorkInfoList.Add(lpnDWorkOrderInfo);
                }

                if ((lpnDWorkOrderInfo.udf1 == "inPutting" || lpnDWorkOrderInfo.statusCode != CodeConcentrationCamp.LPNDSO_FINISHED))
                {
                    //完成状态不需要
                    var cellNo = lpnDWorkOrderInfo.cellIndex.ToString(CultureInfo.InvariantCulture);
                    if (!_curAllCellAndSohId.ContainsKey(cellNo))
                    {
                        _curAllCellAndSohId.Add(cellNo, new List<long> { lpnDWorkOrderInfo.sohId });
                    }
                    else if (!_curAllCellAndSohId[cellNo].Contains(lpnDWorkOrderInfo.sohId))
                    {
                        _curAllCellAndSohId[cellNo].Add(lpnDWorkOrderInfo.sohId);
                    }

                }
                if (dicIdCellInfoList.ContainsKey(lpnDWorkOrderInfo.cellIndex))
                {
                    ContinueAddCellInfo(lpnDWorkOrderInfo, dicIdCellInfoList, loadType);
                }
                else
                {
                    AddCellInfo(lpnDWorkOrderInfo, outCancleCellList, dicIdCellInfoList, loadType);
                }
            }
            return dicIdCellInfoList.Values.ToList();
        }

        /// <summary>
        /// 新增格子号信息
        /// </summary>
        private void AddCellInfo(MbNewLpndSalesOrderInfo lpnDWorkOrderInfo, List<long> outCancleCellList, Dictionary<long, MbCellInfo> dicIdCellInfoList, LoadType loadType)
        {
            //需要new两个对象，防止后面的操作会相互影响
            var cellAllSkuInfo = new CellSkuInfo
            {
                SkuCode = lpnDWorkOrderInfo.skuCode,
                SkuEanCode = lpnDWorkOrderInfo.skuEanCode,
                SkuName = lpnDWorkOrderInfo.skuName,
                QtyAlreadyPickup = lpnDWorkOrderInfo.qtyOutputUom,
                QtyAllocated = lpnDWorkOrderInfo.qtyUom
            };

            var cellValidSkuInfoByLPN = new CellSkuInfo
            {
                SkuCode = lpnDWorkOrderInfo.skuCode,
                SkuEanCode = lpnDWorkOrderInfo.skuEanCode,
                SkuName = lpnDWorkOrderInfo.skuName,
                QtyAlreadyPickup = lpnDWorkOrderInfo.qtyOutputUom,
                QtyAllocated = lpnDWorkOrderInfo.qtyUom
            };

            var cellInfo = new MbCellInfo //格子信息
            {
                CellIndex = lpnDWorkOrderInfo.cellIndex,
                QtyAllocatedUom = lpnDWorkOrderInfo.qtyUom,
                QtyOutputboxUom = lpnDWorkOrderInfo.qtyOutputUom,
                AlreadyScanSoCode = lpnDWorkOrderInfo.isPackSheetPut,
                CellSohId = lpnDWorkOrderInfo.sohId,
                CellAllSkuInfoList = new List<CellSkuInfo> { cellAllSkuInfo }
            };

            //扫描LPN加载，将属于改LPN的DWork id !=0 的记录下来
            if (loadType == LoadType.LPNCode && lpnDWorkOrderInfo.id != 0 && !string.IsNullOrEmpty(lpnDWorkOrderInfo.lpnCode) && !string.IsNullOrEmpty(txtLpnCode.Text) && lpnDWorkOrderInfo.lpnCode.ToUpper() == txtLpnCode.Text.Trim().ToUpper())
            {
                cellInfo.CellValidSkuInfoListByLPN = new List<CellSkuInfo> { cellValidSkuInfoByLPN };
            }
            else
            {
                cellInfo.CellValidSkuInfoListByLPN = new List<CellSkuInfo>();
            }
            if (lpnDWorkOrderInfo.wasted || lpnDWorkOrderInfo.udf3 == Yellow || outCancleCellList.Contains(cellInfo.CellIndex)) //是否是外部取消的格子
            {
                cellInfo.IsOutCancle = true;
            }
            dicIdCellInfoList.Add(lpnDWorkOrderInfo.cellIndex, cellInfo);
        }

        /// <summary>
        /// 继续增加格子号信息
        /// </summary>
        private void ContinueAddCellInfo(MbNewLpndSalesOrderInfo lpnDWorkOrderInfo, Dictionary<long, MbCellInfo> dicIdCellInfoList, LoadType loadType)
        {
            dicIdCellInfoList[lpnDWorkOrderInfo.cellIndex].QtyAllocatedUom += lpnDWorkOrderInfo.qtyUom;  //该格子总量累加
            dicIdCellInfoList[lpnDWorkOrderInfo.cellIndex].QtyOutputboxUom += lpnDWorkOrderInfo.qtyOutputUom;//已上架量累加

            bool isFindInAll = false;
            foreach (var skuInfo in dicIdCellInfoList[lpnDWorkOrderInfo.cellIndex].CellAllSkuInfoList)
            {
                if (skuInfo.SkuCode == lpnDWorkOrderInfo.skuCode)
                {
                    skuInfo.QtyAlreadyPickup += lpnDWorkOrderInfo.qtyOutputUom;
                    skuInfo.QtyAllocated += lpnDWorkOrderInfo.qtyUom;
                    isFindInAll = true;
                    break;
                }
            }
            if (!isFindInAll)  //格子里面新增一个SKU
            {
                var cellSkuInfo = new CellSkuInfo
                {
                    SkuCode = lpnDWorkOrderInfo.skuCode,
                    SkuEanCode = lpnDWorkOrderInfo.skuEanCode,
                    SkuName = lpnDWorkOrderInfo.skuName,
                    QtyAlreadyPickup = lpnDWorkOrderInfo.qtyOutputUom,
                    QtyAllocated = lpnDWorkOrderInfo.qtyUom
                };

                dicIdCellInfoList[lpnDWorkOrderInfo.cellIndex].CellAllSkuInfoList.Add(cellSkuInfo);
            }

            //扫描LPN加载，将属于改LPN的DWork id !=0 的记录下来
            if (loadType == LoadType.LPNCode && lpnDWorkOrderInfo.id != 0 && !string.IsNullOrEmpty(lpnDWorkOrderInfo.lpnCode) && !string.IsNullOrEmpty(txtLpnCode.Text) && lpnDWorkOrderInfo.lpnCode.ToUpper() == txtLpnCode.Text.Trim().ToUpper())
            {
                bool isFindInValidByLPN = false;
                foreach (var skuInfo in dicIdCellInfoList[lpnDWorkOrderInfo.cellIndex].CellValidSkuInfoListByLPN)
                {
                    if (skuInfo.SkuCode == lpnDWorkOrderInfo.skuCode)
                    {
                        skuInfo.QtyAlreadyPickup += lpnDWorkOrderInfo.qtyOutputUom;
                        skuInfo.QtyAllocated += lpnDWorkOrderInfo.qtyUom;
                        isFindInValidByLPN = true;
                        break;
                    }
                }
                if (!isFindInValidByLPN)
                {
                    var cellSkuInfo = new CellSkuInfo
                    {
                        SkuCode = lpnDWorkOrderInfo.skuCode,
                        SkuEanCode = lpnDWorkOrderInfo.skuEanCode,
                        QtyAlreadyPickup = lpnDWorkOrderInfo.qtyOutputUom,
                        QtyAllocated = lpnDWorkOrderInfo.qtyUom
                    };
                    dicIdCellInfoList[lpnDWorkOrderInfo.cellIndex].CellValidSkuInfoListByLPN.Add(cellSkuInfo);
                }
            }
        }

        /// <summary>
        /// 获取外部取消的格子号
        /// </summary>
        private List<long> GetCellOutCancleList(string str)
        {
            var outCancleCellList = new List<long>();
            if (string.IsNullOrEmpty(str))
            {
                return outCancleCellList;
            }

            var strCellOutCancleList = str.Split(new[] { ',', '，' });
            outCancleCellList.AddRange(strCellOutCancleList.Select(strIndex => Convert.ToInt64(strIndex)));

            return outCancleCellList;
        }

        /// <summary>
        /// 获取当前控件
        /// </summary>
        private BaseCellControl GetCurrentCellsControl(long maxCellIndex)
        {
            if (_curCellsControl != null)
            {
                cellsControl.Controls.Remove(_curCellsControl);
            }
            panelTwoTabPages.Visible = false;
            if (_curPickingType == PickingType.BatchAllPicking)
            {
                if (_curCellsControl != null)
                {
                    panelBatchAllPicking.Controls.Remove(_curCellsControl);
                }
                panelBatchAllPicking.Controls.Add(_oneoHundredCellsControl);
                panelTwoTabPages.Visible = true;
                return _oneoHundredCellsControl;
            }
            if (maxCellIndex > 150)
            {
                cellsControl.Controls.Add(_twoHundredCellsControl);
                return _twoHundredCellsControl;
            }

            if (maxCellIndex > 100)
            {
                cellsControl.Controls.Add(_oneoHundredFiftyCellsControl);
                return _oneoHundredFiftyCellsControl;
            }

            cellsControl.Controls.Add(_oneoHundredCellsControl);
            return _oneoHundredCellsControl;
        }

        /// <summary>
        /// 初始化扫描LPN或分组号时页面信息
        /// </summary>
        private void InitShowInfoByLPNOrWave(List<MbNewLpndSalesOrderInfo> lpndWorkInfoList, LoadType loadType)
        {
            if (lpndWorkInfoList.Count == 0)
                return;
            txtSkuCode.Properties.ReadOnly = false;
            double totalPieceQty = 0;
            double alreadyOnWallQty = 0;
            double alreadySoQty = 0;
            var skuList = new List<string>();
            var cellList = new List<long>();
            foreach (var lpnWorkInfo in lpndWorkInfoList)
            {
                if ((lpnWorkInfo.id == 0 && loadType == LoadType.LPNCode))
                {
                    continue;
                }
                if (loadType == LoadType.LPNCode && lpnWorkInfo.id == 0) //扫描lpn && id == 0,不算总件数
                {
                    totalPieceQty += 0;
                }
                else
                {
                    totalPieceQty += lpnWorkInfo.qtyUom;
                }
                alreadyOnWallQty += lpnWorkInfo.qtyOutputUom;
                if (!skuList.Contains(lpnWorkInfo.skuCode))
                {
                    skuList.Add(lpnWorkInfo.skuCode);
                }
                if (!cellList.Contains(lpnWorkInfo.cellIndex))
                {
                    cellList.Add(lpnWorkInfo.cellIndex);
                    if (lpnWorkInfo.isPackSheetPut)
                    {
                        alreadySoQty += 1;
                    }
                }
            }
            labTotalPieceQty.Text = totalPieceQty.ToString(CultureInfo.InvariantCulture); //本箱总件数
            labTotalSo.Text = cellList.Count.ToString(CultureInfo.InvariantCulture); //总格子数
            labAlreadyOnWallQty.Text = alreadyOnWallQty.ToString(CultureInfo.InvariantCulture); //已上墙    
            labAlreadySo.Text = alreadySoQty.ToString(CultureInfo.InvariantCulture);  //已投递装箱单量
            labTotalSoNo.Text = cellList.Count.ToString(CultureInfo.InvariantCulture); //总格子数
            labSkuItem.Text = skuList.Count.ToString(CultureInfo.InvariantCulture); //本箱总品项
            labWaveGroupCode.Text = lpndWorkInfoList[0].pkhCode; //分组号
            if (!string.IsNullOrEmpty(labWaveGroupCode.Text) && (string.IsNullOrEmpty(labAlreadyOnWallQty.Text) || labAlreadyOnWallQty.Text.Trim() == "0"))
            {
                if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                {
                    if (string.IsNullOrEmpty(labAlreadySo.Text) || labAlreadySo.Text.Trim() == "0")
                    {
                        barbtnChangeWall.Enabled = true;
                    }
                }
                else
                {
                    barbtnChangeWall.Enabled = true;
                }
            }
            _preGroupCode = lpndWorkInfoList[0].pkhCode; //分组号
            labPickupOrder.Text = loadType == LoadType.LPNCode ? (from lpnD in lpndWorkInfoList where lpnD.statusCode != CodeConcentrationCamp.LPNDSO_FINISHED select lpnD.workOrderCode).FirstOrDefault() : null;
            labCurCellNo.Text = null;//格子号
            labCompleteQty.Text = null;
            txtSkuCode.Text = null;//货物Code
            labSkuName.Text = null;//货物名称
            _scanSkuCode = null;
            txtSkuCodeAll.Text = null;
            labSkuNameAll.Text = null;
            lblCellUom.Text = null;
            lblTatol.Text = null;
            lkLabModifySkuQty.Visible = false;
            //GetScanDetailByService(); //需要放在分组号后，顺序不能换,为了获取_dicScanDetail
        }

        /// <summary>
        /// 获取改分组号下所有的woi
        /// </summary>
        private List<MbNewLpndSalesOrderInfo> GetAllDworkInfoList(BehaviorReturn br)
        {
            return br.ObjectList.OfType<MbNewLpndSalesOrderInfo>().ToList();
        }

        /// <summary>
        /// 获取当前需要扫描的订单列表
        /// </summary>
        private List<long> GetPutWallSohIdList(BehaviorReturn br)
        {
            var idList = new List<long>();
            foreach (MbNewLpndSalesOrderInfo lpndSalesOrderInfo in br.ObjectList)
            {
                if (!string.IsNullOrEmpty(lpndSalesOrderInfo.operTypeCode) && lpndSalesOrderInfo.operTypeCode == "TR_PUTONWALL"
                    && !idList.Contains(lpndSalesOrderInfo.sohId))
                {
                    idList.Add(lpndSalesOrderInfo.sohId);
                }
            }
            return idList;
        }

        # endregion

        #region 扫描SKU
        /// <summary>
        /// 扫描SkuCode
        /// </summary>
        private void SkuCodeEnter()
        {
            if (string.IsNullOrEmpty(_scanSkuCode))
            {
                MsgHelper.ShowOKForWarning("货物条码不能为空");
                return;
            }

            if (_curType == LoadType.None)
            {
                MsgHelper.ShowOKForWarning("请先扫描LPN号 或 波次分组");
                return;
            }

            if (_curType == LoadType.LPNCode)
            {
                ScanSkuByLPN();
            }

            if (_curType == LoadType.WaveCode)
            {
                ScanSkuByWaveCode();
            }
        }

        /// <summary>
        /// 通过LPN号扫描SKU
        /// </summary>
        private void ScanSkuByLPN()
        {
            var curSameCellWorkInfoList = GetCurWorkInfo(_curLPNDWorkInfoList);
            if (curSameCellWorkInfoList == null)
            {
                return;
            }

            double totalPickingQty = PutonWallAndGetPickingQty(curSameCellWorkInfoList, _curPickingType == PickingType.PiecePicking);
            if (FloatingHelper.DoubleEqual(totalPickingQty, 0))
            {
                return;
            }

            if (_curPickingType == PickingType.BatchAllPicking)
            {
                AfterScanAllSsameSkuSuccess(curSameCellWorkInfoList, LoadType.LPNCode, totalPickingQty);
                return;
            }
            AfterScanSkuSuccess(curSameCellWorkInfoList, LoadType.LPNCode, totalPickingQty);
        }

        /// <summary>
        /// 通过波次分组扫描SKU
        /// </summary>
        private void ScanSkuByWaveCode()
        {
            var curSameCellWorkInfoList = GetCurWorkInfo(_allDWorkInfoList);
            if (curSameCellWorkInfoList == null)
            {
                return;
            }

            double totalPickingQty = PutonWallAndGetPickingQty(curSameCellWorkInfoList, _curPickingType == PickingType.PiecePicking);
            if (FloatingHelper.DoubleEqual(totalPickingQty, 0))
            {
                return;
            }
            if (_curPickingType == PickingType.BatchAllPicking)
            {
                AfterScanAllSsameSkuSuccess(curSameCellWorkInfoList, LoadType.WaveCode, totalPickingQty);
                return;
            }
            AfterScanSkuSuccess(curSameCellWorkInfoList, LoadType.WaveCode, totalPickingQty);
        }

        /// <summary>
        /// 上架
        /// </summary>
        private double PutonWallAndGetPickingQty(List<MbNewLpndSalesOrderInfo> curSameCellWorkInfoList, bool isPiecePicking)
        {
            var lpndWorkOrderIdList = new List<long>();
            var qtyUomList = new List<double>();
            _dicSameCellWorkIdAndqtyUom.Clear();
            foreach (var lpnD in curSameCellWorkInfoList)
            {
                if (!lpndWorkOrderIdList.Contains(lpnD.id))
                    lpndWorkOrderIdList.Add(lpnD.id);
                if (isPiecePicking)
                {
                    qtyUomList.Add(1);
                }
                else
                {
                    qtyUomList.Add(lpnD.qtyUom - lpnD.qtyOutputUom);
                    _dicSameCellWorkIdAndqtyUom.Add(lpnD.id, lpnD.qtyUom - lpnD.qtyOutputUom);
                }
            }
            if (WMSUserConfig.Instance.STR_VideoCamera == "Y")   //启用视频录制功能
            {
                SetVideoCameraStr(curSameCellWorkInfoList);

            }
            var br = isPiecePicking ? _newWorkOrderBusinessService.PutonWall4ConsolidationByLpn(curSameCellWorkInfoList[0].id, 1)
                                    : _newWorkOrderBusinessService.PutonWall4ConsolidationByLpnBatch(lpndWorkOrderIdList.ToArray(), qtyUomList.ToArray());

            if (!br.Success)
            {
                SetSystemMessage(br);
                return 0;
            }

            double totalPickingQty = 0;
            foreach (var lpnD in curSameCellWorkInfoList)
            {
                if (isPiecePicking)
                {
                    lpnD.qtyOutputUom += 1;
                    totalPickingQty += 1;
                }
                else
                {
                    totalPickingQty += (lpnD.qtyUom - lpnD.qtyOutputUom);
                    lpnD.qtyOutputUom += (lpnD.qtyUom - lpnD.qtyOutputUom);
                }
            }
            return totalPickingQty;
        }

        /// <summary>
        /// 设置录制视频时候标的字
        /// </summary>
        private void SetVideoCameraStr(List<MbNewLpndSalesOrderInfo> cellWorkInfoList)
        {
            var cellIndexList = "";
            var soNoList = "";
            if (_curPickingType == PickingType.BatchAllPicking)
            {
                cellIndexList = cellWorkInfoList.Aggregate(cellIndexList,
                    (current, cellInfo) => current + (cellInfo.cellIndex + ","));
                soNoList = cellWorkInfoList.Aggregate(soNoList,
                    (current, cellInfo) => current + (cellInfo.salesOrderNo + ","));
                if (cellIndexList.Length > 0)
                {
                    cellIndexList = cellIndexList.Substring(0, cellIndexList.Length - 1);
                    soNoList = soNoList.Substring(0, soNoList.Length - 1);
                }
            }
            else
            {
                cellIndexList = cellWorkInfoList[0].cellIndex + "";
                soNoList = cellWorkInfoList[0].salesOrderNo + "";
            }
            VideoCameraHelper.Instance.DrawDate = string.Format("SO单号：{0}\r\nskuCode:{1}\r\n格子号:{2}",
                                                                     soNoList, _scanSkuCode, cellIndexList);
            VideoCameraHelper.Instance.EndTime = GetVideoExtensionTime();
        }

        /// <summary>
        /// 显示格子的装箱单投递信息
        /// </summary>
        private void _curCellsControl_ShowSoListCellEvent()
        {
            var blueBrush = new SolidBrush(Color.Black);
            var rect = new Rectangle(0, 0, 8, 14);
            if (_setupInfo.ScanType != (int)ScanTypeEnum.ScanSO)
            {
                return;
            }
            foreach (var cellInfo in _cellInfoLis)
            {
                if (!cellInfo.AlreadyScanSoCode)
                    continue;
                var labCell = (_curCellsControl.Controls.Find("labCell" + cellInfo.CellIndex, false)[0]) as Label;
                if (labCell != null)
                {
                    labCell.CreateGraphics().FillRectangle(blueBrush, rect);
                }
            }
        }

        /// <summary>
        /// 获取当前货物所对应的
        /// </summary>
        private List<MbNewLpndSalesOrderInfo> GetCurWorkInfo(List<MbNewLpndSalesOrderInfo> lpnDWorkInfoList)
        {
            if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
            {
                if (ScanBySo(lpnDWorkInfoList))
                    return null;
            }

            var curSameCellWorkInfoList = new List<MbNewLpndSalesOrderInfo>();
            var skuCodeOrEanList = IsKboxingCustomer(lpnDWorkInfoList[0].manufacturerCode) ? _curBarRules.ParseSkuCodeOrEanListForJB(KBoxingRule.GetSkuListByRule(_scanSkuCode))
                                                                          : _curBarRules.ParseSkuCodeOrEanList(_scanSkuCode);
            if (skuCodeOrEanList.Count == 0)
            {
                MediaHelper.PlaySoundSysError();
                MsgHelper.ShowOKForWarning(string.Format("货物条码不能解析，请检查"));
                return null;
            }
            bool isFindCurSku = false;
            string skuCodeOrEan = null;
            isFindCurSku = IsFindSku(lpnDWorkInfoList, skuCodeOrEanList, ref skuCodeOrEan);
            if (!isFindCurSku)
            {
                MediaHelper.PlaySoundSysError();
                RecordSurveillance(_scanSkuCode, "", "");
                MsgHelper.ShowOKForWarning(_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO
                                               ? string.Format("订单/货物{0}不在本次任务中", _scanSkuCode)
                                               : string.Format("条码为{0}的货物不在本次任务中或货品不存在", _scanSkuCode));
                return null;
            }
            var newLpndSoList = GetNewLpndSoInfoList(lpnDWorkInfoList);
            long curCell = 0;
            foreach (var workInfo in newLpndSoList)
            {
                if (IsContainsRestSkuOrEan(skuCodeOrEan, workInfo))
                {
                    if (curCell == 0)
                    {
                        RecordSurveillance(_scanSkuCode, workInfo.skuCode, workInfo.cellIndex.ToString(CultureInfo.InvariantCulture));//只会执行一次
                        curCell = workInfo.cellIndex;
                    }
                    if (_curPickingType == PickingType.PiecePicking)
                    {
                        curSameCellWorkInfoList.Add(workInfo);

                        if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                        {
                            _searchPickUpTimer.Stop();
                            SysLogger.LogInfo(LoggerService.CreateLogMsg(SysLogger.SysLogCode, "投递指令前停止查询", "光栅分拣", "GetCurWorkInfo"));
                            AfterScanSkuCode(curSameCellWorkInfoList, workInfo);
                            SysLogger.LogInfo(LoggerService.CreateLogMsg(SysLogger.SysLogCode, "投递指令后开始查询", "光栅分拣", "GetCurWorkInfo"));
                            _searchPickUpTimer.Start();
                            return null;
                        }
                        return curSameCellWorkInfoList;
                    }
                    if (_curPickingType == PickingType.BatchBySoPicking && (curCell == 0 || workInfo.cellIndex == curCell))
                    {
                        curSameCellWorkInfoList.Add(workInfo);
                    }
                    if (_curPickingType == PickingType.BatchAllPicking)
                    {
                        curSameCellWorkInfoList.Add(workInfo);
                    }
                }
            }
            if (curSameCellWorkInfoList.Count != 0) //批量分拣，需要找出所有
            {
                if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                {
                    _searchPickUpTimer.Stop();
                    SysLogger.LogInfo(LoggerService.CreateLogMsg(SysLogger.SysLogCode, "投递指令前停止查询", "光栅分拣", "GetCurWorkInfo"));
                    AfterScanSkuCode(curSameCellWorkInfoList, curSameCellWorkInfoList[0]);
                    SysLogger.LogInfo(LoggerService.CreateLogMsg(SysLogger.SysLogCode, "投递指令后开始查询", "光栅分拣", "GetCurWorkInfo"));
                    _searchPickUpTimer.Start();
                    return null;
                }
                return curSameCellWorkInfoList;
            }

            var curSkuCellInfo = GetCurSkuCellInfo(skuCodeOrEan);
            MediaHelper.PlaySoundSysError();
            MsgHelper.ShowOKForWarning(string.Format("重复扫描，货品{0}已上墙完成\r\n\r\n{1}", skuCodeOrEan, curSkuCellInfo));
            return null;
        }

        /// <summary>
        /// 将外部取消的放在最后面就行投递  
        /// 只有一个格子符合时就投，多个时，将外部取消的放在最后
        /// </summary>
        private IEnumerable<MbNewLpndSalesOrderInfo> GetNewLpndSoInfoList(IEnumerable<MbNewLpndSalesOrderInfo> lpnDWorkInfoList)
        {
            var newLpndSoList = new List<MbNewLpndSalesOrderInfo>();   //总的lpndSo
            var outCancelLpndSoList = new List<MbNewLpndSalesOrderInfo>();  //外部取消的lpndSo
            foreach (var lpndInfo in lpnDWorkInfoList)
            {
                if (!IsOutCancle(lpndInfo.cellIndex))
                {
                    newLpndSoList.Add(lpndInfo);  //先将非外部取消的加入
                }
                else
                {
                    outCancelLpndSoList.Add(lpndInfo);  //外部取消的
                }
            }
            if (outCancelLpndSoList.Count > 0)
            {
                newLpndSoList.AddRange(outCancelLpndSoList);   //在非外部取消之后加入外部取消的
            }
            return newLpndSoList;
        }

        /// <summary>
        /// 判断格子是否外部取消
        /// </summary>
        private bool IsOutCancle(int cellIndex)
        {
            foreach (var cellInfo in _cellInfoLis)
            {
                if (cellInfo.CellIndex == cellIndex)
                {
                    return cellInfo.IsOutCancle;
                }
            }
            return false;
        }

        /// <summary>
        /// 光栅支持扫描so单号
        /// </summary>
        private bool ScanBySo(IEnumerable<MbNewLpndSalesOrderInfo> lpnDWorkInfoList)
        {
            //先找 投递的是否为so单号
            foreach (var workInfo in lpnDWorkInfoList)
            {
                if (workInfo.salesOrderNo == _scanSkuCode)
                {
                    foreach (var cellInfo in _cellInfoLis)
                    {
                        if (cellInfo.CellIndex == workInfo.cellIndex)
                        {
                            if (cellInfo.AlreadyScanSoCode)
                            {
                                MediaHelper.PlaySoundSysError();
                                MsgHelper.ShowOKForWarning(string.Format("重复扫描，装箱单{0}已上墙完成", _scanSkuCode));
                                return true;
                            }
                            _searchPickUpTimer.Stop();
                            SysLogger.LogInfo(LoggerService.CreateLogMsg(SysLogger.SysLogCode, "投递指令前停止查询", "光栅分拣", "ScanBySo"));
                            AfterScanSo(cellInfo, workInfo);
                            SysLogger.LogInfo(LoggerService.CreateLogMsg(SysLogger.SysLogCode, "投递指令后开始查询", "光栅分拣", "ScanBySo"));
                            _searchPickUpTimer.Start();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 判断是否找到sku
        /// </summary>
        private bool IsFindSku(List<MbNewLpndSalesOrderInfo> lpnDWorkInfoList, IEnumerable<string> skuCodeOrEanList, ref string skuCodeOrEan)
        {
            bool isFindCurSku = false;
            foreach (var sku in skuCodeOrEanList)
            {
                if (HashTableHelper.CacheInstance.GetServerConfigValueByConfigCode("OB056") == "Y")
                {
                    foreach (var workInfo in lpnDWorkInfoList)
                    {
                        var skuMulEanCode = workInfo.skuEanCode ?? "";
                        var skuEanCodeList = skuMulEanCode.Split(new char[] { ';', '；' });
                        if (IsEqual(workInfo.skuCode, sku))
                        {
                            isFindCurSku = true;
                            break;
                        }
                        foreach (var skuEanCode in skuEanCodeList)
                        {
                            if (IsEqual(skuEanCode, sku))
                            {
                                isFindCurSku = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    isFindCurSku = lpnDWorkInfoList.Any(workInfo => (IsEqual(workInfo.skuCode, sku) || IsEqual(workInfo.skuEanCode, sku)
                         || IsEqual(workInfo.skuEanCode2, sku) || IsEqual(workInfo.skuEanCode3, sku) || IsEqual(workInfo.skuEanCode4, sku)
                         || IsEqual(workInfo.skuEanCode5, sku) || IsEqual(workInfo.skuEanCode6, sku)) && workInfo.id != 0);
                }

                if (isFindCurSku)
                {
                    skuCodeOrEan = sku;
                    _lastScanSkuCode.Add(sku);
                    break;
                }
            }
            return isFindCurSku;
        }

        /// <summary>
        /// 光栅方式：扫描skuCode后
        /// </summary>
        private void AfterScanSkuCode(List<MbNewLpndSalesOrderInfo> curSameCellWorkInfoList, MbNewLpndSalesOrderInfo workInfo)
        {
            MbCellInfo curCellInfo = null;
            CellSkuInfo curCellSkuInfo = null;
            var curCellIndex = curSameCellWorkInfoList[0].cellIndex;
            var curSkuName = curSameCellWorkInfoList[0].skuName;
            if (_curType == LoadType.LPNCode)
            {
                foreach (var cellInfo in _cellInfoLis)
                {
                    if (cellInfo.CellIndex != Convert.ToInt32(curCellIndex))
                        continue;
                    curCellInfo = cellInfo;
                    foreach (var cellSkuInfo in curCellInfo.CellValidSkuInfoListByLPN)
                    {
                        if (cellSkuInfo.SkuCode != curSameCellWorkInfoList[0].skuCode)
                            continue;
                        //cellSkuInfo.QtyAlreadyPickup += totalPickingQty;
                        curCellSkuInfo = cellSkuInfo;
                    }
                }
            }
            foreach (var cellInfo in _cellInfoLis)
            {
                if (cellInfo.CellIndex != Convert.ToInt32(curCellIndex))
                    continue;
                curCellInfo = cellInfo;
                foreach (var cellSkuInfo in curCellInfo.CellAllSkuInfoList)
                {
                    if (cellSkuInfo.SkuCode != curSameCellWorkInfoList[0].skuCode)
                        continue;
                    //cellSkuInfo.QtyAlreadyPickup += totalPickingQty;
                    curCellSkuInfo = cellSkuInfo;
                }
            }
            if (curCellInfo == null)
                return;
            _continueSearch = false;
            SetPutOrder(workInfo, true);
            labSkuName.Text = curSkuName; //货物名称
            labCurCellNo.Visible = true;
            labCurCellNo.Text = curCellIndex.ToString(CultureInfo.InvariantCulture); //格子号
            labCurCellNo.ForeColor = labCurCellNo.ForeColor == Color.Red ? Color.Blue : Color.Red;
            labCurCellNo.Refresh();

            if (curCellSkuInfo != null)
            {
                labCompleteQty.Visible = true;
                labCompleteQty.Text = string.Format("第{0}件/共{1}件", (curCellSkuInfo.QtyAlreadyPickup + 1).ToString(CultureInfo.InvariantCulture), curCellSkuInfo.QtyAllocated.ToString(CultureInfo.InvariantCulture));
                labCompleteQty.ForeColor = labCurCellNo.ForeColor;
                labCompleteQty.Refresh();
            }

            if (!string.IsNullOrEmpty(labAlreadyOnWallQty.Text) && labAlreadyOnWallQty.Text.Trim() != "0")
            {
                if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                {
                    if (!string.IsNullOrEmpty(labAlreadySo.Text) && labAlreadySo.Text.Trim() != "0")
                    {
                        barbtnChangeWall.Enabled = false;
                    }
                }
                else
                {
                    barbtnChangeWall.Enabled = false;
                }
            }
            PlayCellNoSoundAndQty(labCurCellNo.Text.Trim(), 1);
            _curCellIndex = Convert.ToInt32(curCellIndex);
            if (_preLpndSo.id != 0)
            {
                ShowPreDetail();
            }

            _preLpndSo = curSameCellWorkInfoList[0];
            _prePickNum = 1;
            _preIsSo = false;
            _curCellSkuInfo = curCellSkuInfo;
            _stopShowSuccess = true;
            _writeLpndSoList = curSameCellWorkInfoList;
            _waitPutSign = false;
            _continueSearch = true;
        }

        /// <summary>
        /// 光栅方式：扫描装箱单后
        /// </summary>
        private void AfterScanSo(MbCellInfo cellInfo, MbNewLpndSalesOrderInfo workInfo)
        {
            _continueSearch = false;
            SetPutOrder(workInfo, false);
            MediaHelper.PlaySoundByName(workInfo.cellIndex.ToString(CultureInfo.InvariantCulture));
            labCurCellNo.Visible = true;
            labCurCellNo.Text = workInfo.cellIndex.ToString(CultureInfo.InvariantCulture); //格子号
            labCurCellNo.ForeColor = labCurCellNo.ForeColor == Color.Red ? Color.Blue : Color.Red;
            labCompleteQty.Text = "";
            if (_preLpndSo.id != 0)
            {
                ShowPreDetail();
            }
            _prePickNum = 1;
            _preIsSo = true;
            _preLpndSo = workInfo;
            _stopShowSuccess = true;
            _writeCellInfo = cellInfo;
            _continueSearch = true;
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

        /// <summary>
        /// 记录监控信息
        /// </summary>
        private void RecordSurveillance(string skuEan, string skuCode, string cellNo)
        {
            if (_sysConfige.CharsSysCon_WC0010.Length > 2 && _sysConfige.CharsSysCon_WC0010[2].ToString(CultureInfo.InvariantCulture) == "1")
            {
                var scHelper = new SurveillanceCameraHelper();
                if (WMSUserConfig.Instance.SurveillanceType == WMSUserConfig.SurveillanceToNative)
                {
                    scHelper.WriteInfoForSecondPicking(labWaveGroupCode.Text, txtLpnCode.Text, skuEan, skuCode, cellNo, ClientService.CurrentUserInfo.userName, DateTime.Now.ToString(TimeFormatHelper.LongTimeFormat()));
                }
            }
        }

        /// <summary>
        /// 获取当前SKUCode在格子里面的信息
        /// </summary>
        private string GetCurSkuCellInfo(string skuCodeOrEan)
        {
            var sb = new StringBuilder();
            int item = 0;
            foreach (var pair in _dicScanDetail)
            {
                var workInfo = pair.Value;
                //多EAN码
                if (HashTableHelper.CacheInstance.GetServerConfigValueByConfigCode("OB056") == "Y")
                {
                    var skuMulEanCode = workInfo.SKUEanCode ?? "";
                    var skuEanCodeList = skuMulEanCode.Split(new[] { ';', '；' });
                    if (IsEqual(workInfo.SkuCode, skuCodeOrEan))
                    {
                        if (item != 0 && item % 5 == 0)
                        {
                            sb.Append("\r\n");
                        }
                        sb.Append(string.Format("[格子{0}：{1}件] ", workInfo.CellIndex, workInfo.Qty));
                        item++;
                        continue;
                    }
                    if (skuEanCodeList.Any(skuEanCode => IsEqual(skuEanCode, skuCodeOrEan)))
                    {
                        if (item != 0 && item % 5 == 0)
                        {
                            sb.Append("\r\n");
                        }
                        sb.Append(string.Format("[格子{0}：{1}件] ", workInfo.CellIndex, workInfo.Qty));
                        item++;
                    }
                }
                else
                {
                    if (IsEqual(workInfo.SkuCode, skuCodeOrEan) || IsEqual(workInfo.SKUEanCode, skuCodeOrEan) || IsEqual(workInfo.SKUEanCode2, skuCodeOrEan) || IsEqual(workInfo.SKUEanCode3, skuCodeOrEan)
                   || IsEqual(workInfo.SKUEanCode4, skuCodeOrEan) || IsEqual(workInfo.SKUEanCode5, skuCodeOrEan) || IsEqual(workInfo.SKUEanCode6, skuCodeOrEan))
                    {
                        if (item != 0 && item % 5 == 0)
                        {
                            sb.Append("\r\n");
                        }
                        sb.Append(string.Format("[格子{0}：{1}件] ", workInfo.CellIndex, workInfo.Qty));
                        item++;
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 找到所扫描SKU对应的有 空余的DWork && 其id !=0
        /// </summary>
        private bool IsContainsRestSkuOrEan(string skuCodeOrEan, MbNewLpndSalesOrderInfo workInfo)
        {
            //多EAN码
            if (HashTableHelper.CacheInstance.GetServerConfigValueByConfigCode("OB056") == "Y")
            {
                var skuMulEanCode = workInfo.skuEanCode ?? "";
                var skuEanCodeList = skuMulEanCode.Split(new[] { ';', '；' });
                if (IsEqual(workInfo.skuCode, skuCodeOrEan) && workInfo.qtyOutputUom < workInfo.qtyUom && workInfo.id != 0)
                {
                    return true;
                }
                foreach (var skuEanCode in skuEanCodeList)
                {
                    if (IsEqual(skuEanCode, skuCodeOrEan) && workInfo.qtyOutputUom < workInfo.qtyUom && workInfo.id != 0)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if ((IsEqual(workInfo.skuCode, skuCodeOrEan) || IsEqual(workInfo.skuEanCode, skuCodeOrEan) || IsEqual(workInfo.skuEanCode2, skuCodeOrEan) || IsEqual(workInfo.skuEanCode3, skuCodeOrEan)
                || IsEqual(workInfo.skuEanCode4, skuCodeOrEan) || IsEqual(workInfo.skuEanCode5, skuCodeOrEan) || IsEqual(workInfo.skuEanCode6, skuCodeOrEan))
                && workInfo.qtyOutputUom < workInfo.qtyUom && workInfo.id != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsEqual(string skuCode, string skuCodeOrEan)
        {
            if (string.IsNullOrEmpty(skuCode))
            {
                return false;
            }
            return skuCode.ToUpper() == skuCodeOrEan.ToUpper();
        }

        /// <summary>
        /// 成功扫描SKU后
        /// </summary>
        private void AfterScanSkuSuccess(List<MbNewLpndSalesOrderInfo> curSameCellWorkInfoList, LoadType loadType, double totalPickingQty)
        {
            MbCellInfo curCellInfo = null;
            CellSkuInfo curCellSkuInfo = null;
            var curCellIndex = curSameCellWorkInfoList[0].cellIndex;
            var curSkuName = curSameCellWorkInfoList[0].skuName;
            if (loadType == LoadType.LPNCode)
            {
                foreach (var cellInfo in _cellInfoLis)
                {
                    if (cellInfo.CellIndex != Convert.ToInt32(curCellIndex))
                        continue;
                    curCellInfo = cellInfo;
                    foreach (var cellSkuInfo in curCellInfo.CellValidSkuInfoListByLPN)
                    {
                        if (cellSkuInfo.SkuCode != curSameCellWorkInfoList[0].skuCode)
                            continue;
                        cellSkuInfo.QtyAlreadyPickup += totalPickingQty;
                        curCellSkuInfo = cellSkuInfo;
                    }
                }
            }
            foreach (var cellInfo in _cellInfoLis)
            {
                if (cellInfo.CellIndex != Convert.ToInt32(curCellIndex))
                    continue;
                curCellInfo = cellInfo;
                foreach (var cellSkuInfo in curCellInfo.CellAllSkuInfoList)
                {
                    if (cellSkuInfo.SkuCode != curSameCellWorkInfoList[0].skuCode)
                        continue;
                    cellSkuInfo.QtyAlreadyPickup += totalPickingQty;
                    curCellSkuInfo = cellSkuInfo;
                }
            }
            if (curCellInfo == null)
                return;
            curCellInfo.QtyOutputboxUom += totalPickingQty;
            labSkuName.Text = curSkuName; //货物名称
            labCurCellNo.Visible = true;
            labCurCellNo.Text = curCellIndex.ToString(CultureInfo.InvariantCulture); //格子号
            labCurCellNo.ForeColor = labCurCellNo.ForeColor == Color.Red ? Color.Blue : Color.Red;
            labCurCellNo.Refresh();
            if (curCellSkuInfo != null)
            {
                labCompleteQty.Visible = true;
                labCompleteQty.Text = _curPickingType == PickingType.PiecePicking ? string.Format("第{0}件/共{1}件", (curCellSkuInfo.QtyAlreadyPickup).ToString(CultureInfo.InvariantCulture), curCellSkuInfo.QtyAllocated.ToString(CultureInfo.InvariantCulture)) : string.Format("投{0}件", totalPickingQty);
                labCompleteQty.ForeColor = labCurCellNo.ForeColor;
                labCompleteQty.Refresh();
            }
            labAlreadyOnWallQty.Text = (Convert.ToInt64(labAlreadyOnWallQty.Text) + totalPickingQty).ToString(CultureInfo.InvariantCulture);
            labAlreadyOnWallQty.Refresh();
            if (!string.IsNullOrEmpty(labAlreadyOnWallQty.Text) && labAlreadyOnWallQty.Text.Trim() != "0")
            {
                if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                {
                    if (!string.IsNullOrEmpty(labAlreadySo.Text) && labAlreadySo.Text.Trim() != "0")
                    {
                        barbtnChangeWall.Enabled = false;
                    }
                }
                else
                {
                    barbtnChangeWall.Enabled = false;
                }
            }
            _curCellsControl.ChangeCellColor(curCellIndex.ToString(CultureInfo.InvariantCulture), curCellInfo.QtyOutputboxUom < curCellInfo.QtyAllocatedUom ? Color.Red : Color.Green);
            cellsControl.Refresh();
            PlayCellNoSoundAndQty(labCurCellNo.Text.Trim(), totalPickingQty);
            AddThisTimeScanDetail(curSameCellWorkInfoList, totalPickingQty);
            lkLabModifySkuQty.Visible = totalPickingQty > 1;
            _curCellTotalPickingQty = totalPickingQty;
            _curSameCellWorkInfoList = curSameCellWorkInfoList;
            _curCellIndex = Convert.ToInt32(curCellIndex);
            if (_preLpndSo.id != 0)
            {
                ShowPreDetail();
            }

            _preLpndSo = _curSameCellWorkInfoList[0];
            _prePickNum = totalPickingQty;
            _preIsSo = false;
            _curCellSkuInfo = curCellSkuInfo;
            _stopShowSuccess = true;
            if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                return;
            if (labAlreadyOnWallQty.Text.Trim().ToUpper() == labTotalPieceQty.Text.Trim().ToUpper())
            {
                Thread.Sleep(500);
                MediaHelper.PlaySoundSysMsg();

                var wd = new WarningTwoeBtnDialog("恭喜你，你已完成本次任务", "提示", true, "异常登记", "结束分拣");
                switch (wd.ShowDialog(this))
                {
                    case DialogResult.Yes:  //异常登记
                        btnExceptionRecord_ItemClick(null, null);
                        barConfirm_ItemClick(null, null);
                        break;
                    case DialogResult.No:   //结束分拣
                        barConfirm_ItemClick(null, null);
                        break;
                }
            }
        }

        /// <summary>
        /// 成功扫描SKU后
        /// </summary>
        private void AfterScanAllSsameSkuSuccess(List<MbNewLpndSalesOrderInfo> curSameCellWorkInfoList, LoadType loadType,
            double totalPickingQty)
        {
            var curCellInfoList = new Dictionary<MbCellInfo, double>();
            foreach (var obf in curSameCellWorkInfoList)
            {
                var curCellIndex = obf.cellIndex;
                if (loadType == LoadType.LPNCode)
                {
                    foreach (var cellInfo in _cellInfoLis)
                    {
                        if (cellInfo.CellIndex != Convert.ToInt32(curCellIndex))
                            continue;

                        foreach (var cellSkuInfo in cellInfo.CellValidSkuInfoListByLPN)
                        {
                            if (cellSkuInfo.SkuCode != curSameCellWorkInfoList[0].skuCode)
                                continue;
                            if (_dicSameCellWorkIdAndqtyUom.ContainsKey(obf.id))
                                cellSkuInfo.QtyAlreadyPickup += _dicSameCellWorkIdAndqtyUom[obf.id];
                        }
                    }
                }
                foreach (var cellInfo in _cellInfoLis)
                {
                    if (cellInfo.CellIndex != Convert.ToInt32(curCellIndex))
                        continue;
                    foreach (var cellSkuInfo in cellInfo.CellAllSkuInfoList)
                    {
                        if (cellSkuInfo.SkuCode != curSameCellWorkInfoList[0].skuCode)
                            continue;
                        if (_dicSameCellWorkIdAndqtyUom.ContainsKey(obf.id))
                        {
                            cellSkuInfo.QtyAlreadyPickup += _dicSameCellWorkIdAndqtyUom[obf.id];
                            cellInfo.QtyOutputboxUom += _dicSameCellWorkIdAndqtyUom[obf.id];
                        }
                        if (!curCellInfoList.ContainsKey(cellInfo))
                            curCellInfoList.Add(cellInfo, cellSkuInfo.QtyAlreadyPickup);
                        else
                        {
                            if (_dicSameCellWorkIdAndqtyUom.ContainsKey(obf.id))
                                curCellInfoList[cellInfo] += _dicSameCellWorkIdAndqtyUom[obf.id];
                        }
                    }
                }
            }
            if (curCellInfoList.Count == 0)
                return;

            lblTatol.Text = totalPickingQty.ToString(CultureInfo.InvariantCulture);
            lblCellUom.Text = curCellInfoList.Count.ToString(CultureInfo.InvariantCulture);
            labelControl4.Location = new Point(lblTatol.Location.X + lblTatol.Size.Width + 4, labelControl4.Location.Y);
            lblCellUom.Location = new Point(labelControl4.Location.X + labelControl4.Size.Width + 4, lblCellUom.Location.Y);
            labelControl5.Location = new Point(lblCellUom.Location.X + lblCellUom.Size.Width + 4, labelControl5.Location.Y);

            labSkuNameAll.Text = curSameCellWorkInfoList[0].skuName; //货物名称

            labAlreadyOnWallQty.Text = (Convert.ToInt64(labAlreadyOnWallQty.Text) + totalPickingQty).ToString(CultureInfo.InvariantCulture);
            labAlreadyOnWallQty.Refresh();
            if (!string.IsNullOrEmpty(labAlreadyOnWallQty.Text) && labAlreadyOnWallQty.Text.Trim() != "0")
            {
                if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                {
                    if (!string.IsNullOrEmpty(labAlreadySo.Text) && labAlreadySo.Text.Trim() != "0")
                    {
                        barbtnChangeWall.Enabled = false;
                    }
                }
                else
                {
                    barbtnChangeWall.Enabled = false;
                }
            }
            SetPanelCell(_cellInfoLis);
            foreach (var curCellIndex in curCellInfoList.Keys)
            {
                _curCellsControl.ChangeCellColor(curCellIndex.CellIndex.ToString(CultureInfo.InvariantCulture), curCellIndex.QtyOutputboxUom < curCellIndex.QtyAllocatedUom ? Color.Red : Color.Green);

                ReSetPanelCell(curCellIndex.CellIndex, (from VARIABLE in curSameCellWorkInfoList where Convert.ToInt32(VARIABLE.cellIndex) == curCellIndex.CellIndex where _dicSameCellWorkIdAndqtyUom.ContainsKey(VARIABLE.id) select _dicSameCellWorkIdAndqtyUom[VARIABLE.id]).Sum());
            }
            panelBatchAllPicking.Refresh();
            AddThisTimeScanDetail(curSameCellWorkInfoList, totalPickingQty);

            _stopShowSuccess = true;

            if (labAlreadyOnWallQty.Text.Trim().ToUpper() == labTotalPieceQty.Text.Trim().ToUpper())
            {
                Thread.Sleep(500);
                MediaHelper.PlaySoundSysMsg();

                var wd = new WarningTwoeBtnDialog("恭喜你，你已完成本次任务", "提示", true, "异常登记", "结束分拣");
                switch (wd.ShowDialog(this))
                {
                    case DialogResult.Yes:  //异常登记
                        btnExceptionRecord_ItemClick(null, null);
                        barConfirm_ItemClick(null, null);
                        break;
                    case DialogResult.No:   //结束分拣
                        barConfirm_ItemClick(null, null);
                        break;
                }
            }
        }

        private void ReSetPanelCell(long cellIndex, double qtyAlreadyPickup)
        {
            var panel = (panelControl3.Controls.Find("panel" + cellIndex, false)[0]) as Panel;
            if (panel != null)
            {
                panel.Controls.Clear();
                var label2 = new Label
                {
                    Text = cellIndex.ToString(CultureInfo.InvariantCulture),
                    Location = new Point(23, 5),
                    Font = new Font("Tahoma", 12, FontStyle.Bold),
                    ForeColor = Color.FromArgb(121, 121, 121),
                    Size = new Size(cellIndex.ToString(CultureInfo.InvariantCulture).Length * 20, 20)
                };
                var label1 = new Label
                {
                    Text = @"格",
                    Location = new Point(3, 5),
                    Font = new Font("Tahoma", 12),
                    ForeColor = Color.FromArgb(121, 121, 121),
                    Size = new Size(20, 20)
                };
                var label3 = new Label
                {
                    Text = qtyAlreadyPickup.ToString(CultureInfo.InvariantCulture),
                    Location = new Point(31, 25),
                    Font = new Font("Tahoma", 16, FontStyle.Bold),
                    ForeColor = Color.FromArgb(22, 155, 213),
                    Size = new Size(qtyAlreadyPickup.ToString(CultureInfo.InvariantCulture).Length * 20, 25)
                };
                var label4 = new Label
                {
                    Text = @"件",
                    Location = new Point(label3.Location.X + label3.Size.Width + 4, 30),
                    Font = new Font("Tahoma", 12),
                    ForeColor = Color.FromArgb(121, 121, 121),
                    Size = new Size(20, 20)
                };
                panel.Controls.Add(label2);
                panel.Controls.Add(label1);
                panel.Controls.Add(label4);
                panel.Controls.Add(label3);
                panel.BackColor = Color.White;
                panel.Tag = true;
                panel.Invalidate();
            }
        }
        /// <summary>
        /// 显示上一枪明细
        /// </summary>
        private void ShowPreDetail()
        {
            panelControl1.Controls.Add(_secondRichText);
            panelControl1.AutoScroll = false;
            _secondRichText.richTextBox1.Text = "";
            var index1 = 0;
            var index2 = 0;
            var index3 = 0;
            var index4 = 0;

            var length1 = 0;
            var length2 = 0;
            var length3 = 0;
            var length4 = 0;

            if (_preIsSo)
            {
                _secondRichText.richTextBox1.AppendText("  ");
                index1 = ("  ").Length;
                length1 = 0;
                _secondRichText.richTextBox1.AppendText("  装箱单   " + _preLpndSo.salesOrderNo + "   投  " + _prePickNum + "件 " +
                                                        "至" +
                                                        " 格" + _preLpndSo.cellIndex + "\r\n");
                index2 = index1 + length1 + ("  装箱单   ").Length;
                length2 = (_preLpndSo.salesOrderNo).Length;
                index3 = index2 + length2 + "   投  ".Length;
                length3 = (_prePickNum + "件 ").Length;
                index4 = index3 + length3 + "至".Length;
                length4 = (" 格" + _preLpndSo.cellIndex).Length;
            }
            else
            {
                if (_lastScanSkuCode.Count > 1 && ExitSkuCode(_preLpndSo, _lastScanSkuCode[_lastScanSkuCode.Count - 2]))
                {
                    _secondRichText.richTextBox1.AppendText(" 货物条码   ");
                    _secondRichText.richTextBox1.AppendText(_lastScanSkuCode[_lastScanSkuCode.Count - 2]);
                    index1 = (" 货物条码   ").Length;
                    length1 = (_lastScanSkuCode[_lastScanSkuCode.Count - 2]).Length;
                }
                else
                {
                    _secondRichText.richTextBox1.AppendText("  ");
                    index1 = ("  ").Length;
                    length1 = 0;
                }
                _secondRichText.richTextBox1.AppendText("  货物代码   " + _preLpndSo.skuCode + "   投  " + _prePickNum + "件 " +
                                                        "至" +
                                                        " 格" + _preLpndSo.cellIndex + "\r\n");
                _secondRichText.richTextBox1.AppendText("" + _preLpndSo.skuName);
                index2 = index1 + length1 + ("  货物代码   ").Length;
                length2 = (_preLpndSo.skuCode).Length;
                index3 = index2 + length2 + "   投  ".Length;
                length3 = (_prePickNum + "件 ").Length;
                index4 = index3 + length3 + "至".Length;
                length4 = (" 格" + _preLpndSo.cellIndex).Length;
            }

            _secondRichText.richTextBox1.Select(index1, length1);
            _secondRichText.richTextBox1.SelectionFont = new Font("宋体", 16F, FontStyle.Bold);
            _secondRichText.richTextBox1.Select(index2, length2);
            _secondRichText.richTextBox1.SelectionFont = new Font("宋体", 16F, FontStyle.Bold);
            _secondRichText.richTextBox1.Select(index3, length3);
            _secondRichText.richTextBox1.SelectionFont = new Font("宋体", 16F, FontStyle.Bold);
            _secondRichText.richTextBox1.Select(index4, length4);
            _secondRichText.richTextBox1.SelectionFont = new Font("宋体", 16F, FontStyle.Bold);

            _secondRichText.Refresh();
        }

        //判断当前LpndSo是否存在skucode
        private bool ExitSkuCode(MbNewLpndSalesOrderInfo preLpndSo, string skuScanCode)
        {
            if (string.IsNullOrEmpty(skuScanCode))
                return false;
            if (HashTableHelper.CacheInstance.GetServerConfigValueByConfigCode("OB056") == "Y")
            {
                if (IsEqual(preLpndSo.skuCode, skuScanCode))
                {
                    return true;
                }
                if (string.IsNullOrEmpty(preLpndSo.skuEanCode))
                    return false;
                var codeList = preLpndSo.skuEanCode.Split(new char[] { ';', '；' });
                foreach (var skuEanCode in codeList)
                {
                    if (IsEqual(skuEanCode, skuScanCode))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if ((IsEqual(preLpndSo.skuCode, skuScanCode) || IsEqual(preLpndSo.skuEanCode, skuScanCode)
                     || IsEqual(preLpndSo.skuEanCode2, skuScanCode) || IsEqual(preLpndSo.skuEanCode3, skuScanCode) ||
                     IsEqual(preLpndSo.skuEanCode4, skuScanCode)
                     || IsEqual(preLpndSo.skuEanCode5, skuScanCode) || IsEqual(preLpndSo.skuEanCode6, skuScanCode)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 将本次集货明细添加到前台
        /// </summary>
        private void AddThisTimeScanDetail(List<MbNewLpndSalesOrderInfo> curSameCellWorkInfoList, double totalPickingQty)
        {
            if (_curPickingType == PickingType.BatchAllPicking)
            {
                foreach (var curWorkInfo in curSameCellWorkInfoList)
                {
                    if (_dicSameCellWorkIdAndqtyUom.ContainsKey(curWorkInfo.id))
                    {
                        var pickingQty = _dicSameCellWorkIdAndqtyUom[curWorkInfo.id];
                        if (_dicScanDetail.ContainsKey(curWorkInfo.cellIndex + curWorkInfo.skuCode))
                        {
                            _dicScanDetail[curWorkInfo.cellIndex + curWorkInfo.skuCode].Qty += pickingQty;
                        }
                        else
                        {
                            _dicScanDetail.Add(curWorkInfo.cellIndex + curWorkInfo.skuCode,
                                NewSkuScanDetail(curWorkInfo, pickingQty));
                        }
                    }
                }
                return;
            }
            var curWorkInfo1 = curSameCellWorkInfoList[0];
            if (_dicScanDetail.ContainsKey(curWorkInfo1.cellIndex + curWorkInfo1.skuCode))
            {
                _dicScanDetail[curWorkInfo1.cellIndex + curWorkInfo1.skuCode].Qty += totalPickingQty;
            }
            else
            {
                _dicScanDetail.Add(curWorkInfo1.cellIndex + curWorkInfo1.skuCode, NewSkuScanDetail(curWorkInfo1, totalPickingQty));
            }
        }

        private SkuScanDetail NewSkuScanDetail(MbNewLpndSalesOrderInfo curWorkInfo, double pickingQty)
        {
            return new SkuScanDetail
            {
                CellIndex = curWorkInfo.cellIndex,
                SkuCode = curWorkInfo.skuCode,
                SkuName = curWorkInfo.skuName,
                SKUEanCode = curWorkInfo.skuEanCode,
                SKUEanCode2 = curWorkInfo.skuEanCode2,
                SKUEanCode3 = curWorkInfo.skuEanCode3,
                SKUEanCode4 = curWorkInfo.skuEanCode4,
                SKUEanCode5 = curWorkInfo.skuEanCode5,
                SKUEanCode6 = curWorkInfo.skuEanCode6,
                Qty = pickingQty
            };
        }

        /// <summary>
        /// 播放格子号声音
        /// </summary>
        private void PlayCellNoSoundAndQty(string cellNo, double qty)
        {
            if (_curPickingType == PickingType.BatchAllPicking) return;
            if (_curPickingType == PickingType.PiecePicking || qty == 1)
            {
                MediaHelper.PlaySoundByName(cellNo);
                return;
            }
            MediaHelper.PlaySoundByName(cellNo.Trim());
            Thread.Sleep((cellNo.Length - 1) * 100 + 300);
            MediaHelper.PlaySoundByName("投");
            Thread.Sleep(300);
            MediaHelper.PlaySoundByName(qty.ToString(CultureInfo.InvariantCulture));
            Thread.Sleep((qty.ToString(CultureInfo.InvariantCulture).Length - 1) * 100 + 300);
            MediaHelper.PlaySoundByName("件");
        }

        #endregion

        #region 作业排名
        /// <summary>
        /// 作业排名推送
        /// </summary>
        void JobTimer_Tick(object sender, EventArgs e)
        {
            //var jobInfoList = _jobService.GetJobRanking(ClientService.CurrentUserInfo.code, "ONWALL", ClientService.CurrentWareId); ;
            //if (jobInfoList == null || jobInfoList.Count == 0)
            //    return;
            //var threeJobInfo = new List<MbJobRankingInfo>();
            //long totalQty = jobInfoList.Where(job => !string.IsNullOrEmpty(job.optNum)).Sum(job => Convert.ToInt64(job.optNum));
            //foreach (var job in jobInfoList)
            //{
            //    job.totalQty = totalQty.ToString(CultureInfo.InvariantCulture);
            //    if (job.optNum == job.topOptNum)
            //    {
            //        job.order = "1";
            //    }
            //    threeJobInfo.Add(job);
            //    if (threeJobInfo.Count == 3)
            //    {
            //        _jobSerialNum++;
            //        if (!_dicJobInfo.ContainsKey(_jobSerialNum))
            //        {
            //            _dicJobInfo.Add(_jobSerialNum, threeJobInfo);
            //        }
            //        threeJobInfo = new List<MbJobRankingInfo>();
            //    }
            //}
            //if (threeJobInfo.Count != 0)
            //{
            //    _jobSerialNum++;
            //    _dicJobInfo.Add(_jobSerialNum, threeJobInfo);
            //}
        }

        /// <summary>
        /// 作业排名显示
        /// </summary>
        void ShowAlterTimer_Tick(object sender, EventArgs e)
        {
            if (_dicJobInfo.Count == 0)
                return;
            foreach (var alert in jobMsgControl.AlertFormList)
            {
                alert.Close();
            }
            foreach (var pair in _dicJobInfo)
            {
                var count = pair.Value.Count;
                if (count == 0)
                    continue;
                _curJobKey = pair.Key;
                for (int i = 0; i < count; i++)
                {
                    jobMsgControl.Show(this, _alertInfo);
                }
                return;
            }
        }

        void jobMsgControl_BeforeFormShow(object sender, AlertFormEventArgs e)
        {
            if (!_dicJobInfo.ContainsKey(_curJobKey))
                return;
            if (_dicJobInfo[_curJobKey].Count == 0)
                return;
            var job = _dicJobInfo[_curJobKey][0];
            var point = GetLoactionPoint(_dicJobInfo[_curJobKey].Count);
            AlertFormHelper.AddLabelToAlterForm(e, point, job, "ONWALL");
            _dicJobInfo[_curJobKey].Remove(job);
            if (_dicJobInfo[_curJobKey].Count == 0)
            {
                _dicJobInfo.Remove(_curJobKey);
            }
        }

        private Point GetLoactionPoint(int count)
        {
            if (count == 1)
            {
                return new Point(SystemInformation.WorkingArea.Width - 200, SystemInformation.WorkingArea.Height - 350);
            }
            if (count == 2)
            {
                return new Point(SystemInformation.WorkingArea.Width - 200, SystemInformation.WorkingArea.Height - 600);
            }
            if (count == 3)
            {
                return new Point(SystemInformation.WorkingArea.Width - 200, SystemInformation.WorkingArea.Height - 850);
            }
            return new Point(SystemInformation.WorkingArea.Width - 200, SystemInformation.WorkingArea.Height - 350);
        }
        #endregion

        private void BtnExceptionRecordClick()
        {
            btnExceptionRecord_ItemClick(null, null);
        }

        /// <summary>
        /// 异常登记
        /// </summary>
        void btnExceptionRecord_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_curBarRules == null)
                return;
            var exceptionRecordDialog = new ExceptionRecordDiaForSecondPickupLPN(_curBarRules, _curType == LoadType.LPNCode ?
                _curLPNDWorkInfoList : _allDWorkInfoList);
            exceptionRecordDialog.ShowDialog();
            SkuCodeFocus();
        }

        private void BarConfirmClick()
        {
            barConfirm_ItemClick(null, null);
        }

        /// <summary>
        /// 集货完成
        /// </summary>
        void barConfirm_ItemClick(object sender, ItemClickEventArgs e)
        {
            ClearSystemMessage();
            if (string.IsNullOrEmpty(labWaveGroupCode.Text))
            {
                return;
            }
            lkLabModifySkuQty.Visible = false;
            if (ShowOutCancleAndLoseSkuCells())
            {
                if (!MsgHelper.ShowYesNoDefaultNo("有外部取消或缺货的格子，确认集货完成？", "上架提醒"))
                {
                    IsVisiableOutCancleCells(false);
                    IsVisibleCurCellNo(true);
                    return; //点击否返回
                }
            }

            ClearPreScan();

            BehaviorReturn br = _newWorkOrderBusinessService.ConfirmPutOnWallFinish(labWaveGroupCode.Text.Trim());
            //由于增加换箱功能，原来的LPN补录功能去除。代码暂不删除
            /*if (!br.Success)
            {
                //增加LPN的补录
                if (CheckLpnByConfig(br))
                {
                    br = _newWorkOrderBusinessService.ConfirmPutOnWallFinish(labWaveGroupCode.Text.Trim());
                    if (!br.Success)
                    {
                        if (IsNeedChangeLPN(br))
                        {
                            br = _newWorkOrderBusinessService.ConfirmPutOnWallFinish(labWaveGroupCode.Text.Trim());
                        }
                    }
                }
                else
                {
                    if (IsNeedChangeLPN(br))
                    {
                        br = _newWorkOrderBusinessService.ConfirmPutOnWallFinish(labWaveGroupCode.Text.Trim());
                    }
                }
            }*/
            if (br.Success)
            {
                DoOutCancel();
                AutomaticCheckAndShip(labWaveGroupCode.Text.Trim());
                if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                {
                    SendAfterConfirm();
                }
                barbtnSet.Enabled = true;
                btnChangeLpn.Enabled = false;
                ClearInfo();
                IsEnableButton(false);
                IsVisiableOutCancleCells(false);
                IsVisibleCurCellNo(true);
                txtSkuCode.Properties.ReadOnly = true;
                txtSkuCodeAll.Properties.ReadOnly = true;
                FocusControl();
                if (WMSUserConfig.Instance.STR_VideoCamera == "Y")
                {
                    VideoCameraHelper.Instance.VideoEndAfterWait();
                }
                return;
            }
            if (!br.hasMessage())
                return;
            if (br.MessageList[0].code != "ERR_WO_ALREADY_PICKED")
            {
                SetSystemMessage(br);
                return;
            }
            if (!MsgHelper.ShowYesNoDefaultNo("还有未处理的工单，是否继续？？", "提醒"))
            {
                return;
            }
            var brAssemble = _assemblerMoveWorkOrderItemService.ForceReleasePkhAssemblerByPkh(labWaveGroupCode.Text);
            if (!brAssemble.Success)
            {
                SetSystemMessage(brAssemble);
                return;
            }
            var brWorker = _newWorkOrderBusinessService.ConfirmPutOnWallFinish(labWaveGroupCode.Text.Trim());
            if (!brWorker.Success)
            {
                SetSystemMessage(brWorker);
            }
            else
            {
                DoOutCancel();
                AutomaticCheckAndShip(labWaveGroupCode.Text.Trim());
                if (WMSUserConfig.Instance.STR_VideoCamera == "Y")
                {
                    VideoCameraHelper.Instance.VideoEndAfterWait();
                }
            }
            if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
            {
                SendAfterConfirm();
            }
        }

        /// <summary>
        /// 自动验货发货
        /// </summary>
        private void AutomaticCheckAndShip(string pkhCode)
        {
            _progressBarHelper.ShowProgressbar();
            var br = _newWorkOrderBusinessService.AutomaticCheckAndShip(pkhCode);
            _progressBarHelper.SysnCloseProgressbar();
            if (br == null)
            {
                MsgHelper.ShowOKForWarning("自动验货发货失败!");
                CreateShipExceptionByPkhCodeList(pkhCode, "自动验货发货失败!");
                return;
            }
            //有错误信息直接报错显示
            if (!br.Success)
            {
                SetSystemMessage(br);
                var sbError = new StringBuilder();
                foreach (var sm in br.MessageList)
                {
                    sbError.Append(sm.messageCN);
                }
                CreateShipExceptionByPkhCodeList(pkhCode, sbError.ToString());
                return;
            }
            //提示信息
            if (!string.IsNullOrEmpty(br.udf3))
            {
                var msgBoxDialog = new MessageBoxDialog(br.udf3, "提示");
                msgBoxDialog.ShowDialog();
                CreateShipExceptionByPkhCodeList(pkhCode, br.udf3);
            }
        }

        /// <summary>
        /// 记录发运报错信息
        /// </summary>
        private void CreateShipExceptionByPkhCodeList(string pkhCode, string errStr)
        {
            if (!_configInfoWhenOpen.useShipException)
                return;
            _exceptionRecordService.CreateShipExceptionByPkhCodeList(
                    new MbShipExceptionRecordInfo
                    {
                        whId = ClientService.CurrentWareId,
                        orgId = ClientService.CurrentOrgId,
                        shipView = "二次分拣",
                        shipTypeId = _shipTypeId,
                        errorMsg = errStr
                    }, new List<string> { pkhCode });
        }

        /// <summary>
        /// 分拣结束成功后  单据是外部取消时进行判断操作
        /// </summary>
        private void DoOutCancel()
        {
            if (_sohIdList.Count <= 0)
                return;

            //取配置项
            var checkConfigValueInfo = GetCheckConfigValueInfo(_sohIdList);

            if (checkConfigValueInfo == null)
                return;
            if (!checkConfigValueInfo.outCancelClose)
                return;

            var br = _newWorkOrderBusinessService.GetOutCancledSohListAndCellIndex(_allDWorkInfoList[0].pkhId);
            if (!br.hasEntity())
                return;
            var outCancelSohList = br.ObjectList.Cast<MbSalesOrderHeaderInfo>().ToList();
            var outCancelConfig = new OutCancelConfig(outCancelSohList, "ONWALL", lbWallLoc.Text, br.udf7 + " 已被外部取消。请取出订单货物，并录入货物的取消库位!");
            outCancelConfig.OutCancelByConfig(checkConfigValueInfo);
            return;
        }

        /// <summary>
        /// 获取订单相关的配置项
        /// </summary>
        private MbCheckConfigValueInfo GetCheckConfigValueInfo(List<long> sohIdList)
        {
            var configBr = _configurationService.GetConfigWhenOutBoundReleaseInventory(sohIdList.ToArray());
            if (!configBr.hasEntity())
            {
                MsgHelper.ShowOKForWarning(configBr.hasMessage() ? configBr.MessageList[0].messageCN : "加载配置项错误,请正确加载配置项后再操作!");
                return null;
            }

            return configBr.ObjectList[0] as MbCheckConfigValueInfo;
        }

        /// <summary>
        /// 是否需要换LPN号,换箱是否成功,true表示需要重新掉服务
        /// </summary>
        private bool IsNeedChangeLPN(BehaviorReturn br)
        {
            if (!br.hasMessage() || !br.MessageList[0].code.Contains("LPN"))
                return false;

            if (_bizConfigInfo == null)
                return false;

            //不保留LPN或 一个LPN
            if (StringHelper.EqualIgnoreCase(GetPickUpRetainLpnEnumStr(), PickUpRetainLpnEnum.NOT_RETAIN.ToString()) ||
                    StringHelper.EqualIgnoreCase(GetPickUpRetainLpnEnumStr(), PickUpRetainLpnEnum.ONE_LPN.ToString()))
                return false;

            var dicInvalidCellAndLpn = new Dictionary<string, string>(); //无效的cell号和lpn
            //一个分货格一个LPN
            if (StringHelper.EqualIgnoreCase(GetPickUpRetainLpnEnumStr(), PickUpRetainLpnEnum.ONE_SO_ONE_LPN.ToString()))
            {
                foreach (var msg in br.MessageList)
                {
                    var strSpilt = msg.messageCN.Split(new[] { '[', ']' });
                    if (strSpilt.Length < 2)
                        continue;
                    var lpnCode = strSpilt[1];
                    if (string.IsNullOrEmpty(lpnCode))
                        continue;
                    if (!_dicLpnCellNo.ContainsKey(lpnCode.ToUpper()))
                        continue;
                    dicInvalidCellAndLpn.Add(_dicLpnCellNo[lpnCode.ToUpper()], lpnCode.ToUpper());
                }
            }
            if (dicInvalidCellAndLpn.Count == 0)  //一个分货格一个LPN的时候不需要考虑_curWaveLpn
                return false;
            var changeLPNAndCellMapDia = new ChangeLPNAndCellMapDia(_allDWorkInfoList[0].pkhId, GetPickUpRetainLpnEnumStr(), _curWaveLpn, dicInvalidCellAndLpn, _dicExistCellAndSohId);
            changeLPNAndCellMapDia.ShowDialog();
            return changeLPNAndCellMapDia.DialogResult == DialogResult.OK;
        }

        private bool CheckLpnByConfig(BehaviorReturn br)
        {
            if (!br.hasMessage() || (br.MessageList[0].code != "ERR_PUTONWALL_LPN_INVALID" && br.MessageList[0].code != "ERR_LPN_OBZ_TWO_EXCEPTION"))
                return false;
            if (_bizConfigInfo == null)
                return false;
            //一个LPN
            if (br.MessageList[0].code == "ERR_PUTONWALL_LPN_INVALID" && StringHelper.EqualIgnoreCase(GetPickUpRetainLpnEnumStr(), PickUpRetainLpnEnum.ONE_LPN.ToString()))
            {
                MbNewLpnHRelationOutboundInfo curLpnHRelationOutbound = GetCurConfigAndLpnHRelationOutbound(txtLpnCode.Text.Trim(), txtWaveGroupCode.Text.Trim());
                if (curLpnHRelationOutbound == null || _bizConfigInfo == null)
                    return false;
                var scanPickupCarDia = new ScanPickupCarDia(curLpnHRelationOutbound.pickupHeaderId, txtLpnCode.Text.Trim(), txtWaveGroupCode.Text.Trim(),_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO );
                scanPickupCarDia.ShowDialog();
                if (scanPickupCarDia.DialogResult == DialogResult.OK)
                {
                    if (string.IsNullOrEmpty(scanPickupCarDia.LPNCode))
                        return false;
                    _curWaveLpn = scanPickupCarDia.LPNCode;
                    _curWall = scanPickupCarDia.LPNCode; //配置一个LPN时,如果不等于前一次的分组，弹出toLPN输入框，扫描1个可用LPN（同时更新墙角库位的值等于该LPN号）
                    lbWallLoc.Text = _curWall;
                    return true;
                }
            }
            //一个分货格一个LPN
            if (br.MessageList[0].code == "ERR_LPN_OBZ_TWO_EXCEPTION" &&
               StringHelper.EqualIgnoreCase(GetPickUpRetainLpnEnumStr(), PickUpRetainLpnEnum.ONE_SO_ONE_LPN.ToString()))
            {
                MbNewLpnHRelationOutboundInfo curLpnHRelationOutbound = GetCurConfigAndLpnHRelationOutbound(txtLpnCode.Text.Trim(), txtWaveGroupCode.Text.Trim());
                if (curLpnHRelationOutbound == null || _bizConfigInfo == null)
                    return false;
                var dicExistCellAndLpn = GetLPNAndCellInfo(curLpnHRelationOutbound.pickupHeaderId);
                if (dicExistCellAndLpn != null && dicExistCellAndLpn.Count == _curAllCellAndSohId.Count && !_isFrist)
                    return true;

                var bindLPNAndCellMapDialog = new BindLPNAndCellMapDialog(curLpnHRelationOutbound.pickupHeaderId, dicExistCellAndLpn, _curAllCellAndSohId);
                bindLPNAndCellMapDialog.ShowDialog();
                if (bindLPNAndCellMapDialog.DialogResult == DialogResult.OK)
                {
                    _dicLpnCellNo = bindLPNAndCellMapDialog.DicLpnCellNo;
                    _isFrist = false;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 显示外部取消和缺货格子
        /// </summary>
        private bool ShowOutCancleAndLoseSkuCells()
        {
            BehaviorReturn br = _newWorkOrderBusinessService.GetLackCellIndex(labWaveGroupCode.Text);
            if (string.IsNullOrEmpty(br.udf3))
                return false;
            string lackSkuCellInfo = null;
            string outCancleCellInfo = null;
            string lackSoCellInfo = null;
            var strs = br.udf3.Split(new[] { ';' });//不能移除空字符串
            //方法返回值的udf3存 缺货格子(中间用逗号分隔) | 作废格子(中间用逗号分隔)  | 缺装箱单格子
            if (strs.Length > 0)
            {
                lackSkuCellInfo = string.IsNullOrEmpty(strs[0]) ? null : strs[0];
            }
            if (strs.Length > 1)
            {
                outCancleCellInfo = string.IsNullOrEmpty(strs[1]) ? null : strs[1];
            }
            if (strs.Length > 2 && _setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
            {
                lackSoCellInfo = string.IsNullOrEmpty(strs[2]) ? null : strs[2];
            }
            if (lackSkuCellInfo == null && outCancleCellInfo == null && lackSoCellInfo == null)
            {
                return false;
            }

            labLackSkuCells.Text = lackSkuCellInfo;
            labOutCancelCells.Text = outCancleCellInfo;
            labLackSoCells.Text = lackSoCellInfo;

            IsVisiableOutCancleCells(true);
            IsVisibleCurCellNo(false);

            ShowFontSize(lackSkuCellInfo, labLackSkuCells);
            ShowFontSize(outCancleCellInfo, labOutCancelCells);
            ShowFontSize(lackSoCellInfo, labLackSoCells);

            return true;
        }

        /// <summary>
        /// 根据字符串长度显示字体的大小
        /// </summary>
        private void ShowFontSize(string strCellInfo , Label lackLabel)
        {
            if (strCellInfo == null)
                return;
            if (strCellInfo.Length > 240)
            {
                lackLabel.Font = new Font("Tahoma", 8);
            }
            else if (strCellInfo.Length > 160)
            {
                lackLabel.Font = new Font("Tahoma", 10);
            }
            else if (strCellInfo.Length > 100)
            {
                lackLabel.Font = new Font("Tahoma", 12);
            }
            else
            {
                lackLabel.Font = new Font("Tahoma", 16);
            }
        }

        private void BarScanDetailClick()
        {
            barScanDetail_ItemClick(null, null);
        }

        /// <summary>
        /// 集货明细
        /// </summary>
        void barScanDetail_ItemClick(object sender, ItemClickEventArgs e)
        {
            ClearSystemMessage();
            var skuScanDetails = GetScanDetailByService();
            if (skuScanDetails == null || skuScanDetails.Count == 0)
            {
                MsgHelper.ShowOKForWarning("没有扫描明细");
                return;
            }
            var scanDetailDia = new ScanDetailDia(skuScanDetails);
            scanDetailDia.ShowDialog();
        }

        private List<SkuScanDetail> GetScanDetailByService()
        {
            if (string.IsNullOrEmpty(labWaveGroupCode.Text))
            {
                return null;
            }
            var br = _newWorkOrderBusinessService.GetMbLpnDPkdByLpnCodeAndPkhCode(null, labWaveGroupCode.Text.Trim()); //返回所对应分组号下的全部 woi
            if (!br.Success)
            {
                SetSystemMessage(br);
                return null;
            }
            var skuScanDetails = GetScanDetails(br);
            var sortByCellIndex = new ScanDetailsListSortByCellIndexm();
            skuScanDetails.Sort(sortByCellIndex);
            return skuScanDetails;
        }

        private List<SkuScanDetail> GetScanDetails(BehaviorReturn br)
        {
            _dicScanDetail.Clear();
            foreach (var obj in br.ObjectList)
            {
                var lpnDwork = obj as MbNewLpndSalesOrderInfo;
                if (lpnDwork == null || lpnDwork.qtyOutputUom == 0) continue;

                if (_dicScanDetail.ContainsKey(lpnDwork.cellIndex + lpnDwork.skuCode))
                {
                    _dicScanDetail[lpnDwork.cellIndex + lpnDwork.skuCode].Qty += lpnDwork.qtyOutputUom;
                }
                else
                {
                    var scanSkuInfo = new SkuScanDetail
                    {
                        CellIndex = lpnDwork.cellIndex,
                        SkuCode = lpnDwork.skuCode,
                        SkuName = lpnDwork.skuName,
                        Qty = lpnDwork.qtyOutputUom,
                        SKUEanCode = lpnDwork.skuEanCode,
                        SKUEanCode2 = lpnDwork.skuEanCode2,
                        SKUEanCode3 = lpnDwork.skuEanCode3,
                        SKUEanCode4 = lpnDwork.skuEanCode4,
                        SKUEanCode5 = lpnDwork.skuEanCode5,
                        SKUEanCode6 = lpnDwork.skuEanCode6,
                    };
                    _dicScanDetail.Add(lpnDwork.cellIndex + lpnDwork.skuCode, scanSkuInfo);
                }
            }
            return _dicScanDetail.Values.ToList();
        }

        private void BarRestartClick()
        {
            barRestart_ItemClick(null, null);
        }


        /// <summary>
        /// 切换分拣模式
        /// </summary>
        void barSwitch_ItemClick(object sender, ItemClickEventArgs e)
        {
            var setupScanRate = new SetupScanRate();
            setupScanRate.ShowDialog(this);
            if (setupScanRate.DialogResult != DialogResult.OK) return;
            var pickingTypeValue = _iniFile.IniReadvalue("MbSecondEachPickupByLPNForm" + ClientService.CurrentWareId, "PickingType");
            SetPickingType(pickingTypeValue);
            ShowScanSkuInfo(_curPickingType != PickingType.BatchAllPicking);
            if (_curPickingType != PickingType.BatchAllPicking)
            {
                panelTwoTabPages.Visible = false;
                panelBatchAllPicking.Controls.Clear();
                cellsControl.Controls.Add(_curCellsControl);
                labCurCellNo.Visible = true;
                labCompleteQty.Visible = true;
                SetPanelCell(_cellInfoLis);
                txtSkuCodeAll.Text = null;
                labSkuNameAll.Text = null;
                lblCellUom.Text = null;
                lblTatol.Text = null;
            }
            else
            {
                panelTwoTabPages.Visible = true;
                cellsControl.Controls.Clear();
                panelBatchAllPicking.Controls.Add(_curCellsControl);
                labCurCellNo.Visible = false;
                labCompleteQty.Visible = false;
            }
        }

        void SetPickingType(string pickingType)
        {
            if (string.IsNullOrEmpty(pickingType) || StringHelper.EqualIgnoreCase(pickingType, "PiecePicking"))
            {
                _curPickingType = PickingType.PiecePicking;
                txtCurPickingType.Text = @"当前分拣模式：逐件分拣";
            }
            else if (StringHelper.EqualIgnoreCase(pickingType, "BatchBySoPicking"))
            {
                _curPickingType = PickingType.BatchBySoPicking;
                txtCurPickingType.Text = @"当前分拣模式：按单批量分拣";
            }
            else if (StringHelper.EqualIgnoreCase(pickingType, "BatchAllPicking"))
            {
                _curPickingType = PickingType.BatchAllPicking;
                txtCurPickingType.Text = @"当前分拣模式：按SKU批量分拣";
            }
        }

        /// <summary>
        /// 扣减货物数量
        /// </summary>
        void lkLabModifySkuQty_Click(object sender, EventArgs e)
        {
            if (_curCellTotalPickingQty < 2)
                return;
            var modifySkuQtyDialog = new ModifySkuQtyDialog(Tools.GetObjectArray<MbNewLpndSalesOrderInfo, MbNewLpndSalesOrderInfo>(_curSameCellWorkInfoList).ToList(), _curCellTotalPickingQty, _curBarRules);
            modifySkuQtyDialog.ShowDialog();
            if (modifySkuQtyDialog.DialogResult != DialogResult.OK)
                return;
            var dicLpndAndDeductionQty = modifySkuQtyDialog.DicLpndAndDeductionQty;
            BehaviorReturn br = _newWorkOrderBusinessService.ReducePutonWall4ConsolidationByLpnBatch(dicLpndAndDeductionQty.Keys.ToArray(), dicLpndAndDeductionQty.Values.ToArray());
            if (!br.Success)
            {
                SetSystemMessage(br);
                MediaHelper.PlaySoundSysError();
                return;
            }
            foreach (var lpnd in _curSameCellWorkInfoList)
            {
                if (dicLpndAndDeductionQty.ContainsKey(lpnd.id))
                {
                    lpnd.qtyOutputUom = lpnd.qtyOutputUom - dicLpndAndDeductionQty[lpnd.id];
                }
            }
            double reduceQty = dicLpndAndDeductionQty.Values.Sum();
            labCompleteQty.Text = string.Format("投{0}件", _curCellTotalPickingQty - reduceQty);
            labAlreadyOnWallQty.Text = (Convert.ToInt64(labAlreadyOnWallQty.Text) - reduceQty).ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(labAlreadyOnWallQty.Text) && labAlreadyOnWallQty.Text.Trim() != "0")
            {
                if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                {
                    if (!string.IsNullOrEmpty(labAlreadySo.Text) && labAlreadySo.Text.Trim() != "0")
                    {
                        barbtnChangeWall.Enabled = false;
                    }
                }
                else
                {
                    barbtnChangeWall.Enabled = false;
                }
            }
            _cellInfoLis[_curCellIndex - 1].QtyOutputboxUom -= reduceQty;
            _curCellTotalPickingQty -= reduceQty;
            _curCellSkuInfo.QtyAlreadyPickup -= reduceQty;
            var curWorkInfo = _curSameCellWorkInfoList[0];
            if (_dicScanDetail.ContainsKey(curWorkInfo.cellIndex + curWorkInfo.skuCode))
            {
                _dicScanDetail[curWorkInfo.cellIndex + curWorkInfo.skuCode].Qty -= reduceQty;
            }
            _curCellsControl.ChangeCellColor(_curCellIndex.ToString(CultureInfo.InvariantCulture), Color.Red);
            SkuCodeFocus();
        }

        /// <summary>
        /// 重置（只能对分组进行重置）
        /// </summary>
        void barRestart_ItemClick(object sender, ItemClickEventArgs e)
        {
            ClearSystemMessage();
            ClearPreScan();
            if (string.IsNullOrEmpty(labWaveGroupCode.Text))
            {
                MsgHelper.ShowOKForWarning("分组号 不能为空");
                return;
            }
            BehaviorReturn br = _newWorkOrderBusinessService.ResetLpndSoList(labWaveGroupCode.Text.Trim());
            if (!br.Success)
            {
                SetSystemMessage(br);
                return;
            }
            if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
            {
                ClearStatus();
            }
            if (GetServiceInfo(null, labWaveGroupCode.Text.Trim(), _curType))
            {
                InitShowInfoByLPNOrWave(_curType == LoadType.LPNCode ? _curLPNDWorkInfoList : _allDWorkInfoList, _curType);
            }
        }

        /// <summary>
        /// 货物条码输入框
        /// </summary>
        private void SkuCodeFocus()
        {
            if (_curPickingType == PickingType.BatchAllPicking)
            {
                txtSkuCodeAll.Focus();
                txtSkuCodeAll.SelectAll();
                return;
            }
			
            txtSkuCode.Focus();
            txtSkuCode.SelectAll();
        }

        void txtWaveGroupCode_Click(object sender, EventArgs e)
        {
            SecondEachPickupByLPNForm_MouseClick(null, null);
            txtWaveGroupCode.SelectAll();
        }

        void txtSkuCode_Click(object sender, EventArgs e)
        {
            SecondEachPickupByLPNForm_MouseClick(null, null);
            txtSkuCode.SelectAll();
        }

        void txtLpnCode_Click(object sender, EventArgs e)
        {
            SecondEachPickupByLPNForm_MouseClick(null, null);
            txtLpnCode.SelectAll();
        }

        void SecondEachPickupByLPNForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_jobTimer != null)
            {
                _jobTimer.Stop();
            }
            if (_showTimer != null)
            {
                _showTimer.Stop();
            }
            if (_searchPickUpTimer != null)
            {
                _searchPickUpTimer.Stop();
            }
            SecondEachPickUp.Instance.Dispose();
            if (WMSUserConfig.Instance.STR_VideoCamera == "Y")
            {
                VideoCameraHelper.Instance.VideoStop();
            }
        }

        /// <summary>
        /// 快捷点
        /// </summary>
        private void SecondEachPickupByLPNForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    if (_curType == LoadType.None)
                    {
                        MsgHelper.ShowOKForWarning("请先扫描LPN号 或 波次分组");
                        return;
                    }
                    barRestart_ItemClick(null, null); // 重置
                    break;

                case Keys.F6:
                    barScanDetail_ItemClick(null, null); // 扫描明细
                    break;

                case Keys.F2:
                    if (_curType == LoadType.None)
                    {
                        MsgHelper.ShowOKForWarning("请先扫描LPN号 或 波次分组");
                        return;
                    }
                    barConfirm_ItemClick(null, null); //确认下架
                    break;

                case Keys.F8:
                    barbtnStopPut_ItemClick(null, null); // 中止投递
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 清除页面信息
        /// </summary>
        private void ClearInfo()
        {
            labTotalPieceQty.Text = null; //本箱总件数
            labAlreadyOnWallQty.Text = null; //已上墙    
            labTotalSo.Text = null; //装箱单数
            labAlreadySo.Text = null; //已投放
            labTotalSoNo.Text = null; //总订单数
            labSkuItem.Text = null; //本箱总品项
            labWaveGroupCode.Text = null;//分组号
            barbtnChangeWall.Enabled = false;
            labPickupOrder.Text = null; //拣货工单
            txtSkuCode.Text = null;
            _scanSkuCode = null;
            txtSkuCodeAll.Text = null;
            txtWaveGroupCode.Text = null;
            txtLpnCode.Text = null;
            labSkuName.Text = null;
            labSkuNameAll.Text = null;
            labCurCellNo.Text = null;
            lblCellUom.Text = null;
            lblTatol.Text = null;
            labCompleteQty.Text = null;
            lkLabModifySkuQty.Visible = false;
            panelTwoTabPages.Visible = false;
            if (_curCellsControl != null)
            {
                cellsControl.Controls.Remove(_curCellsControl);
            }
        }

        /// <summary>
        /// 控制按钮状态
        /// </summary>
        private void IsEnableButton(bool isEnable)
        {
            barRestart.Enabled = isEnable;
            barScanDetail.Enabled = isEnable;
            barConfirm.Enabled = isEnable;
            barSwitch.Enabled = isEnable;
            btnExceptionRecord.Enabled = isEnable;
            if (StringHelper.EqualIgnoreCase(GetPickUpRetainLpnEnumStr(), PickUpRetainLpnEnum.ONE_SO_ONE_LPN.ToString()))
            {
                btnChangeLpn.Enabled = isEnable;
            }
            else
            {
                btnChangeLpn.Enabled = false;
            }
        }

        /// <summary>
        /// 是否显示 缺货格子和 作废格子
        /// </summary>
        private void IsVisiableOutCancleCells(bool isVisible)
        {
            label1.Visible = isVisible;
            label2.Visible = isVisible;
            labOutCancelCells.Visible = isVisible;
            labLackSkuCells.Visible = isVisible;

            label19.Visible = isVisible && _setupInfo.ScanType == (int)ScanTypeEnum.ScanSO;
            labLackSoCells.Visible = isVisible && _setupInfo.ScanType == (int)ScanTypeEnum.ScanSO;
        }

        /// <summary>
        /// 是否显示 当前格子号
        /// </summary>
        private void IsVisibleCurCellNo(bool isVisible)
        {
            if (_curPickingType == PickingType.BatchAllPicking)
            {
                labCurCellNo.Visible = false;
                labCompleteQty.Visible = false;
                return;
            }
            labCurCellNo.Visible = isVisible;
            labCompleteQty.Visible = isVisible;
        }

        private void FocusControl()
        {
            if (_curType == LoadType.LPNCode)
            {
                txtLpnCode.Focus();
                return;
            }
            if (_curType == LoadType.WaveCode && _isLpnFake)
            {
                txtLpnCode.Focus();
                return;
            }
            txtWaveGroupCode.Focus();
        }

        /// <summary>
        /// 条码输入框、分拣结束按钮是否可用
        /// </summary>
        private void IsEnableSkuCode(bool isEnable)
        {
            txtSkuCode.Properties.ReadOnly = !isEnable;
            barConfirm.Enabled = isEnable;
        }

        #region 光栅指令消息
        /// <summary>
        /// 从分拣柜获取消息
        /// </summary>
        private void SearchReturnData(object sender, EventArgs e)
        {
            if (_setupInfo.ScanType != (int)ScanTypeEnum.ScanSO)
                return;

            if (SecondEachPickUp.Instance.PreReceiveTime != DateTime.MinValue)
            {
                if ((DateTime.Now - SecondEachPickUp.Instance.PreReceiveTime).TotalSeconds > 30)
                {
                    _searchPickUpTimer.Stop();
                    SecondEachPickUp.Instance.PreReceiveTime = DateTime.Now;
                    SysLogger.LogInfo(LoggerService.CreateLogMsg(SysLogger.SysLogCode, "串口接受数据超时", "光栅分拣", "SearchReturnData"));
                    MsgHelper.ShowOKForWarning("分拣柜连接超时，请检查并重新打开页面!");
                    SecondEachPickUp.Instance.ReceiveSuccess = true;
                    SecondEachPickUp.Instance.PreReceiveTime = DateTime.Now;
                    _searchPickUpTimer.Start();
                }
                if ((DateTime.Now - SecondEachPickUp.Instance.PreReceiveTime).TotalSeconds > 5)
                {
                    SysLogger.LogInfo(LoggerService.CreateLogMsg(SysLogger.SysLogCode, "串口接受数据超时5s", "光栅分拣", "SearchReturnData"));
                    SecondEachPickUp.Instance.ReceiveSuccess = true;
                    SecondEachPickUp.Instance.PreReceiveTime = DateTime.Now;
                }
            }

            if (!SecondEachPickUp.Instance.ReceiveSuccess)
                return;
            GetReturnMessage(SecondEachPickUp.Instance.ReceiveBytes);

            SecondEachPickUp.Instance.WriteByte(_searchMsgBytes);  //查询消息
        }

        /// <summary>
        /// 检查串口连接是否正确
        /// </summary>
        private void TestConnect()
        {
            if (!SecondEachPickUp.Instance.Connect())
            {
                MsgHelper.ShowOKForWarning("串口连接不正确，请检查并重新打开页面!");
            }
        }

        /// <summary>
        /// 自检
        /// </summary>
        private void SelfCheck()
        {
            _searchPickUpTimer.Stop();
            SecondEachPickUp.Instance.WriteByte(_checkSelfBytes);
            _searchPickUpTimer.Start();
        }

        /// <summary>
        /// 清除串口状态
        /// </summary>
        private void ClearStatus()
        {
            _searchPickUpTimer.Stop();
            SecondEachPickUp.Instance.WriteByte(_clearStatusBytes);
            _searchPickUpTimer.Start();
        }

        private void GetReturnMessage(byte[] returnByte)
        {
            if (returnByte == null)
                return;

            if (returnByte.Length == 16)
            {
                if (_continueSearch)
                {
                    _continueSearch = false;
                    GetReturnBySearch(returnByte);
                    _continueSearch = true;
                }
            }
            else if (returnByte.Length == 8)
            {
                if (_continueSearch)
                {
                    _continueSearch = false;
                    GetReturnBySend(returnByte);
                    _continueSearch = true;
                }
            }
        }

        /// <summary>
        /// 查询当前格子信息
        /// </summary>
        private void GetReturnBySearch(byte[] returnByte)
        {
            switch (returnByte[4])
            {
                case (0xA4):
                    {
                        //SecondEachPickUp.Instance.ReceiveSuccess = true;
                        //查询返回的数据
                        if (returnByte[6] == 0xFF)  //投递状态  没有投递
                        {
                        }
                        else if (returnByte[6] == 0x00)  //投递正确
                        {
                            AfterReceiveByteRight();
                        }
                        else if (returnByte[6] == 0x01 && _preSkuCodeAndEan.Count != 0 && _stopErrorMsg)  //投递错误
                        {
                            _stopErrorMsg = false;
                            // 返回错误的格子号
                            var throwErrorDia = new ThrowErrorDia(_preSkuCodeAndEan, _preScanSkuCode);
                            if (throwErrorDia.ShowDialog() == DialogResult.OK)
                            {
                                _stopErrorMsg = true;
                                SecondEachPickUp.Instance.WriteByte(_preSendBytes); //设置投递命令
                                Thread.Sleep(100);
                            }
                            return;
                        }

                        if ((returnByte[10] != 0x00 || returnByte[11] != 0x00 || returnByte[12] != 0x00 || returnByte[13] != 0x00) && _stopShowMsg)
                        {
                            _stopShowMsg = false;
                            IsEnableSkuCode(false);
                            //var cellIndexList = GetCellIndex(returnByte[10], returnByte[11]);
                            //if (cellIndexList == null || cellIndexList.Count == 0)
                            //    return;
                            //var str = new StringBuilder();
                            //foreach (var cellIndex in cellIndexList)
                            //{
                            //    str.Append(cellIndex);
                            //    str.Append(",");
                            //}
                            //_stopShowMsg = false;
                            //MsgHelper.ShowOKForWarning(string.Format("格[{0}]处光栅被遮挡，请整理格子内的货物!", str.ToString().Substring(0, str.Length - 1)));
                            //_stopShowMsg = true;
                        }
                        if (returnByte[10] == 0x00 && returnByte[11] == 0x00 && returnByte[12] == 0x00 && returnByte[13] == 0x00
                                && _writeCellInfo == null && _writeLpndSoList == null && !_stopShowMsg)
                        {
                            IsEnableSkuCode(true);
                            _stopShowMsg = true;
                        }
                    }
                    return;
            }
        }

        /// <summary>
        /// 收到投递正确的信息
        /// </summary>
        private void AfterReceiveByteRight()
        {
            if (_writeCellInfo != null)   //投递装箱单
            {
                var br = _newWorkOrderBusinessService.PutonPackSheet(_writeCellInfo.CellSohId);
                if (!br.Success)
                {
                    SetSystemMessage(br);
                    return;
                }
                _writeCellInfo.AlreadyScanSoCode = true;
                _curCellsControl.ChangeBorderColor(_writeCellInfo.CellIndex.ToString(CultureInfo.InvariantCulture));           
                labAlreadySo.Text = (Convert.ToInt64(labAlreadySo.Text) + 1).ToString(CultureInfo.InvariantCulture);
                if (_writeCellInfo.QtyOutputboxUom >= _writeCellInfo.QtyAllocatedUom && _writeCellInfo.AlreadyScanSoCode)
                {
                    _greenLightBytes[5] = (byte)_writeCellInfo.CellIndex;
                    _greenLightBytes[9] = 5;
                    SecondEachPickUp.Instance.WriteByte(_greenLightBytes);  //后端亮绿灯
                }
                _writeCellInfo = null;
                IsEnableSkuCode(true);
            }
            if (!_waitPutSign && _writeLpndSoList != null && _writeLpndSoList[0].qtyOutputUom < _writeLpndSoList[0].qtyUom)
            {
                double totalPickingQty = PutonWallAndGetPickingQty(_writeLpndSoList,true);
                if (totalPickingQty == 0)
                {
                    _writeLpndSoList = null;
                    return;
                }
                MbCellInfo curCellInfo = null;
                var curCellIndex = _writeLpndSoList[0].cellIndex;
                var curSkuName = _writeLpndSoList[0].skuName;
                if (_curType == LoadType.LPNCode)
                {
                    foreach (var cellInfo in _cellInfoLis)
                    {
                        if (cellInfo.CellIndex != Convert.ToInt32(curCellIndex))
                            continue;
                        curCellInfo = cellInfo;
                        foreach (var cellSkuInfo in curCellInfo.CellValidSkuInfoListByLPN)
                        {
                            if (cellSkuInfo.SkuCode != _writeLpndSoList[0].skuCode)
                                continue;
                            cellSkuInfo.QtyAlreadyPickup += totalPickingQty;
                        }
                    }
                }
                foreach (var cellInfo in _cellInfoLis)
                {
                    if (cellInfo.CellIndex != Convert.ToInt32(curCellIndex))
                        continue;
                    curCellInfo = cellInfo;
                    foreach (var cellSkuInfo in curCellInfo.CellAllSkuInfoList)
                    {
                        if (cellSkuInfo.SkuCode != _writeLpndSoList[0].skuCode)
                            continue;
                        cellSkuInfo.QtyAlreadyPickup += totalPickingQty;
                    }
                }
                if (curCellInfo == null)
                    return;
                curCellInfo.QtyOutputboxUom += totalPickingQty;
                labSkuName.Text = curSkuName; //货物名称
                labAlreadyOnWallQty.Text = (Convert.ToInt64(labAlreadyOnWallQty.Text) + totalPickingQty).ToString(CultureInfo.InvariantCulture);
                labAlreadyOnWallQty.Refresh();
                if (!string.IsNullOrEmpty(labAlreadyOnWallQty.Text) && labAlreadyOnWallQty.Text.Trim() != "0")
                {
                    if (_setupInfo.ScanType == (int)ScanTypeEnum.ScanSO)
                    {
                        if (!string.IsNullOrEmpty(labAlreadySo.Text) && labAlreadySo.Text.Trim() != "0")
                        {
                            barbtnChangeWall.Enabled = false;
                        }
                    }
                    else
                    {
                        barbtnChangeWall.Enabled = false;
                    }
                }
                _curCellsControl.ChangeCellColor(curCellIndex.ToString(CultureInfo.InvariantCulture), curCellInfo.QtyOutputboxUom < curCellInfo.QtyAllocatedUom ? Color.Red : Color.Green);
                cellsControl.Refresh();
                AddThisTimeScanDetail(_writeLpndSoList, totalPickingQty);
                lkLabModifySkuQty.Visible = totalPickingQty > 1;
                _curCellTotalPickingQty = totalPickingQty;
                _curSameCellWorkInfoList = _writeLpndSoList;
                _curCellIndex = Convert.ToInt32(curCellIndex);
                _stopShowSuccess = true;
                if (curCellInfo.QtyOutputboxUom >= curCellInfo.QtyAllocatedUom && curCellInfo.AlreadyScanSoCode)
                {
                    _greenLightBytes[5] = (byte)curCellInfo.CellIndex;
                    _greenLightBytes[9] = 5;
                    SecondEachPickUp.Instance.WriteByte(_greenLightBytes);  //后端亮绿灯
                }
                if (_curPickingType == PickingType.PiecePicking)
                {
                    _writeLpndSoList = null;
                    _waitPutSign = true;
                    IsEnableSkuCode(true); 
                }
                else
                {
                    if (_writeLpndSoList != null && _writeLpndSoList[0].qtyOutputUom >= _writeLpndSoList[0].qtyUom)
                    {
                        _writeLpndSoList.Remove(_writeLpndSoList[0]);
                        if (_writeLpndSoList.Count == 0)
                        {
                            _writeLpndSoList = null;
                        }
                    }
                    if (_writeLpndSoList != null)
                    {
                        AfterScanSkuCode(_writeLpndSoList, _writeLpndSoList[0]);
                        _waitPutSign = false;
                    }
                    else
                    {
                        _waitPutSign = true;
                        IsEnableSkuCode(true);
                    }
                }       
            }
            if (labAlreadyOnWallQty.Text.Trim().ToUpper() == labTotalPieceQty.Text.Trim().ToUpper()
                && labTotalSo.Text.Trim().ToUpper() == labAlreadySo.Text.Trim().ToUpper() && _stopErrorMsg && _stopShowSuccess && barConfirm.Enabled
                && !string.IsNullOrEmpty(labAlreadyOnWallQty.Text.Trim().ToUpper()))
            {
                _stopErrorMsg = false;
                _stopShowSuccess = false;
                Thread.Sleep(500);
                MediaHelper.PlaySoundSysMsg();

                var wd = new WarningTwoeBtnDialog("恭喜你，你已完成本次任务", "提示", true, "异常登记", "结束分拣");
                switch (wd.ShowDialog(this))
                {
                    case DialogResult.Yes:  //异常登记
                        btnExceptionRecord_ItemClick(null, null);
                        break;
                    case DialogResult.No:   //结束分拣
                        barConfirm_ItemClick(null, null);
                        break;
                }
                Thread.Sleep(100);
                _stopErrorMsg = true;
            }
        }

        /// <summary>
        /// 中止投递
        /// </summary>
        private void barbtnStopPut_ItemClick(object sender, ItemClickEventArgs e)
        {
            StopPut();
        }

        /// <summary>
        /// 中止投递
        /// </summary>
        private void StopPut()
        {
            if (_waitPutSign || _writeLpndSoList == null || _writeLpndSoList.Count == 0)
                return;
            //考虑现场实际操作，不需要弹窗了，都会中止
            //if (!MsgHelper.ShowYesNo("是否中止本次投递？", "提示"))
            //    return;
            labCompleteQty.Text = "";

            _writeLpndSoList = null;
            _waitPutSign = true;
            _preSendBytes[5] = 1;
            SecondEachPickUp.Instance.WriteByte(_preSendBytes); //设置取消投递命令
            IsEnableSkuCode(true);
        }

        /// <summary>
        /// 发送后的应答信息
        ///  </summary>
        private void GetReturnBySend(byte[] returnByte)
        {
            switch (returnByte[5])
            {
                case 0x00://接收正确
                    {
                        //SecondEachPickUp.Instance.ReceiveSuccess = true;
                        return;
                    }
                case 0x01: //校验错误
                    {
                        if (_stopReceiveMsg)
                        {
                            _stopReceiveMsg = false;
                            MsgHelper.ShowOKForWarning(string.Format("分拣柜非正常工作，请检查!"));
                            Thread.Sleep(100);
                            _stopReceiveMsg = true;
                        }
                        return;
                    }
                case 0x02: //包不完整（字节间接收超时、无包尾等）
                    {
                        if (_stopReceiveMsg)
                        {
                            _stopReceiveMsg = false;
                            MsgHelper.ShowOKForWarning(string.Format("分拣柜非正常工作，请检查!"));
                            Thread.Sleep(100);
                            _stopReceiveMsg = true;
                        }
                        return;
                    }
                case 0x03: //未定义命令
                    {
                        if (_stopReceiveMsg)
                        {
                            _stopReceiveMsg = false;
                            MsgHelper.ShowOKForWarning(string.Format("分拣柜非正常工作，请检查!"));
                            Thread.Sleep(100);
                            _stopReceiveMsg = true;
                        }
                        return;
                    }
            }
        }

        /// <summary>
        /// 分拣结束发送 闪灯的命令
        /// </summary>
        private void SendAfterConfirm()
        {
            _searchPickUpTimer.Stop();
            foreach (var cellInfo in _cellInfoLis)
            {
                if (cellInfo.IsOutCancle)
                {
                    _orangeLightBytes[5] = (byte)cellInfo.CellIndex;
                    SecondEachPickUp.Instance.WriteByte(_orangeLightBytes);  //亮黄灯
                }
                else if (cellInfo.QtyOutputboxUom >= cellInfo.QtyAllocatedUom && cellInfo.AlreadyScanSoCode)
                {
                    _greenLightBytes[5] = (byte)cellInfo.CellIndex;
                    _greenLightBytes[9] = 0;
                    SecondEachPickUp.Instance.WriteByte(_greenLightBytes);  //亮绿灯
                }
                else
                {
                    _redLightBytes[5] = (byte)cellInfo.CellIndex;
                    SecondEachPickUp.Instance.WriteByte(_redLightBytes);  //亮红灯
                }
                Thread.Sleep(100);
            }
            _searchPickUpTimer.Start();
        }

        /// <summary>
        /// 设置投递命令
        /// </summary>
        private void SetPutOrder(MbNewLpndSalesOrderInfo workInfo, bool isSkuCodeOrEan)
        {
            _preSkuCodeAndEan.Clear();
            _preScanSkuCode = "";
            _preScanSkuCode = _scanSkuCode;

            if (isSkuCodeOrEan)
            {
                if (HashTableHelper.CacheInstance.GetServerConfigValueByConfigCode("OB056") == "Y")
                {
                    var skuMulEanCode = workInfo.skuEanCode ?? "";
                    var skuEanCodeList = skuMulEanCode.Split(new[] { ';', '；' });
                    if (!string.IsNullOrEmpty(workInfo.skuCode))
                    {
                        _preSkuCodeAndEan.Add(workInfo.skuCode.ToUpper());
                    }
                    foreach (var skuEanCode in skuEanCodeList)
                    {
                        if (!string.IsNullOrEmpty(skuEanCode))
                        {
                            _preSkuCodeAndEan.Add(skuEanCode.ToUpper());
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(workInfo.skuCode))
                    {
                        _preSkuCodeAndEan.Add(workInfo.skuCode.ToUpper());
                    }
                    if (!string.IsNullOrEmpty(workInfo.skuEanCode))
                    {
                        _preSkuCodeAndEan.Add(workInfo.skuEanCode.ToUpper());
                    }
                    if (!string.IsNullOrEmpty(workInfo.skuEanCode2))
                    {
                        _preSkuCodeAndEan.Add(workInfo.skuEanCode2.ToUpper());
                    }
                    if (!string.IsNullOrEmpty(workInfo.skuEanCode3))
                    {
                        _preSkuCodeAndEan.Add(workInfo.skuEanCode3.ToUpper());
                    }
                    if (!string.IsNullOrEmpty(workInfo.skuEanCode4))
                    {
                        _preSkuCodeAndEan.Add(workInfo.skuEanCode4.ToUpper());
                    }
                    if (!string.IsNullOrEmpty(workInfo.skuEanCode5))
                    {
                        _preSkuCodeAndEan.Add(workInfo.skuEanCode5.ToUpper());
                    }
                    if (!string.IsNullOrEmpty(workInfo.skuEanCode6))
                    {
                        _preSkuCodeAndEan.Add(workInfo.skuEanCode6.ToUpper());
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(workInfo.salesOrderNo))
                {
                    _preSkuCodeAndEan.Add(workInfo.salesOrderNo.ToUpper());
                }
            }

            if (workInfo.cellIndex < 9)
            {
                _setPutOrderBytes[6] = (byte)((0x01) << 8 - workInfo.cellIndex);
                _setPutOrderBytes[7] = 0x00;
            }
            else
            {
                _setPutOrderBytes[7] = (byte)((0x01) << 16 - workInfo.cellIndex);
                _setPutOrderBytes[6] = 0x00;
            }
            if (!isSkuCodeOrEan || _curPickingType == PickingType.PiecePicking)
            {
                _setPutOrderBytes[8] = 5;
            }
            else
            {
                _setPutOrderBytes[8] = (byte) (_setPutOrderBytes[8] == 6 ? 7 : 6);
            }
            _preSendBytes = Tools.GetObjectArray(_setPutOrderBytes);
            SecondEachPickUp.Instance.WriteByte(_setPutOrderBytes); //设置投递命令
            IsEnableSkuCode(false);
        }

        private List<int> GetCellIndex(byte inputByte1, byte inputByte2)
        {
            var cellIndexList = new List<int>();
            if (inputByte1 != 0x00)
            {
                for (var i = 0; i < 8; i++)
                {
                    var a = inputByte1 << i;
                    if ((a & 0x80) == 0x80)
                    {
                        cellIndexList.Add(i + 1);
                    }
                }
            }
            if (inputByte2 != 0x00)
            {
                for (var i = 0; i < 8; i++)
                {
                    var a = inputByte2 << i;
                    if ((a & 0x80) == 0x80)
                    {
                        cellIndexList.Add(i + 9);
                    }
                }
            }
            return cellIndexList;
        }
        #endregion

        /// <summary>
        /// 校验LPN号
        /// </summary>
        private bool CheckLpnCode(string lpnCode)
        {
            if (string.IsNullOrEmpty(_strRegex))
            {
                return true;
            }
            var regex = new Regex(_strRegex.Trim());
            if (!regex.IsMatch(lpnCode))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取相关的配置项
        /// </summary>
        private MbRebinConfigInfo GetCheckConfigValueInfo()
        {
            if (CollectionHelper.CollectionIsEmpty(_sohIdList))
                return null;
            var configBr = _newWorkOrderBusinessService.GetLpnConfig(_sohIdList);
            if (!configBr.hasEntity())
            {
                SetSystemMessage(configBr);
                return null;
            }

            return configBr.ObjectList[0] as MbRebinConfigInfo;
        }

        /// <summary>
        /// 获取配置项  视频录制延长时间
        /// </summary>
        private double GetVideoExtensionTime()
        {
            return _checkConfigValueInfo == null ? 0 : _checkConfigValueInfo.videoExtensionTimeSeconds;
        }

        /// <summary>
        /// 获取分组相关的配置项
        /// </summary>
        private OnWallBizConfigInfo GetOnWallBizConfig(long pkhId)
        {
            var configBr = configOutboundController.GetOnWallBizConfig(pkhId);
            if (!configBr.hasEntity())
            {
                SetSystemMessage(configBr);
                return null;
            }

            return configBr.ObjectList[0] as OnWallBizConfigInfo;
        }

        /// <summary>
        /// 获取保留LPN配置的值
        /// </summary>
        private string GetPickUpRetainLpnEnumStr()
        {
            if (_bizConfigInfo == null)
                return "";
            return _bizConfigInfo.pickUpRetainLpnEnumVo.ToString();
        }
    }

    public class MbCellInfo
    {
        //格子号
        public long CellIndex { get; set; }

        //计划量
        public double QtyAllocatedUom { get; set; }

        //已上架量
        public double QtyOutputboxUom { get; set; }

        //外部取消标志
        public bool IsOutCancle { get; set; }

        //该格子含有的全部SKU及其数量(包括 DWork id=0 和 != 0 的总和)
        public List<CellSkuInfo> CellAllSkuInfoList { get; set; }

        //该格子含有的有效的SKU及其数量
        public List<CellSkuInfo> CellValidSkuInfoList { get; set; }

        //LPN扫描时，该LPN所在的格子有效的SKU及其数量
        public List<CellSkuInfo> CellValidSkuInfoListByLPN { get; set; }

        //格子已经投递过装箱单
        public bool AlreadyScanSoCode { get; set; }

        //格子中订单Id
        public long CellSohId { get; set; }
    }

    public class CellSkuInfo
    {
        public string SkuCode { get; set; }

        public string SkuEanCode { get; set; }

        public string SkuName { get; set; }

        //该分组中此SKU已拣数量
        public double QtyAlreadyPickup { get; set; }

        //该分组中此SKU全部的数量
        public double QtyAllocated { get; set; }
    }

    /// <summary>
    /// 扫描信息
    /// </summary>
    public class SkuScanDetail
    {
        //格子号
        public long CellIndex { get; set; }

        //货物代码
        public string SkuCode { get; set; }

        //货物名称
        public string SkuName { get; set; }

        //数量
        public double Qty { get; set; }

        public string SKUEanCode { get; set; }

        public string SKUEanCode2 { get; set; }

        public string SKUEanCode3 { get; set; }

        public string SKUEanCode4 { get; set; }

        public string SKUEanCode5 { get; set; }

        public string SKUEanCode6 { get; set; }
    }

    //排序
    public class CellListSortByIndexm : IComparer<MbCellInfo>
    {
        int IComparer<MbCellInfo>.Compare(MbCellInfo x, MbCellInfo y)
        {
            return ((new CaseInsensitiveComparer()).Compare(x.CellIndex, y.CellIndex));
        }
    }

    //排序
    public class ScanDetailsListSortByCellIndexm : IComparer<SkuScanDetail>
    {
        int IComparer<SkuScanDetail>.Compare(SkuScanDetail x, SkuScanDetail y)
        {
            return ((new CaseInsensitiveComparer()).Compare(x.CellIndex, y.CellIndex));
        }
    }
}

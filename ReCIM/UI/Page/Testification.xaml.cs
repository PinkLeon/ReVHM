using AOI.Model;
using AOIProject;
using Core.Implementation;
using Core.Interface;
using Core.Model;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace VHM
{
    /// <summary>
    /// Testification.xaml 的互動邏輯
    /// </summary>
    public partial class Testification : Window
    {
        ILightControl lightControl;

        private string Recipe;

        private string type;

        //共用變數
        private ICoreParameter _coreParameter;

        //AOI資料層    
        private AOIContext aOIContext;

        private HSmartWindowControlWPF hSmartWindowFront;
        private HSmartWindowControlWPF hSmartWindowBack;

        public ObservableCollection<DisplayItem> MeasureResults { get; set; }

        //正面檢測(抽像工廠)(Action是抽像類別)(產生的檢測方法依據抽象工廠) 
        private Core.Interface.Action frontAction;

        private int pieceNumber = 1;

        private bool StopProcessing = false;

        private bool FrontRunning = false;

        private bool BackRunning = false;

        private FlowProcess flowProcess;
        public HObject FrontImage { get; set; }

        public HObject BackImage { get; set; }

        private List<TestSpectation> specfication { get; set; }
        /// <summary>
        /// 目標:
        /// 1.正面,單一算法
        /// 1.1 正面,單算法,單片作業 --> 完成
        /// 1.2 正面,單算法,多片作業
        /// 2.正面,多算法
        /// 3.正+背面,單一算法  --> 完成
        /// 4.正+背面,多算法
        /// </summary>

        public Testification()
        {
            InitializeComponent();

            this.Loaded += UserControl_Loaded;
        }

        public Testification(ICoreParameter coreParameter)
        {
            InitializeComponent();
            _coreParameter = coreParameter;
            flowProcess = new FlowProcess(_coreParameter);
            this.Loaded += (s, e) => UserControl_Loaded(s, e);
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            hSmartWindowFront = new HSmartWindowControlWPF();
            hSmartWindowBack = new HSmartWindowControlWPF();
            HalconHost1.Child = hSmartWindowFront;
            HalconHost2.Child = hSmartWindowBack;

            //_coreParameter是共用變數,AOIContext 是 AOI資料層
            aOIContext = new AOIContext(FrontImage, BackImage, _coreParameter);
            //正面檢測(抽像工廠)(Action是抽像類別)(產生的檢測方法依據抽象工廠)  recipe,type,應該包在coreparameter裡?
            frontAction = new FrontAction(new FrontInspection(_coreParameter), Recipe, type, _coreParameter);

            //AOICore是共用屬性
            AOICore aOICore = new AOICore();
            aOICore.Thickness = 25;

            //step 6:檢測 AOICore 給AOI專用資料層  0402原本是從處方那裡選 
            specfication = aOIContext.InitializeAOIParameter(aOICore, "0402");


        }


        private async void GoButton_Click(object sender, RoutedEventArgs e)
        {
            //step 1: 檢查是否初始化-確認有連通plc,光源有連接
            //Go button UI thread ,要用非同步執行while迴圈 不然會卡死
            //讓正面反面接續跑
            await Task.Run(async () => await RunSwitch());

            //ActionStart();

            //step 2: 檢查是否選擇處方

            //step 3: 取得處方資料做更新開始記錄

            //step 4: 開始檢測

        }

        private async Task RunSwitch()
        {
            while (true)
            {
                if (StopProcessing)
                {
                    break;
                }

                BackRunning = false;
                FrontRunning = true;
                var runBack = await ActionStart();

                await ActionStop(runBack);
            }

        }

        /// <summary>
        ///反覆做正反面動作(多執行緒)(非同步平行作業)
        /// </summary>
        private async Task<bool> ActionStart()
        {
            //正面量測動作
            bool result = await Task.Run(async () =>
            {
                return await RunFront();
            });

            await Task.Delay(5000);
            return result;
        }

        private async Task ActionStop(bool result)
        {
            //正面動作
            FrontRunning = false;
            BackRunning = true;

            bool reWork = false;
            if (result)
            {
                //反面量測動作
                await Task.Run(async () =>
                 {
                     reWork = await RunBack();
                 });

            }
            else
            {
                //背面站之後的動作
                await Task.Run(async () =>
                {
                    await NGRun(reWork);
                });
            }

            await Task.Delay(5000);
        }

        /// <summary>
        /// 正面檢測流程
        /// </summary>
        private async Task<bool> RunFront()
        {
            var list = new List<DisplayItem>();
            list.Clear();
            // 於 UI 建立 ObservableCollection 並綁定（確保 UI 執行緒建立集合）
            bool feedback = false;

            //此專案,用工廠方法和材料工廠好像有點過於複雜
            //應該只要將影像 分成  是/否 為模擬模式
            //1.光源和相機都只要 用介面 再實作
            //2.正背面流程 也用介面,再實作,
            //正背面流程有共同行為(和plc通訊,開燈,取像,關燈) 但又有不同的行為(檢測算法,判斷標準)
            //所以可以將正背面流程分成兩個類別,但共用一個介面,裡面有共同行為的定義,再實作不同的行為
            //正背面模擬時,可以實作同一個介面,裡面定義正背面共同行為,再實作不同的行為,但又不須和plc通訊,開燈,取像,關燈
            //會變的部分要包起來

            // 1.和plc通訊,接收到正面訊號後,開始啟動線掃描,開啟燈光,取像,關閉燈源
            //利用 介面 + 策略模式 - 將正面 背面行為放一組(plc通訊,開燈,取像,關燈) 或 正背面分開 但就沒共同行為?
            // Action介面 實作,模擬及實際的正反面動作 ,每個正反面動作有許多組合 設計模式P.160
            // Inspection介面 底下有 產生動方法, 正反面檢測要實作此介面 
            // Inspection 依賴 動作介面 正反面檢測,再實作 各種動作

            //正面站(PizzaStore) - 做正面動作 (Action)才對 
            // createAction style是 front (工廠方法) --> 不須硬套,這邊只需要用想要style的Action 即可

            ////_coreParameter是共用變數,AOIContext 是 AOI資料層
            //aOIContext = new AOIContext(_coreParameter);
            ////正面檢測(抽像工廠)(Action是抽像類別)(產生的檢測方法依據抽象工廠)  recipe,type,應該包在coreparameter裡?
            //Core.Interface.Action frontAction = new FrontAction(new FrontInspection(_coreParameter), Recipe, type, _coreParameter);
            //應該只要呼叫prepare,其他應該都在裡面做完
            //frontAction.prepare();
            //prepare裡應該包含step 1~5
            //step 1:開燈  燈光這類,可以在Global做,隨程式開啟和關閉
            //step 2:呼叫擷取卡,做線掃描
            //step 3:移動平台至拍照位 讓平台開始移動，讓Line Scan相機取像
            //step 4:取像
            flowProcess.Create();
            flowProcess.FrontProcess();
            FrontImage = flowProcess.fAction.GetImage();
            aOIContext.AOICore.FrontImage = FrontImage;

            //顯示影像
            await DisplayImage(FrontImage, hSmartWindowFront);
            //step 5:關燈
            _coreParameter.fLControl.TurnOFF();

            AOIPerform aOIPerform = new AOIPerform(aOIContext, _coreParameter);
            await aOIPerform.Start();

            var result = aOIContext.Result;

            ObservableCollection<DisplayItem> MeasureResults = null;

            // 一定在 UI thread 建立
            Dispatcher.Invoke(() =>
            {
                MeasureResults = new ObservableCollection<DisplayItem>();

                MainGrid.ItemsSource = MeasureResults;
            });

            var data = await DisplayResult(result.Where(n => n.DefectValues != null).ToList());

            // 回 UI thread 更新
            Dispatcher.Invoke(() =>
            {
                foreach (var item in data)
                {
                    MeasureResults.Add(item);
                }
            });

            //顯示overlay影像
            await DisplayImage(aOIContext.OverlayImage.First(), hSmartWindowFront);

            //step 8:移動至下一位置
            pieceNumber++;

            //檢測必備流程

            // 9.由取得影像及檢測標準及公差,比較後判斷,規格是否在合格範圍並在畫面顯示正面結果
            if (data.First().Judgement == "NG")
            {
                feedback = false;
            }
            else
            {
                feedback = true;
            }

            // 10.若NG,儲存影像,不測背面,傳給plc檢測完畢

            //step 11:由正面拍照位至翻轉位
            //await Global.PLCTFrontUse.WriteMR("12", true);
            //STEP 12:由翻轉位至背面拍照位
            //await Global.PLCTFrontUse.WriteMR("12", true);


            return feedback;
        }

        /// <summary>
        /// 背面檢測流程
        /// 動作基本上和正面類似,共用frontAction,但inspection要分開
        /// </summary>
        private async Task<bool> RunBack()
        {
            var list = new List<DisplayItem>();
            list.Clear();
            // 於 UI 建立 ObservableCollection 並綁定（確保 UI 執行緒建立集合）
            bool backJudge = false;

            // 1.和plc通訊,接收到背面訊號後,開啟燈光,取像,關閉燈源
            flowProcess.Create();
            flowProcess.BackProcess();
            // 2.由取得影像及檢測標準及公差,比較後判斷,規格是否在合格範圍,並在畫面顯示背面結果
            BackImage = flowProcess.bAction.GetImage();
            aOIContext.AOICore.BackImage = BackImage;
            //顯示影像
            await DisplayImage(BackImage, hSmartWindowBack);
            //step 5:關燈
            _coreParameter.bLControl.TurnOFF();

            AOIPerform aOIPerform = new AOIPerform(aOIContext, _coreParameter);
            await aOIPerform.Start();

            var result = aOIContext.Result;

            ObservableCollection<DisplayItem> MeasureResults = null;

            // 一定在 UI thread 建立
            Dispatcher.Invoke(() =>
            {
                MeasureResults = new ObservableCollection<DisplayItem>(); //Binding 關鍵 :配合DataGridTextColumn

                MainGrid.ItemsSource = MeasureResults;
            });

            var data = await DisplayResult(result.Where(n => n.DefectValues != null).ToList());

            // 回 UI thread 更新
            Dispatcher.Invoke(() =>
            {
                foreach (var item in data)
                {
                    MeasureResults.Add(item); //可以再追加區分正反面
                }
            });

            //顯示overlay影像
            await DisplayImage(aOIContext.OverlayImage.Last(), hSmartWindowBack);
            if (data.Last().Judgement == "NG")
            {
                return false;
            }
            else
            {
                backJudge = true;
            }

            // 3.若NG,儲存影像,傳給plc檢測完畢
            if (backJudge)
            {
                await NGRun(false);
                return false;
            }


            //return false;
            return backJudge;
        }

        private async Task<List<DisplayItem>> DisplayResult(List<Result> results)
        {
            return await Task.Run(() =>
            {
                var list = new List<DisplayItem>();
                foreach (var item in results)
                {
                    if (item != null)
                    {
                        var judmnt = item?.Judge(new List<string> { item?.DefectValues?.D.ToString() }
                        , specfication.Where(n => n.item_name == item.ItemName).ToList());
                        var type = _coreParameter.ConverToleranceType(specfication.Where(n => n.item_name == item.ItemName)
                            .First().tolerance_type);

                        list.Add(
                            new DisplayItem()
                            {
                                MeasuredValue = item.DefectValues.D.ToString(),
                                ItemName = item.ItemName,
                                ToleranceType = type,
                                Spec = specfication.Where(n => n.item_name == item.ItemName).First().standardvalue.ToString(),
                                Tolerance = specfication.Where(n => n.item_name == item.ItemName).First().tolerance.ToString(),
                                No = pieceNumber,
                                Judgement = judmnt.Last() ? "OK" : "NG"
                            });

                    }
                    else
                    {
                        return new List<DisplayItem>();
                    }

                }

                //將量測值由pixel轉為實際單位
                _coreParameter.ConvertRealDistance(list);
                return list;
            });

        }
        /// <summary>
        /// 正面ng時,不跑背面的檢測流程
        /// </summary>
        private async Task<bool> NGRun(bool rework)
        {
            if (rework)
            {
                //await Global.PLCTFrontUse.WriteMR("12", true);
                return true;
            }

            //STEP 3:背面不拍照或結束拍照後,移到ng軌道,重工或良品軌道
            //await Global.PLCTFrontUse.WriteMR("12", true);

            return true;

        }


        private async Task DisplayImage(HObject hObject, HSmartWindowControlWPF W1)
        {
            if (hObject != null && hObject.IsInitialized())
                //HOperatorSet.ReadImage(out Hoimage, imgPath);
                await UpdateHalconWindow1(hObject, W1);
        }

        public async Task UpdateHalconWindow1(HObject hObject, HSmartWindowControlWPF W1)
        {


            await W1.Dispatcher.InvokeAsync(() =>
            {
                W1.HalconWindow.ClearWindow();
                FitImage(hObject, W1);
                //if (W2 != null)
                //{
                //    await W2.Dispatcher.Invoke(async () =>
                //    {
                //        W2.HalconWindow.ClearWindow();
                //        FitImage(Hoimage, W2);
                //    });

                //}

            });


        }

        public void FitImage(HObject ho_Image, HSmartWindowControlWPF HWC)
        {
            // 调用重载方法，并传入默认值
            FitImage(ho_Image, HWC.ActualWidth, HWC.ActualHeight, HWC);
        }

        public void FitImage(HObject ho_Image, HTuple hv_WinWidth, HTuple hv_WinHeight, HSmartWindowControlWPF HWC)
        {
            // Local iconic variables 

            HObject ho_ZoomedImage;

            // Local control variables 

            HTuple hv_Width = new HTuple(), hv_Height = new HTuple();
            HTuple hv_ScaleWidth = new HTuple(), hv_ScaleHeight = new HTuple();
            HTuple hv_Scale = new HTuple();

            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_ZoomedImage);

            //獲取圖像尺寸
            HOperatorSet.GetImageSize(ho_Image, out hv_Width, out hv_Height);

            if ((int)(new HTuple(hv_Width.TupleNotEqual(new HTuple()))) != 0 || (int)(new HTuple(hv_Height.TupleNotEqual(new HTuple()))) != 0)
            {
                //獲取窗口尺寸
                double windowWidth = hv_WinWidth;
                double windowHeight = hv_WinHeight;
                //var w1 = HWDefect.WindowSize.Width;
                //var h1 = HWDefect.WindowSize.Height;
                // 計算縮放比例
                double widthRatio = windowWidth / (double)hv_Width;
                double heightRatio = windowHeight / (double)hv_Height;
                double scalingFactor = Math.Min(widthRatio, heightRatio);

                // 設置窗口縮放後的顯示區域
                double newWidth = (double)(windowWidth * scalingFactor);
                double newHeight = (double)(windowHeight * scalingFactor);

                // 用於計算影像在視窗中的偏移量，以確保影像居中顯示
                int xOffset = (int)((windowWidth - newWidth) / 2);
                int yOffset = (int)((windowHeight - newHeight) / 2);

                // 将 SetPart 参数类型明确为 int
                //HOperatorSet.SetPart(HWDefect.HalconWindow, 0, 0, (int)windowHeight - 1, (int)windowWidth - 1);
                //HWC.HalconWindow.ClearWindow();

                // 将 SetWindowExtents 参数类型明确为 int
                HOperatorSet.SetWindowExtents(HWC.HalconWindow, xOffset, yOffset, (int)newWidth, (int)newHeight);
                //// 显示缩放后的图像
                //HOperatorSet.ZoomImageFactor(ho_Image, out HObject Hout, 1, 1, "constant");  

                // 显示图像
                HWC.HalconWindow.DispObj(ho_Image);
            }



            ho_ZoomedImage.Dispose();
            hv_Width.Dispose();
            hv_Height.Dispose();
            hv_ScaleWidth.Dispose();
            hv_ScaleHeight.Dispose();
            hv_Scale.Dispose();

            return;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopProcessing = true;
        }
    }
}

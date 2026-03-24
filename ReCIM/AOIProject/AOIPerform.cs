using AOI.Model;
using Core.Interface;
using HalconDotNet;


namespace AOIProject
{
    /// <summary>
    /// AOI流程層
    /// </summary>
    public class AOIPerform
    {
        //依賴注入: 方便外部測試,不寫死
        private readonly AOIContext _context;

        private ICoreParameter _parameters;
        /// <summary>
        /// 呼叫時,給影像,即可執行AOI檢測,但檢測結果和缺陷圖片怎傳出去?
        /// 這邊放資料(context),流程放其他地方?
        /// 資料層:AIContext(model) context 放各種需用到資料型態
        /// 流程層:將資料傳到輸入 Measurement.DO(context)
        /// </summary>
        /// <param name="hobject">可放正面或背面</param>
        public AOIPerform(AOIContext context, ICoreParameter parameters)
        {
            _context = context;
            _context.AOICore = context.AOICore;
            _context.Measurements = context.Measurements;
            _parameters = parameters;
        }

        public async Task Start()
        {
            _context.Result.Clear();
            //1.找出影像是正面或背面(需要?,若只傳一張影像,可由尺寸看是正面還是反面,若傳兩張怎分?

            //2.影像前處理(後續可統一優化,做前處理)

            //根據各算法的interval執行,所以要適時移除特定算法?

            try
            {
                //每算法執行時間限制不超過設定秒數

                //3.DO 執行算法,
                //_context.frontResult = 1;
                //若只有正面影像,背面的要不跑

                foreach (var ele in _context.Measurements)
                {

                    //重新開始計時設定，否則下一次會直接timeout
                    CancellationTokenSource cts = new CancellationTokenSource();

                    //設定演算法Timeout秒數，若超過就直接算NG
                    cts.CancelAfter(TimeSpan.FromSeconds(20));
                    try
                    {
                        //裡面做的事:
                        //1.分離前景背景
                        //2.找出ROI
                        //3.找出各項目量測值
                        //4.找出超過標準值+公差?
                        //5.找出NG處ROI,並圈出
                        //6.在原圖上,疊出
                        var task = Task.Run(() =>
                        {
                            //是不是應該用static 建立_context就好,不用傳來傳去,testification建立再傳入即可?

                            //正反面需分開? 還是其實只要排序即可?
                            return ele.Do(_parameters, _context.AOICore);
                        }, cts.Token);

                        var result = await task;

                        _context.Result.Add(result);

                        //任務完成或超時就結束
                        await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));

                        if (cts.IsCancellationRequested)
                        {
                            throw new OperationCanceledException();
                        }

                        await task;
                    }
                    catch (OperationCanceledException)
                    {
                        //HOperatorSet.GenEmptyRegion(out DefectRegion);
                        ele.DefectValue = 999;
                        //Class = 1;
                    }
                    catch (Exception ex)
                    {
                        //HOperatorSet.GenEmptyRegion(out DefectRegion);
                        ele.DefectValue = 999;
                        //Class = 1;
                    }


                    var defectRegions = _context.Result.Select(n => n.DefectRegions).ToList();
                    var defectValues = _context.Result.Select(n => n.DefectValues).ToList();
                    var defectClass = _context.Result.Select(n => n.ClassNumber).ToList();

                    _context.CurrentImage = ele.Image;
                    //統整檢測結果
                    //HOperatorSet.HeightWidthRatio(DefectRegion, out HTuple H, out HTuple W, out HTuple R);
                    //defectRegions.Add(ele.DefectRegionImage);
                    //defectValues.Add(ele.DefectValue);
                    //defectClass.Add(ele.classNumber - 1);
                    if (defectRegions.First() != null && defectRegions.First().IsInitialized()
                        && _context.CurrentImage != null && _context.CurrentImage.IsInitialized() && defectClass.ToArray().Any())
                        //將缺陷區域標記在影像中 -->為何又要缺陷影像,又要標記?
                        _context.OverlayImage = ProcessOverlayImage(defectRegions, _context.CurrentImage, new HTuple(defectClass.ToArray()));
                }

                //5.原圖標出缺陷位置及分類 一樣把標出缺陷位置的疊圖,傳到Result
                //有可能多重缺陷? 還是測出一個就不繼續? 多重比較保險

            }
            catch (Exception ex)
            {

            }


        }

        /// <summary>
        /// 將影像加上Overlay記號
        /// </summary>
        /// <param name="ho_RegionBoundary"></param>
        /// <param name="hoimage"></param>
        /// <param name="Farea"></param>
        /// <returns></returns>
        private HObject ProcessOverlayImage(List<HObject> ho_RegionBoundary, HObject hoimage, HTuple Class)
        {
            //顏色設定
            List<string> ListColors = new List<string>()
            { "red", "green", "blue", "cyan", "magenta", "yellow", "medium slate blue", "coral", "dark olive green", "pink", "cadet blue" };

            HOperatorSet.CountChannels(hoimage, out HTuple Channels);
            HObject ho_Image1, ho_Image2, ho_Image3, FinalImage;

            try
            {
                if (Channels == 1)
                {
                    HOperatorSet.CopyImage(hoimage, out ho_Image1);
                    HOperatorSet.CopyImage(hoimage, out ho_Image2);
                    HOperatorSet.CopyImage(hoimage, out ho_Image3);
                    HOperatorSet.CopyImage(hoimage, out FinalImage);
                    HOperatorSet.Compose3(ho_Image1, ho_Image2, ho_Image3, out FinalImage);
                    ho_Image1.Dispose(); ho_Image2.Dispose(); ho_Image3.Dispose();
                }
                else
                {
                    HOperatorSet.CopyImage(hoimage, out FinalImage);
                }

                for (int i = 0; i < Class.Length; i++)
                {
                    HOperatorSet.AreaCenter(ho_RegionBoundary[i], out HTuple Area, out HTuple row, out HTuple col);
                    if (Area > 0)
                    {
                        color_string_to_rgb(ListColors[Class[i]], out HTuple RGB);
                        HOperatorSet.PaintRegion(ho_RegionBoundary[i], FinalImage, out FinalImage, RGB, "fill");
                    }

                }

                return FinalImage;
            }
            catch (Exception ex)
            {
                string Error = ex.Message;
                FinalImage = null;
                return FinalImage;
            }

        }

        /// <summary>
        /// 將顏色文字轉成RGB數值
        /// </summary>
        /// <param name="hv_Color"></param>
        /// <param name="hv_RGB"></param>
        public void color_string_to_rgb(HTuple hv_Color, out HTuple hv_RGB)
        {



            // Local iconic variables 

            HObject ho_Rectangle, ho_Image;

            // Local control variables 

            HTuple hv_WindowHandleBuffer = new HTuple();
            HTuple hv_Exception = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Rectangle);
            HOperatorSet.GenEmptyObj(out ho_Image);
            hv_RGB = new HTuple();
            try
            {
                hv_WindowHandleBuffer.Dispose();
                HOperatorSet.OpenWindow(0, 0, 1, 1, 0, "buffer", "", out hv_WindowHandleBuffer);
                HOperatorSet.SetPart(hv_WindowHandleBuffer, 0, 0, -1, -1);
                ho_Rectangle.Dispose();
                HOperatorSet.GenRectangle1(out ho_Rectangle, 0, 0, 0, 0);
                try
                {
                    HOperatorSet.SetColor(hv_WindowHandleBuffer, hv_Color);
                }
                // catch (Exception) 
                catch (HalconException HDevExpDefaultException1)
                {
                    HDevExpDefaultException1.ToHTuple(out hv_Exception);
                    hv_Exception.Dispose();
                    hv_Exception = "Wrong value of control parameter Color (must be a valid color string)";
                    throw new HalconException(hv_Exception);
                }
                HOperatorSet.DispObj(ho_Rectangle, hv_WindowHandleBuffer);
                ho_Image.Dispose();
                HOperatorSet.DumpWindowImage(out ho_Image, hv_WindowHandleBuffer);
                HOperatorSet.CloseWindow(hv_WindowHandleBuffer);
                hv_RGB.Dispose();
                HOperatorSet.GetGrayval(ho_Image, 0, 0, out hv_RGB);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_RGB = hv_RGB + (
                            (new HTuple(0)).TupleConcat(0)).TupleConcat(0);
                        hv_RGB.Dispose();
                        hv_RGB = ExpTmpLocalVar_RGB;
                    }
                }
                ho_Rectangle.Dispose();
                ho_Image.Dispose();

                hv_WindowHandleBuffer.Dispose();
                hv_Exception.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_Rectangle.Dispose();
                ho_Image.Dispose();

                hv_WindowHandleBuffer.Dispose();
                hv_Exception.Dispose();

            }
        }

    }
}

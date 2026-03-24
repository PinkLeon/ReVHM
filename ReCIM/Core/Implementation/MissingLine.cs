using Core.Interface;
using Core.Model;
using HalconDotNet;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Implementation
{
    public class MissingLine : FrontMeasurement
    {
        //private ICoreParameter _CoreParameter;
        //public AOICore _aOICore;
        //public MissingLine(ICoreParameter coreParameter, AOICore aOICore) : base(coreParameter, aOICore)

        //{
        //    _CoreParameter = coreParameter;
        //    _aOICore = aOICore;
        //}

        public MeasurementTable measurement { get; set; }

        //private CoreParameter coreParameter { get; set; }


        public async override Task<Result> Do(ICoreParameter coreParameter, AOICore aOICore)
        {
            // 先呼叫 FrontMeasurement.Do，確保 Image 被設定
            var baseResult = await base.Do(coreParameter, aOICore);
            Result result = new Result();
            result.ClassNumber = 1;
            result.ItemName = "MissingLine";
            AOICore Data = aOICore;
            var data = aOICore.TestSpectations.Where(n => n.item_name == result.ItemName).ToList().First();
            result.DefectRegions = new HObject();
            result.DefectValues = new HTuple();
            result.CurrentImage = new HObject();
            HObject Regions = new HObject();
            HObject DefectRegions = new HObject();
            HObject ho_Image = new HObject();
            HTuple Values = new HTuple();
            HTuple hv_GridWidthPixel = new HTuple();
            HTuple hv_GridHeightPixel = new HTuple();
            HTuple DefectValues = new HTuple();

            if (Image != null && Image.IsInitialized())
            {
                result.CurrentImage = Image;

                try
                {

                    //降影像解析度 要丟原圖
                    HOperatorSet.ZoomImageFactor(Image, out ho_Image, Data.ZoomFactor, Data.ZoomFactor, "constant");

                    //計算格線寬、高的Pixel
                    CalculateGridPixel(Data.GridWidth, Data.GridHeight, Data.PixelSize, Data.ZoomFactor, out hv_GridWidthPixel, out hv_GridHeightPixel);
                }
                catch (Exception ex)
                {
                    await coreParameter.WriteLog("HALCON", "MissingLine Error", ex.Message + ex.StackTrace);
                }


                try
                {
                    if (0.46 <= Data.Thickness && Data.Thickness <= 0.54)
                    {
                        DetectMissingLine_1206(ho_Image, out Regions, hv_GridWidthPixel, hv_GridHeightPixel, Data.ZoomFactor, 0.3, out Values);
                    }
                    else if ((0.42 <= Data.Thickness && Data.Thickness <= 0.45))
                    {
                        DetectMissingLine_0805(ho_Image, out Regions, hv_GridWidthPixel, hv_GridHeightPixel, Data.ZoomFactor, 0.1, out Values);
                    }
                    else if ((0.36 <= Data.Thickness && Data.Thickness <= 0.41))
                    {
                        DetectMissingLine_0603(ho_Image, out Regions, hv_GridWidthPixel, hv_GridHeightPixel, Data.ZoomFactor, 0.1, out Values);
                    }
                    else if ((0.22 <= Data.Thickness && Data.Thickness <= 0.27))
                    {
                        DetectMissingLine_0402(ho_Image, out Regions, hv_GridWidthPixel, hv_GridHeightPixel, Data.ZoomFactor, 0.4, out Values);
                    }
                    else
                    {
                        Regions = null;
                    }
                }
                catch (Exception ex)
                {
                    await coreParameter.WriteLog("HALCON", "MissingLine Error", ex.Message + ex.StackTrace);
                    HOperatorSet.GenEmptyRegion(out Regions);
                    Values = 999;
                }


                //根據公差做篩選
                FilterResult(Regions, out HObject ZoomRegion, data.tolerance_type, Values, data.tolerance, out DefectValues);

                //HObject ho_MinDefectImage = new HObject();
                //HObject ho_MaxDefectImage = new HObject();

                if (DefectValues.Length != 0 && (int)(new HTuple(((DefectValues.TupleSelect(0))).TupleNotEqual(999))) != 0)
                {
                    //輸出最大與最小NG區域影像
                    FilterMinMaxResult(ho_Image, Regions, ZoomRegion, out HObject ho_MinDefectImage, out HObject ho_MaxDefectImage, out ZoomRegion, data.tolerance_type, Values, data.tolerance);
                    FrontMinDefectImage = ho_MinDefectImage;
                    //if (FrontMinDefectImage.IsInitialized())
                    //    HOperatorSet.WriteImage(FrontMinDefectImage, "png", 0, @"C:\Users\User\Desktop\FrontMinDefectImage.png");
                    FrontMaxDefectImage = ho_MaxDefectImage;
                    //if (FrontMaxDefectImage.IsInitialized())
                    //    HOperatorSet.WriteImage(FrontMaxDefectImage, "png", 0, @"C:\Users\User\Desktop\FrontMaxDefectImage.png");

                }
                else
                {
                    HOperatorSet.GenEmptyRegion(out Regions);
                    //HOperatorSet.GenEmptyObj(out HObject ho_MaxDefectImage);
                    //HOperatorSet.GenEmptyObj(out HObject ho_MinDefectImage);
                    HObject ho_MaxDefectImage = new HObject();
                    HObject ho_MinDefectImage = new HObject();
                    FrontMaxDefectImage = ho_MaxDefectImage;
                    FrontMinDefectImage = ho_MinDefectImage;
                }

                //將漏線區域轉回原影像尺寸區域
                HOperatorSet.ZoomRegion(ZoomRegion, out DefectRegions, 1 / Data.ZoomFactor, 1 / Data.ZoomFactor);
                ZoomRegion.Dispose();

                //將結果已result傳出或以context傳出難以抉擇?
                var task = await Task.Run(async () =>
                {
                    //不良區域
                    result.DefectRegions = DefectRegions;
                    //不良數值
                    result.DefectValues = DefectValues;
                    return result;
                });

                return task;
            }

            return new Result();
        }

        /// <summary>
        /// 針對1206的Line Scan影像找出斷線區域及長度
        /// </summary>
        /// <param name="ho_Image00011"></param>
        /// <param name="ho_RegionBorder1"></param>
        /// <param name="hv_IgnorePixelSize"></param>
        /// <param name="hv_GridWidthPixel"></param>
        /// <param name="hv_GridHeightPixel"></param>
        /// <param name="hv_ZoomFactor"></param>
        /// <param name="hv_FinalResult"></param>

        /// <summary>
        /// 針對1206的Line Scan影像找出斷線區域及長度
        /// </summary>
        /// <param name="ho_Image00011"></param>
        /// <param name="ho_RegionBorder1"></param>
        /// <param name="hv_IgnorePixelSize"></param>
        /// <param name="hv_GridWidthPixel"></param>
        /// <param name="hv_GridHeightPixel"></param>
        /// <param name="hv_ZoomFactor"></param>
        /// <param name="hv_FinalResult"></param>
        private void DetectMissingLine_1206(HObject ho_Image00011, out HObject ho_RegionBorder1,
      HTuple hv_GridWidthPixel, HTuple hv_GridHeightPixel, HTuple hv_ZoomFactor, HTuple hv_Factor,
      out HTuple hv_FinalResult)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_Region, ho_RegionTrans, ho_RegionBorder;
            HObject ho_ImageReduced, ho_RegionTrans2, ho_RegionErosion2;
            HObject ho_ImageReduced1, ho_ImageMean, ho_RegionDynThresh1;
            HObject ho_ImageReduced2, ho_RegionDynThresh, ho_RegionDilation;
            HObject ho_RegionUnion, ho_ConnectedRegions, ho_SelectedRegions;
            HObject ho_RegionFillUp, ho_RegionDifference, ho_AllRegions;
            HObject ho_RegionTrans4, ho_RegionErosion1, ho_RegionBorder3;
            HObject ho_RegionUnion5, ho_ConnectedRegions6, ho_InnerRegions;
            HObject ho_InnerMissingLineRegions, ho_OuterMissingLineRegions;
            HObject ho_AllRegionsUnion, ho_InnerRegionsUnion, ho_OuterRegions;
            HObject ho_OuterRegionLines, ho_RegionLineSelected = null;
            HObject ho_RegionUnion6 = null, ho_ConnectedRegions2 = null;
            HObject ho_RegionOut = null, ho_RegionIntersection1 = null;
            HObject ho_ConnectedRegions3 = null, ho_SortedRegions2 = null;
            HObject ho_ObjectsReduced = null, ho_RegionUnion7 = null, ho_RegionUnion2;
            HObject ho_MissingLineRegion;

            // Local control variables 

            HTuple hv_Width = new HTuple(), hv_Height = new HTuple();
            HTuple hv_Min1 = new HTuple(), hv_Max1 = new HTuple();
            HTuple hv_Range2 = new HTuple(), hv_Energy1 = new HTuple();
            HTuple hv_Correlation1 = new HTuple(), hv_Homogeneity1 = new HTuple();
            HTuple hv_Contrast1 = new HTuple(), hv_Area6 = new HTuple();
            HTuple hv_Row6 = new HTuple(), hv_Column6 = new HTuple();
            HTuple hv_Area2 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Column2 = new HTuple(), hv_Index = new HTuple();
            HTuple hv_Number = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_RegionBorder1);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans2);
            HOperatorSet.GenEmptyObj(out ho_RegionErosion2);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced1);
            HOperatorSet.GenEmptyObj(out ho_ImageMean);
            HOperatorSet.GenEmptyObj(out ho_RegionDynThresh1);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced2);
            HOperatorSet.GenEmptyObj(out ho_RegionDynThresh);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference);
            HOperatorSet.GenEmptyObj(out ho_AllRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans4);
            HOperatorSet.GenEmptyObj(out ho_RegionErosion1);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder3);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion5);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions6);
            HOperatorSet.GenEmptyObj(out ho_InnerRegions);
            HOperatorSet.GenEmptyObj(out ho_InnerMissingLineRegions);
            HOperatorSet.GenEmptyObj(out ho_OuterMissingLineRegions);
            HOperatorSet.GenEmptyObj(out ho_AllRegionsUnion);
            HOperatorSet.GenEmptyObj(out ho_InnerRegionsUnion);
            HOperatorSet.GenEmptyObj(out ho_OuterRegions);
            HOperatorSet.GenEmptyObj(out ho_OuterRegionLines);
            HOperatorSet.GenEmptyObj(out ho_RegionLineSelected);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion6);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions2);
            HOperatorSet.GenEmptyObj(out ho_RegionOut);
            HOperatorSet.GenEmptyObj(out ho_RegionIntersection1);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions3);
            HOperatorSet.GenEmptyObj(out ho_SortedRegions2);
            HOperatorSet.GenEmptyObj(out ho_ObjectsReduced);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion7);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion2);
            HOperatorSet.GenEmptyObj(out ho_MissingLineRegion);
            hv_FinalResult = new HTuple();
            //=====找出待測物範圍=====*
            ho_Region.Dispose(); ho_RegionTrans.Dispose(); ho_RegionBorder.Dispose();
            FindFrontRegion(ho_Image00011, out ho_Region, out ho_RegionTrans, out ho_RegionBorder
                );
            ho_ImageReduced.Dispose();
            HOperatorSet.ReduceDomain(ho_Image00011, ho_RegionTrans, out ho_ImageReduced);

            //-----找出板子灰度區間-----*
            ho_RegionTrans2.Dispose();
            HOperatorSet.ShapeTrans(ho_Region, out ho_RegionTrans2, "rectangle1");
            hv_Width.Dispose();
            HOperatorSet.RegionFeatures(ho_RegionTrans2, "width", out hv_Width);
            hv_Height.Dispose();
            HOperatorSet.RegionFeatures(ho_RegionTrans2, "height", out hv_Height);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionErosion2.Dispose();
                HOperatorSet.ErosionRectangle1(ho_RegionTrans2, out ho_RegionErosion2, hv_Width / 15,
                    hv_Height / 15);
            }
            ho_ImageReduced1.Dispose();
            HOperatorSet.ReduceDomain(ho_Image00011, ho_RegionErosion2, out ho_ImageReduced1
                );
            //-----決定Local threshold的Range-----*
            hv_Min1.Dispose(); hv_Max1.Dispose(); hv_Range2.Dispose();
            HOperatorSet.MinMaxGray(ho_ImageReduced1, ho_ImageReduced1, 0, out hv_Min1, out hv_Max1,
                out hv_Range2);

            //-----算出影像對比度-----*
            hv_Energy1.Dispose(); hv_Correlation1.Dispose(); hv_Homogeneity1.Dispose(); hv_Contrast1.Dispose();
            HOperatorSet.CoocFeatureImage(ho_RegionTrans, ho_Image00011, 6, 0, out hv_Energy1,
                out hv_Correlation1, out hv_Homogeneity1, out hv_Contrast1);

            //-----先大略篩出線段區域，以加快local_threshold的速度-----*
            ho_ImageMean.Dispose();
            HOperatorSet.MeanImage(ho_ImageReduced, out ho_ImageMean, 31, 31);
            ho_RegionDynThresh1.Dispose();
            HOperatorSet.DynThreshold(ho_ImageReduced, ho_ImageMean, out ho_RegionDynThresh1,
                5, "dark");
            ho_ImageReduced2.Dispose();
            HOperatorSet.ReduceDomain(ho_ImageReduced, ho_RegionDynThresh1, out ho_ImageReduced2
                );

            //-----Factor: 0.1~2 (鬆~嚴)-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionDynThresh.Dispose();
                HOperatorSet.LocalThreshold(ho_ImageReduced2, out ho_RegionDynThresh, "adapted_std_deviation",
                    "dark", ((new HTuple("mask_size")).TupleConcat("scale")).TupleConcat("range"),
                    ((((30 * hv_ZoomFactor)).TupleConcat((hv_Contrast1 / 10) * hv_Factor))).TupleConcat(
                    hv_Range2 / 2));
            }
            //scale: 調整線段與底色對比，對比高數值越高，反之則反

            //-----4邊往內縮一段不做判斷的區域-----*
            ho_RegionDilation.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionBorder, out ho_RegionDilation, 25, 25);

            //-----合併邊界與二值化結果-----*
            ho_RegionUnion.Dispose();
            HOperatorSet.Union2(ho_RegionDynThresh, ho_RegionDilation, out ho_RegionUnion
                );

            //-----去除線段以外的雜點-----*
            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_RegionUnion, out ho_ConnectedRegions);
            hv_Area6.Dispose(); hv_Row6.Dispose(); hv_Column6.Dispose();
            HOperatorSet.AreaCenter(ho_ConnectedRegions, out hv_Area6, out hv_Row6, out hv_Column6);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_SelectedRegions, "area",
                    "and", 150, (hv_Area6.TupleMax()) + 1);
            }

            //-----將線段填滿-----*
            ho_RegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_SelectedRegions, out ho_RegionFillUp);

            //-----找出填滿區域與二值化區域不同處，即可找出所有方塊區域-----*
            ho_RegionDifference.Dispose();
            HOperatorSet.Difference(ho_RegionFillUp, ho_ConnectedRegions, out ho_RegionDifference
                );

            //-----分離所有方塊區域-----*
            ho_AllRegions.Dispose();
            HOperatorSet.Connection(ho_RegionDifference, out ho_AllRegions);

            //=====四周往內縮一格=====*
            //-----根據基板形狀建立一個矩形-----*
            ho_RegionTrans4.Dispose();
            HOperatorSet.ShapeTrans(ho_Region, out ho_RegionTrans4, "rectangle2");

            //-----將矩形往內縮小-----*
            ho_RegionErosion1.Dispose();
            HOperatorSet.ErosionRectangle1(ho_RegionTrans4, out ho_RegionErosion1, 120, 120);

            //-----找出矩陣縮小後的邊框-----*
            ho_RegionBorder3.Dispose();
            HOperatorSet.Boundary(ho_RegionErosion1, out ho_RegionBorder3, "inner");

            //-----與所有方塊區域合併-----*
            ho_RegionUnion5.Dispose();
            HOperatorSet.Union2(ho_RegionBorder3, ho_RegionDifference, out ho_RegionUnion5
                );

            //-----分離方塊後，將最大面積移除，即可得到內圈方塊-----*
            ho_ConnectedRegions6.Dispose();
            HOperatorSet.Connection(ho_RegionUnion5, out ho_ConnectedRegions6);
            hv_Area2.Dispose(); hv_Row2.Dispose(); hv_Column2.Dispose();
            HOperatorSet.AreaCenter(ho_ConnectedRegions6, out hv_Area2, out hv_Row2, out hv_Column2);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_InnerRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions6, out ho_InnerRegions, "area", "and",
                    1, (hv_Area2.TupleMax()) - 1);
            }

            //=====內圈方塊處理=====*
            //-----找出內圈斷線區域-----*
            ho_InnerMissingLineRegions.Dispose();
            SearchMissingLine(ho_InnerRegions, out ho_InnerMissingLineRegions, hv_GridWidthPixel,
                hv_GridHeightPixel);

            //=====處理外圈方塊區域=====*
            ho_OuterMissingLineRegions.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_OuterMissingLineRegions);

            //-----篩出外圈方塊區域-----*
            ho_AllRegionsUnion.Dispose();
            HOperatorSet.Union1(ho_AllRegions, out ho_AllRegionsUnion);
            ho_InnerRegionsUnion.Dispose();
            HOperatorSet.Union1(ho_InnerRegions, out ho_InnerRegionsUnion);
            ho_OuterRegions.Dispose();
            HOperatorSet.Difference(ho_AllRegionsUnion, ho_InnerRegionsUnion, out ho_OuterRegions
                );

            //-----產生邊界內縮線段-----*
            ho_OuterRegionLines.Dispose();
            FindOuterLines(ho_RegionBorder3, out ho_OuterRegionLines);

            for (hv_Index = 0; (int)hv_Index <= 3; hv_Index = (int)hv_Index + 1)
            {
                //-----對應方向的線段-----*
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_RegionLineSelected.Dispose();
                    HOperatorSet.SelectObj(ho_OuterRegionLines, out ho_RegionLineSelected, hv_Index + 1);
                }

                //-----將四周方格區域與對應的邊合併-----*
                ho_RegionUnion6.Dispose();
                HOperatorSet.Union2(ho_OuterRegions, ho_RegionLineSelected, out ho_RegionUnion6
                    );
                //-----將合併後的區域分開，找出最大面積就是對應邊的方塊區域+線段-----*
                ho_ConnectedRegions2.Dispose();
                HOperatorSet.Connection(ho_RegionUnion6, out ho_ConnectedRegions2);
                ho_RegionOut.Dispose();
                RemoveRegionByArea(ho_ConnectedRegions2, out ho_RegionOut, 15);
                //-----因為方塊區域有合併線段，所以找出與方塊重疊區域，即為對應方塊區域-----*
                ho_RegionIntersection1.Dispose();
                HOperatorSet.Intersection(ho_OuterRegions, ho_RegionOut, out ho_RegionIntersection1
                    );
                //-----將方塊區域連通-----*
                ho_ConnectedRegions3.Dispose();
                HOperatorSet.Connection(ho_RegionIntersection1, out ho_ConnectedRegions3);

                //=====判斷上下左右區域=====*
                switch (hv_Index.I)
                {
                    //-----上、下邊檢測-----*
                    case 0:
                    case 2:
                        //-----左至右排序-----*
                        ho_SortedRegions2.Dispose();
                        HOperatorSet.SortRegion(ho_ConnectedRegions3, out ho_SortedRegions2, "character",
                            "true", "row");

                        //-----移除頭尾兩個Region-----*
                        hv_Number.Dispose();
                        HOperatorSet.CountObj(ho_SortedRegions2, out hv_Number);
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_ObjectsReduced.Dispose();
                            HOperatorSet.RemoveObj(ho_SortedRegions2, out ho_ObjectsReduced, (new HTuple(1)).TupleConcat(
                                hv_Number));
                        }

                        //-----分出上、下區域的斷線區域-----*
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_RegionUnion7.Dispose();
                            SearchMissingLine(ho_ObjectsReduced, out ho_RegionUnion7, hv_GridWidthPixel * 1000,
                                hv_GridHeightPixel);
                        }

                        break;

                    //-----左、右邊檢測-----*
                    case 1:
                    case 3:
                        //-----找出左、右區域的斷線區域；不看X軸線段漏線，因此將高度設大-----*
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_RegionUnion7.Dispose();
                            SearchMissingLine(ho_ConnectedRegions3, out ho_RegionUnion7, hv_GridWidthPixel,
                                hv_GridHeightPixel * 1000);
                        }

                        break;

                }

                //-----合併四邊斷線區域-----*
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.Union2(ho_OuterMissingLineRegions, ho_RegionUnion7, out ExpTmpOutVar_0
                        );
                    ho_OuterMissingLineRegions.Dispose();
                    ho_OuterMissingLineRegions = ExpTmpOutVar_0;
                }

            }

            //-----以矩形標記斷線區域-----*
            ho_RegionUnion2.Dispose();
            HOperatorSet.Union2(ho_OuterMissingLineRegions, ho_InnerMissingLineRegions, out ho_RegionUnion2
                );
            ho_MissingLineRegion.Dispose();
            HOperatorSet.Connection(ho_RegionUnion2, out ho_MissingLineRegion);
            ho_RegionBorder1.Dispose();
            MarkDefectArea(ho_MissingLineRegion, out ho_RegionBorder1, 5, 30);

            //-----計算斷線距離-----*
            hv_FinalResult.Dispose();
            CalculateMissingLineDistance(ho_ImageReduced, ho_MissingLineRegion, ho_RegionBorder,
                ho_RegionDynThresh, hv_ZoomFactor, hv_GridWidthPixel, hv_GridHeightPixel,
                out hv_FinalResult);

            ho_Region.Dispose();
            ho_RegionTrans.Dispose();
            ho_RegionBorder.Dispose();
            ho_ImageReduced.Dispose();
            ho_RegionTrans2.Dispose();
            ho_RegionErosion2.Dispose();
            ho_ImageReduced1.Dispose();
            ho_ImageMean.Dispose();
            ho_RegionDynThresh1.Dispose();
            ho_ImageReduced2.Dispose();
            ho_RegionDynThresh.Dispose();
            ho_RegionDilation.Dispose();
            ho_RegionUnion.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_SelectedRegions.Dispose();
            ho_RegionFillUp.Dispose();
            ho_RegionDifference.Dispose();
            ho_AllRegions.Dispose();
            ho_RegionTrans4.Dispose();
            ho_RegionErosion1.Dispose();
            ho_RegionBorder3.Dispose();
            ho_RegionUnion5.Dispose();
            ho_ConnectedRegions6.Dispose();
            ho_InnerRegions.Dispose();
            ho_InnerMissingLineRegions.Dispose();
            ho_OuterMissingLineRegions.Dispose();
            ho_AllRegionsUnion.Dispose();
            ho_InnerRegionsUnion.Dispose();
            ho_OuterRegions.Dispose();
            ho_OuterRegionLines.Dispose();
            ho_RegionLineSelected.Dispose();
            ho_RegionUnion6.Dispose();
            ho_ConnectedRegions2.Dispose();
            ho_RegionOut.Dispose();
            ho_RegionIntersection1.Dispose();
            ho_ConnectedRegions3.Dispose();
            ho_SortedRegions2.Dispose();
            ho_ObjectsReduced.Dispose();
            ho_RegionUnion7.Dispose();
            ho_RegionUnion2.Dispose();
            ho_MissingLineRegion.Dispose();

            hv_Width.Dispose();
            hv_Height.Dispose();
            hv_Min1.Dispose();
            hv_Max1.Dispose();
            hv_Range2.Dispose();
            hv_Energy1.Dispose();
            hv_Correlation1.Dispose();
            hv_Homogeneity1.Dispose();
            hv_Contrast1.Dispose();
            hv_Area6.Dispose();
            hv_Row6.Dispose();
            hv_Column6.Dispose();
            hv_Area2.Dispose();
            hv_Row2.Dispose();
            hv_Column2.Dispose();
            hv_Index.Dispose();
            hv_Number.Dispose();

        }

        /// <summary>
        /// 針對0805的Line Scan影像找出斷線區域及長度
        /// </summary>
        /// <param name="ho_Image00011"></param>
        /// <param name="ho_RegionBorder1"></param>
        /// <param name="hv_IgnorePixelSize"></param>
        /// <param name="hv_GridWidthPixel"></param>
        /// <param name="hv_GridHeightPixel"></param>
        /// <param name="hv_ZoomFactor"></param>
        /// <param name="hv_FinalResult"></param>
        private void DetectMissingLine_0805(HObject ho_Image00011, out HObject ho_RegionBorder1,
      HTuple hv_GridWidthPixel, HTuple hv_GridHeightPixel, HTuple hv_ZoomFactor, HTuple hv_Factor,
      out HTuple hv_FinalResult)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_Region, ho_RegionTrans, ho_RegionBorder;
            HObject ho_ImageReduced, ho_RegionTrans2, ho_RegionErosion2;
            HObject ho_ImageReduced1, ho_ImageMean, ho_RegionDynThresh1;
            HObject ho_ImageReduced2, ho_RegionDynThresh, ho_RegionDilation;
            HObject ho_RegionUnion, ho_ConnectedRegions, ho_SelectedRegions;
            HObject ho_RegionFillUp, ho_RegionDifference, ho_AllRegions;
            HObject ho_RegionTrans4, ho_RegionErosion1, ho_RegionBorder3;
            HObject ho_RegionUnion5, ho_ConnectedRegions6, ho_InnerRegions;
            HObject ho_InnerMissingLineRegions, ho_OuterMissingLineRegions;
            HObject ho_AllRegionsUnion, ho_InnerRegionsUnion, ho_OuterRegions;
            HObject ho_OuterRegionLines, ho_RegionLineSelected = null;
            HObject ho_RegionUnion6 = null, ho_ConnectedRegions2 = null;
            HObject ho_RegionOut = null, ho_RegionIntersection1 = null;
            HObject ho_ConnectedRegions3 = null, ho_SortedRegions2 = null;
            HObject ho_ObjectsReduced = null, ho_RegionUnion7 = null, ho_RegionUnion2;
            HObject ho_MissingLineRegion;

            // Local control variables 

            HTuple hv_Width = new HTuple(), hv_Height = new HTuple();
            HTuple hv_Min1 = new HTuple(), hv_Max1 = new HTuple();
            HTuple hv_Range2 = new HTuple(), hv_Energy1 = new HTuple();
            HTuple hv_Correlation1 = new HTuple(), hv_Homogeneity1 = new HTuple();
            HTuple hv_Contrast1 = new HTuple(), hv_Area6 = new HTuple();
            HTuple hv_Row6 = new HTuple(), hv_Column6 = new HTuple();
            HTuple hv_Area2 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Column2 = new HTuple(), hv_Index = new HTuple();
            HTuple hv_Number = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_RegionBorder1);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans2);
            HOperatorSet.GenEmptyObj(out ho_RegionErosion2);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced1);
            HOperatorSet.GenEmptyObj(out ho_ImageMean);
            HOperatorSet.GenEmptyObj(out ho_RegionDynThresh1);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced2);
            HOperatorSet.GenEmptyObj(out ho_RegionDynThresh);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference);
            HOperatorSet.GenEmptyObj(out ho_AllRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans4);
            HOperatorSet.GenEmptyObj(out ho_RegionErosion1);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder3);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion5);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions6);
            HOperatorSet.GenEmptyObj(out ho_InnerRegions);
            HOperatorSet.GenEmptyObj(out ho_InnerMissingLineRegions);
            HOperatorSet.GenEmptyObj(out ho_OuterMissingLineRegions);
            HOperatorSet.GenEmptyObj(out ho_AllRegionsUnion);
            HOperatorSet.GenEmptyObj(out ho_InnerRegionsUnion);
            HOperatorSet.GenEmptyObj(out ho_OuterRegions);
            HOperatorSet.GenEmptyObj(out ho_OuterRegionLines);
            HOperatorSet.GenEmptyObj(out ho_RegionLineSelected);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion6);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions2);
            HOperatorSet.GenEmptyObj(out ho_RegionOut);
            HOperatorSet.GenEmptyObj(out ho_RegionIntersection1);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions3);
            HOperatorSet.GenEmptyObj(out ho_SortedRegions2);
            HOperatorSet.GenEmptyObj(out ho_ObjectsReduced);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion7);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion2);
            HOperatorSet.GenEmptyObj(out ho_MissingLineRegion);
            hv_FinalResult = new HTuple();
            //=====找出待測物範圍=====*
            ho_Region.Dispose(); ho_RegionTrans.Dispose(); ho_RegionBorder.Dispose();
            FindFrontRegion(ho_Image00011, out ho_Region, out ho_RegionTrans, out ho_RegionBorder
                );
            ho_ImageReduced.Dispose();
            HOperatorSet.ReduceDomain(ho_Image00011, ho_RegionTrans, out ho_ImageReduced);

            //-----找出板子灰度區間-----*
            ho_RegionTrans2.Dispose();
            HOperatorSet.ShapeTrans(ho_Region, out ho_RegionTrans2, "rectangle1");
            hv_Width.Dispose();
            HOperatorSet.RegionFeatures(ho_RegionTrans2, "width", out hv_Width);
            hv_Height.Dispose();
            HOperatorSet.RegionFeatures(ho_RegionTrans2, "height", out hv_Height);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionErosion2.Dispose();
                HOperatorSet.ErosionRectangle1(ho_RegionTrans2, out ho_RegionErosion2, hv_Width / 15,
                    hv_Height / 15);
            }
            ho_ImageReduced1.Dispose();
            HOperatorSet.ReduceDomain(ho_Image00011, ho_RegionErosion2, out ho_ImageReduced1
                );
            //-----決定Local threshold的Range-----*
            hv_Min1.Dispose(); hv_Max1.Dispose(); hv_Range2.Dispose();
            HOperatorSet.MinMaxGray(ho_ImageReduced1, ho_ImageReduced1, 0, out hv_Min1, out hv_Max1,
                out hv_Range2);

            //-----取得影像對比度-----*
            hv_Energy1.Dispose(); hv_Correlation1.Dispose(); hv_Homogeneity1.Dispose(); hv_Contrast1.Dispose();
            HOperatorSet.CoocFeatureImage(ho_RegionTrans, ho_Image00011, 6, 0, out hv_Energy1,
                out hv_Correlation1, out hv_Homogeneity1, out hv_Contrast1);

            //-----先大略篩出線段區域，以加快local_threshold的速度-----*
            ho_ImageMean.Dispose();
            HOperatorSet.MeanImage(ho_ImageReduced, out ho_ImageMean, 31, 31);
            ho_RegionDynThresh1.Dispose();
            HOperatorSet.DynThreshold(ho_ImageReduced, ho_ImageMean, out ho_RegionDynThresh1,
                5, "dark");
            ho_ImageReduced2.Dispose();
            HOperatorSet.ReduceDomain(ho_ImageReduced, ho_RegionDynThresh1, out ho_ImageReduced2
                );

            //-----0.1~2 (鬆~嚴)-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionDynThresh.Dispose();
                HOperatorSet.LocalThreshold(ho_ImageReduced2, out ho_RegionDynThresh, "adapted_std_deviation",
                    "dark", ((new HTuple("mask_size")).TupleConcat("scale")).TupleConcat("range"),
                    ((((20 * hv_ZoomFactor)).TupleConcat((hv_Contrast1 / 10) * hv_Factor))).TupleConcat(
                    hv_Range2 / 2));
            }
            //scale: 調整線段與底色對比，對比高數值越高，反之則反

            //-----4邊往內縮一段不做判斷的區域-----*
            ho_RegionDilation.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionBorder, out ho_RegionDilation, 25, 25);

            //-----合併邊界與二值化結果-----*
            ho_RegionUnion.Dispose();
            HOperatorSet.Union2(ho_RegionDynThresh, ho_RegionDilation, out ho_RegionUnion
                );

            //-----去除線段以外的雜點-----*
            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_RegionUnion, out ho_ConnectedRegions);
            hv_Area6.Dispose(); hv_Row6.Dispose(); hv_Column6.Dispose();
            HOperatorSet.AreaCenter(ho_ConnectedRegions, out hv_Area6, out hv_Row6, out hv_Column6);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_SelectedRegions, "area",
                    "and", 150, (hv_Area6.TupleMax()) + 1);
            }

            //-----將線段填滿-----*
            ho_RegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_SelectedRegions, out ho_RegionFillUp);

            //-----找出填滿區域與二值化區域不同處，即可找出所有方塊區域-----*
            ho_RegionDifference.Dispose();
            HOperatorSet.Difference(ho_RegionFillUp, ho_ConnectedRegions, out ho_RegionDifference
                );

            //-----分離所有方塊區域-----*
            ho_AllRegions.Dispose();
            HOperatorSet.Connection(ho_RegionDifference, out ho_AllRegions);

            //=====四周往內縮一格=====*
            //-----根據基板形狀建立一個矩形-----*
            ho_RegionTrans4.Dispose();
            HOperatorSet.ShapeTrans(ho_Region, out ho_RegionTrans4, "rectangle2");

            //-----將矩形往內縮小-----*
            ho_RegionErosion1.Dispose();
            HOperatorSet.ErosionRectangle1(ho_RegionTrans4, out ho_RegionErosion1, 120, 120);

            //-----找出矩陣縮小後的邊框-----*
            ho_RegionBorder3.Dispose();
            HOperatorSet.Boundary(ho_RegionErosion1, out ho_RegionBorder3, "inner");

            //-----與所有方塊區域合併-----*
            ho_RegionUnion5.Dispose();
            HOperatorSet.Union2(ho_RegionBorder3, ho_RegionDifference, out ho_RegionUnion5
                );

            //-----分離方塊後，將最大面積移除，即可得到內圈方塊-----*
            ho_ConnectedRegions6.Dispose();
            HOperatorSet.Connection(ho_RegionUnion5, out ho_ConnectedRegions6);
            hv_Area2.Dispose(); hv_Row2.Dispose(); hv_Column2.Dispose();
            HOperatorSet.AreaCenter(ho_ConnectedRegions6, out hv_Area2, out hv_Row2, out hv_Column2);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_InnerRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions6, out ho_InnerRegions, "area", "and",
                    1, (hv_Area2.TupleMax()) - 1);
            }

            //=====內圈方塊處理=====*
            //-----找出內圈斷線區域-----*
            ho_InnerMissingLineRegions.Dispose();
            SearchMissingLine(ho_InnerRegions, out ho_InnerMissingLineRegions, hv_GridWidthPixel,
                hv_GridHeightPixel);

            //=====處理外圈方塊區域=====*
            ho_OuterMissingLineRegions.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_OuterMissingLineRegions);

            //-----篩出外圈方塊區域-----*
            ho_AllRegionsUnion.Dispose();
            HOperatorSet.Union1(ho_AllRegions, out ho_AllRegionsUnion);
            ho_InnerRegionsUnion.Dispose();
            HOperatorSet.Union1(ho_InnerRegions, out ho_InnerRegionsUnion);
            ho_OuterRegions.Dispose();
            HOperatorSet.Difference(ho_AllRegionsUnion, ho_InnerRegionsUnion, out ho_OuterRegions
                );

            //-----產生邊界內縮線段-----*
            ho_OuterRegionLines.Dispose();
            FindOuterLines(ho_RegionBorder3, out ho_OuterRegionLines);

            for (hv_Index = 0; (int)hv_Index <= 3; hv_Index = (int)hv_Index + 1)
            {
                //-----對應方向的線段-----*
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_RegionLineSelected.Dispose();
                    HOperatorSet.SelectObj(ho_OuterRegionLines, out ho_RegionLineSelected, hv_Index + 1);
                }

                //-----將四周方格區域與對應的邊合併-----*
                ho_RegionUnion6.Dispose();
                HOperatorSet.Union2(ho_OuterRegions, ho_RegionLineSelected, out ho_RegionUnion6
                    );
                //-----將合併後的區域分開，找出最大面積就是對應邊的方塊區域+線段-----*
                ho_ConnectedRegions2.Dispose();
                HOperatorSet.Connection(ho_RegionUnion6, out ho_ConnectedRegions2);
                ho_RegionOut.Dispose();
                RemoveRegionByArea(ho_ConnectedRegions2, out ho_RegionOut, 15);
                //-----因為方塊區域有合併線段，所以找出與方塊重疊區域，即為對應方塊區域-----*
                ho_RegionIntersection1.Dispose();
                HOperatorSet.Intersection(ho_OuterRegions, ho_RegionOut, out ho_RegionIntersection1
                    );
                //-----將方塊區域連通-----*
                ho_ConnectedRegions3.Dispose();
                HOperatorSet.Connection(ho_RegionIntersection1, out ho_ConnectedRegions3);

                //=====判斷上下左右區域=====*
                switch (hv_Index.I)
                {
                    //-----上、下邊檢測-----*
                    case 0:
                    case 2:
                        //-----左至右排序-----*
                        ho_SortedRegions2.Dispose();
                        HOperatorSet.SortRegion(ho_ConnectedRegions3, out ho_SortedRegions2, "character",
                            "true", "row");

                        //-----移除頭尾兩個Region-----*
                        hv_Number.Dispose();
                        HOperatorSet.CountObj(ho_SortedRegions2, out hv_Number);
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_ObjectsReduced.Dispose();
                            HOperatorSet.RemoveObj(ho_SortedRegions2, out ho_ObjectsReduced, (new HTuple(1)).TupleConcat(
                                hv_Number));
                        }

                        //-----分出上、下區域的斷線區域-----*
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_RegionUnion7.Dispose();
                            SearchMissingLine(ho_ObjectsReduced, out ho_RegionUnion7, hv_GridWidthPixel * 1000,
                                hv_GridHeightPixel);
                        }

                        break;

                    //-----左、右邊檢測-----*
                    case 1:
                    case 3:
                        //-----分出上、下區域的斷線區域-----*
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_RegionUnion7.Dispose();
                            SearchMissingLine(ho_ConnectedRegions3, out ho_RegionUnion7, hv_GridWidthPixel,
                                hv_GridHeightPixel * 1000);
                        }

                        break;

                }

                //-----合併四邊斷線區域-----*
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.Union2(ho_OuterMissingLineRegions, ho_RegionUnion7, out ExpTmpOutVar_0
                        );
                    ho_OuterMissingLineRegions.Dispose();
                    ho_OuterMissingLineRegions = ExpTmpOutVar_0;
                }

            }

            //-----以矩形標記斷線區域-----*
            ho_RegionUnion2.Dispose();
            HOperatorSet.Union2(ho_OuterMissingLineRegions, ho_InnerMissingLineRegions, out ho_RegionUnion2
                );
            ho_MissingLineRegion.Dispose();
            HOperatorSet.Connection(ho_RegionUnion2, out ho_MissingLineRegion);
            ho_RegionBorder1.Dispose();
            MarkDefectArea(ho_MissingLineRegion, out ho_RegionBorder1, 5, 30);

            //-----計算斷線距離-----*
            hv_FinalResult.Dispose();
            CalculateMissingLineDistance(ho_ImageReduced, ho_MissingLineRegion, ho_RegionBorder,
                ho_RegionDynThresh, hv_ZoomFactor, hv_GridWidthPixel, hv_GridHeightPixel,
                out hv_FinalResult);

            ho_Region.Dispose();
            ho_RegionTrans.Dispose();
            ho_RegionBorder.Dispose();
            ho_ImageReduced.Dispose();
            ho_RegionTrans2.Dispose();
            ho_RegionErosion2.Dispose();
            ho_ImageReduced1.Dispose();
            ho_ImageMean.Dispose();
            ho_RegionDynThresh1.Dispose();
            ho_ImageReduced2.Dispose();
            ho_RegionDynThresh.Dispose();
            ho_RegionDilation.Dispose();
            ho_RegionUnion.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_SelectedRegions.Dispose();
            ho_RegionFillUp.Dispose();
            ho_RegionDifference.Dispose();
            ho_AllRegions.Dispose();
            ho_RegionTrans4.Dispose();
            ho_RegionErosion1.Dispose();
            ho_RegionBorder3.Dispose();
            ho_RegionUnion5.Dispose();
            ho_ConnectedRegions6.Dispose();
            ho_InnerRegions.Dispose();
            ho_InnerMissingLineRegions.Dispose();
            ho_OuterMissingLineRegions.Dispose();
            ho_AllRegionsUnion.Dispose();
            ho_InnerRegionsUnion.Dispose();
            ho_OuterRegions.Dispose();
            ho_OuterRegionLines.Dispose();
            ho_RegionLineSelected.Dispose();
            ho_RegionUnion6.Dispose();
            ho_ConnectedRegions2.Dispose();
            ho_RegionOut.Dispose();
            ho_RegionIntersection1.Dispose();
            ho_ConnectedRegions3.Dispose();
            ho_SortedRegions2.Dispose();
            ho_ObjectsReduced.Dispose();
            ho_RegionUnion7.Dispose();
            ho_RegionUnion2.Dispose();
            ho_MissingLineRegion.Dispose();

            hv_Width.Dispose();
            hv_Height.Dispose();
            hv_Min1.Dispose();
            hv_Max1.Dispose();
            hv_Range2.Dispose();
            hv_Energy1.Dispose();
            hv_Correlation1.Dispose();
            hv_Homogeneity1.Dispose();
            hv_Contrast1.Dispose();
            hv_Area6.Dispose();
            hv_Row6.Dispose();
            hv_Column6.Dispose();
            hv_Area2.Dispose();
            hv_Row2.Dispose();
            hv_Column2.Dispose();
            hv_Index.Dispose();
            hv_Number.Dispose();

        }

        /// <summary>
        /// 針對0603的Line Scan影像找出斷線區域及長度
        /// </summary>
        /// <param name="ho_Image00011"></param>
        /// <param name="ho_RegionBorder1"></param>
        /// <param name="hv_IgnorePixelSize"></param>
        /// <param name="hv_GridWidthPixel"></param>
        /// <param name="hv_GridHeightPixel"></param>
        /// <param name="hv_ZoomFactor"></param>
        /// <param name="hv_FinalResult"></param>
        private void DetectMissingLine_0603(HObject ho_Image00011, out HObject ho_RegionBorder1,
      HTuple hv_GridWidthPixel, HTuple hv_GridHeightPixel, HTuple hv_ZoomFactor, HTuple hv_Factor,
      out HTuple hv_FinalResult)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_Region, ho_RegionTrans, ho_RegionBorder;
            HObject ho_ImageReduced, ho_RegionTrans2, ho_RegionErosion2;
            HObject ho_ImageReduced1, ho_ImageMean, ho_RegionDynThresh;
            HObject ho_ImageReduced2, ho_RegionUnionX, ho_RegionUnionY;
            HObject ho_RegionDilationX, ho_BorderRegionDilation, ho_RegionDifferenceX;
            HObject ho_RegionIntersectionX, ho_ImageReducedX, ho_ImageMeanX;
            HObject ho_RegionDynThreshX, ho_RegionDilationY, ho_RegionDifferenceY;
            HObject ho_RegionIntersectionY, ho_ImageReducedY, ho_ImageMeanY;
            HObject ho_RegionDynThreshY, ho_RegionDilation, ho_RegionUnion;
            HObject ho_ConnectedRegions, ho_SelectedRegions, ho_RegionFillUp;
            HObject ho_RegionDifference, ho_AllRegions, ho_RegionTrans4;
            HObject ho_RegionErosion1, ho_RegionBorder3, ho_RegionUnion5;
            HObject ho_ConnectedRegions6, ho_InnerRegions, ho_InnerMissingLineRegions;
            HObject ho_OuterMissingLineRegions, ho_AllRegionsUnion;
            HObject ho_InnerRegionsUnion, ho_OuterRegions, ho_OuterRegionLines;
            HObject ho_RegionLineSelected = null, ho_RegionUnion6 = null;
            HObject ho_ConnectedRegions2 = null, ho_RegionOut = null, ho_RegionIntersection1 = null;
            HObject ho_ConnectedRegions3 = null, ho_SortedRegions2 = null;
            HObject ho_ObjectsReduced = null, ho_RegionUnion7 = null, ho_RegionUnion2;
            HObject ho_MissingLineRegion;

            // Local control variables 

            HTuple hv_Width = new HTuple(), hv_Height = new HTuple();
            HTuple hv_Min1 = new HTuple(), hv_Max1 = new HTuple();
            HTuple hv_Range2 = new HTuple(), hv_Energy1 = new HTuple();
            HTuple hv_Correlation1 = new HTuple(), hv_Homogeneity1 = new HTuple();
            HTuple hv_Contrast1 = new HTuple(), hv_Area6 = new HTuple();
            HTuple hv_Row6 = new HTuple(), hv_Column6 = new HTuple();
            HTuple hv_Area2 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Column2 = new HTuple(), hv_Index = new HTuple();
            HTuple hv_Number = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_RegionBorder1);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans2);
            HOperatorSet.GenEmptyObj(out ho_RegionErosion2);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced1);
            HOperatorSet.GenEmptyObj(out ho_ImageMean);
            HOperatorSet.GenEmptyObj(out ho_RegionDynThresh);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced2);
            HOperatorSet.GenEmptyObj(out ho_RegionUnionX);
            HOperatorSet.GenEmptyObj(out ho_RegionUnionY);
            HOperatorSet.GenEmptyObj(out ho_RegionDilationX);
            HOperatorSet.GenEmptyObj(out ho_BorderRegionDilation);
            HOperatorSet.GenEmptyObj(out ho_RegionDifferenceX);
            HOperatorSet.GenEmptyObj(out ho_RegionIntersectionX);
            HOperatorSet.GenEmptyObj(out ho_ImageReducedX);
            HOperatorSet.GenEmptyObj(out ho_ImageMeanX);
            HOperatorSet.GenEmptyObj(out ho_RegionDynThreshX);
            HOperatorSet.GenEmptyObj(out ho_RegionDilationY);
            HOperatorSet.GenEmptyObj(out ho_RegionDifferenceY);
            HOperatorSet.GenEmptyObj(out ho_RegionIntersectionY);
            HOperatorSet.GenEmptyObj(out ho_ImageReducedY);
            HOperatorSet.GenEmptyObj(out ho_ImageMeanY);
            HOperatorSet.GenEmptyObj(out ho_RegionDynThreshY);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference);
            HOperatorSet.GenEmptyObj(out ho_AllRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans4);
            HOperatorSet.GenEmptyObj(out ho_RegionErosion1);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder3);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion5);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions6);
            HOperatorSet.GenEmptyObj(out ho_InnerRegions);
            HOperatorSet.GenEmptyObj(out ho_InnerMissingLineRegions);
            HOperatorSet.GenEmptyObj(out ho_OuterMissingLineRegions);
            HOperatorSet.GenEmptyObj(out ho_AllRegionsUnion);
            HOperatorSet.GenEmptyObj(out ho_InnerRegionsUnion);
            HOperatorSet.GenEmptyObj(out ho_OuterRegions);
            HOperatorSet.GenEmptyObj(out ho_OuterRegionLines);
            HOperatorSet.GenEmptyObj(out ho_RegionLineSelected);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion6);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions2);
            HOperatorSet.GenEmptyObj(out ho_RegionOut);
            HOperatorSet.GenEmptyObj(out ho_RegionIntersection1);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions3);
            HOperatorSet.GenEmptyObj(out ho_SortedRegions2);
            HOperatorSet.GenEmptyObj(out ho_ObjectsReduced);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion7);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion2);
            HOperatorSet.GenEmptyObj(out ho_MissingLineRegion);
            hv_FinalResult = new HTuple();
            //=====找出待測物範圍=====*
            ho_Region.Dispose(); ho_RegionTrans.Dispose(); ho_RegionBorder.Dispose();
            FindFrontRegion(ho_Image00011, out ho_Region, out ho_RegionTrans, out ho_RegionBorder
                );
            ho_ImageReduced.Dispose();
            HOperatorSet.ReduceDomain(ho_Image00011, ho_RegionTrans, out ho_ImageReduced);

            //-----找出板子灰度區間-----*
            ho_RegionTrans2.Dispose();
            HOperatorSet.ShapeTrans(ho_Region, out ho_RegionTrans2, "rectangle1");
            hv_Width.Dispose();
            HOperatorSet.RegionFeatures(ho_RegionTrans2, "width", out hv_Width);
            hv_Height.Dispose();
            HOperatorSet.RegionFeatures(ho_RegionTrans2, "height", out hv_Height);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionErosion2.Dispose();
                HOperatorSet.ErosionRectangle1(ho_RegionTrans2, out ho_RegionErosion2, hv_Width / 15,
                    hv_Height / 15);
            }
            ho_ImageReduced1.Dispose();
            HOperatorSet.ReduceDomain(ho_Image00011, ho_RegionErosion2, out ho_ImageReduced1
                );
            //-----決定Local threshold的Range-----*
            hv_Min1.Dispose(); hv_Max1.Dispose(); hv_Range2.Dispose();
            HOperatorSet.MinMaxGray(ho_ImageReduced1, ho_ImageReduced1, 0, out hv_Min1, out hv_Max1,
                out hv_Range2);

            //-----取得影像對比度-----*
            hv_Energy1.Dispose(); hv_Correlation1.Dispose(); hv_Homogeneity1.Dispose(); hv_Contrast1.Dispose();
            HOperatorSet.CoocFeatureImage(ho_RegionTrans, ho_Image00011, 6, 0, out hv_Energy1,
                out hv_Correlation1, out hv_Homogeneity1, out hv_Contrast1);

            //-----先大略篩出線段區域，以加快local_threshold的速度-----*
            //mean_image (ImageReduced, ImageMean, 11, 11)
            //dyn_threshold (ImageReduced, ImageMean, RegionDynThresh, 5, 'dark')
            //reduce_domain (ImageReduced, RegionDynThresh, ImageReduced2)
            //-----0.1~2 (鬆~嚴)-----*
            //local_threshold (ImageReduced2, RegionDynThresh, 'adapted_std_deviation', 'dark', ['mask_size','scale','range'], [25*ZoomFactor,Contrast1/10*Factor,Range2/2])
            //*直的橫的分開用mean+dyn,參數用兩組
            //x
            ho_ImageMean.Dispose();
            HOperatorSet.MeanImage(ho_ImageReduced, out ho_ImageMean, 31, 31);

            ho_RegionDynThresh.Dispose();
            HOperatorSet.DynThreshold(ho_ImageReduced, ho_ImageMean, out ho_RegionDynThresh,
                5, "dark");
            ho_ImageReduced2.Dispose();
            HOperatorSet.ReduceDomain(ho_ImageReduced, ho_RegionDynThresh, out ho_ImageReduced2
                );
            //-----0.1~2 (鬆~嚴)-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionDynThresh.Dispose();
                HOperatorSet.LocalThreshold(ho_ImageReduced2, out ho_RegionDynThresh, "adapted_std_deviation",
                    "dark", ((new HTuple("mask_size")).TupleConcat("scale")).TupleConcat("range"),
                    ((((25 * hv_ZoomFactor)).TupleConcat((hv_Contrast1 / 10) * hv_Factor))).TupleConcat(
                    hv_Range2 / 2));
            }
            ho_RegionUnionX.Dispose(); ho_RegionUnionY.Dispose();
            ExtractRowAndColLines(ho_RegionDynThresh, out ho_RegionUnionX, out ho_RegionUnionY,
                hv_GridHeightPixel, hv_GridWidthPixel, hv_ZoomFactor);
            //*加寬x線段,擷取加寬厚的區域,再作二值化
            ho_RegionDilationX.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionUnionX, out ho_RegionDilationX, 2, 21);
            //--去掉抓到邊框部分*
            ho_BorderRegionDilation.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionBorder, out ho_BorderRegionDilation,
                30, 30);
            ho_RegionDifferenceX.Dispose();
            HOperatorSet.Difference(ho_RegionDilationX, ho_BorderRegionDilation, out ho_RegionDifferenceX
                );
            ho_RegionIntersectionX.Dispose();
            HOperatorSet.Intersection(ho_RegionDifferenceX, ho_Region, out ho_RegionIntersectionX
                );
            //*先抓 x
            ho_ImageReducedX.Dispose();
            HOperatorSet.ReduceDomain(ho_ImageReduced, ho_RegionIntersectionX, out ho_ImageReducedX
                );
            //--再作二值化--*
            ho_ImageMeanX.Dispose();
            HOperatorSet.MeanImage(ho_ImageReduced, out ho_ImageMeanX, 9, 9);
            ho_RegionDynThreshX.Dispose();
            HOperatorSet.DynThreshold(ho_ImageReducedX, ho_ImageMeanX, out ho_RegionDynThreshX,
                5, "dark");
            //reduce_domain (ImageReduced, RegionIntersectionX, ImageReducedX)
            //-----0.1~2 (鬆~嚴)-----*
            //local_threshold (ImageReducedX, RegionDynThreshX, 'adapted_std_deviation', 'dark', ['mask_size','scale','range'], [25*ZoomFactor,Contrast1/10*Factor,Range2/2])
            //y
            //*加寬y線段,擷取加寬厚的區域,再作二值化
            ho_RegionDilationY.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionUnionY, out ho_RegionDilationY, 21,
                2);
            //--去掉抓到邊框部分*
            ho_BorderRegionDilation.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionBorder, out ho_BorderRegionDilation,
                30, 30);
            ho_RegionDifferenceY.Dispose();
            HOperatorSet.Difference(ho_RegionDilationY, ho_BorderRegionDilation, out ho_RegionDifferenceY
                );
            ho_RegionIntersectionY.Dispose();
            HOperatorSet.Intersection(ho_RegionDifferenceY, ho_Region, out ho_RegionIntersectionY
                );
            //*抓 Y
            ho_ImageReducedY.Dispose();
            HOperatorSet.ReduceDomain(ho_ImageReduced, ho_RegionIntersectionY, out ho_ImageReducedY
                );
            //--再作二值化--*
            //--均值化處理的Mask方塊越大,忽略更多雜點(塞孔)--*
            ho_ImageMeanY.Dispose();
            HOperatorSet.MeanImage(ho_ImageReduced, out ho_ImageMeanY, 15, 15);
            //--動態閥值的offset越大,只有亮度明顯不同的區域才會被分割出來
            ho_RegionDynThreshY.Dispose();
            HOperatorSet.DynThreshold(ho_ImageReducedY, ho_ImageMeanY, out ho_RegionDynThreshY,
                8, "dark");
            //--合併XY--*
            ho_RegionDynThresh.Dispose();
            HOperatorSet.Union2(ho_RegionDynThreshX, ho_RegionDynThreshY, out ho_RegionDynThresh
                );
            //scale: 調整線內縮段與底色對比，對比高數值越高，反之則反
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.Union2(ho_BorderRegionDilation, ho_RegionDynThresh, out ExpTmpOutVar_0
                    );
                ho_RegionDynThresh.Dispose();
                ho_RegionDynThresh = ExpTmpOutVar_0;
            }
            //-----4邊往一段不做判斷的區域-----*
            ho_RegionDilation.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionBorder, out ho_RegionDilation, 25, 25);
            //-----合併邊界與二值化結果-----*

            ho_RegionUnion.Dispose();
            HOperatorSet.Union2(ho_RegionDynThresh, ho_RegionDilation, out ho_RegionUnion
                );
            //-----去除線段以外的雜點-----*
            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_RegionUnion, out ho_ConnectedRegions);
            hv_Area6.Dispose(); hv_Row6.Dispose(); hv_Column6.Dispose();
            HOperatorSet.AreaCenter(ho_ConnectedRegions, out hv_Area6, out hv_Row6, out hv_Column6);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_SelectedRegions, "area",
                    "and", 150, (hv_Area6.TupleMax()) + 1);
            }

            //-----將線段填滿-----
            ho_RegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_SelectedRegions, out ho_RegionFillUp);

            //-----找出填滿區域與二值化區域不同處，即可找出所有方塊區域-----*
            ho_RegionDifference.Dispose();
            HOperatorSet.Difference(ho_RegionFillUp, ho_ConnectedRegions, out ho_RegionDifference
                );

            //-----分離所有方塊區域-----*
            ho_AllRegions.Dispose();
            HOperatorSet.Connection(ho_RegionDifference, out ho_AllRegions);




            //=====四周往內縮一格=====*
            //-----根據基板形狀建立一個矩形-----*
            ho_RegionTrans4.Dispose();
            HOperatorSet.ShapeTrans(ho_Region, out ho_RegionTrans4, "rectangle2");

            //-----將矩形往內縮小-----*
            ho_RegionErosion1.Dispose();
            HOperatorSet.ErosionRectangle1(ho_RegionTrans4, out ho_RegionErosion1, 120, 120);

            //-----找出矩陣縮小後的邊框-----*
            ho_RegionBorder3.Dispose();
            HOperatorSet.Boundary(ho_RegionErosion1, out ho_RegionBorder3, "inner");

            //-----與所有方塊區域合併-----*
            ho_RegionUnion5.Dispose();
            HOperatorSet.Union2(ho_RegionBorder3, ho_RegionDifference, out ho_RegionUnion5
                );

            //-----分離方塊後，將最大面積移除，即可得到內圈方塊-----*
            ho_ConnectedRegions6.Dispose();
            HOperatorSet.Connection(ho_RegionUnion5, out ho_ConnectedRegions6);
            hv_Area2.Dispose(); hv_Row2.Dispose(); hv_Column2.Dispose();
            HOperatorSet.AreaCenter(ho_ConnectedRegions6, out hv_Area2, out hv_Row2, out hv_Column2);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_InnerRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions6, out ho_InnerRegions, "area", "and",
                    1, (hv_Area2.TupleMax()) - 1);
            }

            //=====內圈方塊處理=====*
            //-----找出內圈斷線區域-----*
            ho_InnerMissingLineRegions.Dispose();
            SearchMissingLine(ho_InnerRegions, out ho_InnerMissingLineRegions, hv_GridWidthPixel,
                hv_GridHeightPixel);

            //=====處理外圈方塊區域=====*
            ho_OuterMissingLineRegions.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_OuterMissingLineRegions);

            //-----篩出外圈方塊區域-----*
            ho_AllRegionsUnion.Dispose();
            HOperatorSet.Union1(ho_AllRegions, out ho_AllRegionsUnion);
            ho_InnerRegionsUnion.Dispose();
            HOperatorSet.Union1(ho_InnerRegions, out ho_InnerRegionsUnion);
            ho_OuterRegions.Dispose();
            HOperatorSet.Difference(ho_AllRegionsUnion, ho_InnerRegionsUnion, out ho_OuterRegions
                );

            //-----產生邊界內縮線段-----*
            ho_OuterRegionLines.Dispose();
            FindOuterLines(ho_RegionBorder3, out ho_OuterRegionLines);

            for (hv_Index = 0; (int)hv_Index <= 3; hv_Index = (int)hv_Index + 1)
            {
                //-----對應方向的線段-----*
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_RegionLineSelected.Dispose();
                    HOperatorSet.SelectObj(ho_OuterRegionLines, out ho_RegionLineSelected, hv_Index + 1);
                }

                //-----將四周方格區域與對應的邊合併-----*
                ho_RegionUnion6.Dispose();
                HOperatorSet.Union2(ho_OuterRegions, ho_RegionLineSelected, out ho_RegionUnion6
                    );
                //-----將合併後的區域分開，找出最大面積就是對應邊的方塊區域+線段-----*
                ho_ConnectedRegions2.Dispose();
                HOperatorSet.Connection(ho_RegionUnion6, out ho_ConnectedRegions2);
                ho_RegionOut.Dispose();
                RemoveRegionByArea(ho_ConnectedRegions2, out ho_RegionOut, 15);
                //-----因為方塊區域有合併線段，所以找出與方塊重疊區域，即為對應方塊區域-----*
                ho_RegionIntersection1.Dispose();
                HOperatorSet.Intersection(ho_OuterRegions, ho_RegionOut, out ho_RegionIntersection1
                    );
                //-----將方塊區域連通-----*
                ho_ConnectedRegions3.Dispose();
                HOperatorSet.Connection(ho_RegionIntersection1, out ho_ConnectedRegions3);

                //=====判斷上下左右區域=====*
                switch (hv_Index.I)
                {
                    //-----上、下邊檢測-----*
                    case 0:
                    case 2:
                        //-----左至右排序-----*
                        ho_SortedRegions2.Dispose();
                        HOperatorSet.SortRegion(ho_ConnectedRegions3, out ho_SortedRegions2, "character",
                            "true", "row");

                        //-----移除頭尾兩個Region-----*
                        hv_Number.Dispose();
                        HOperatorSet.CountObj(ho_SortedRegions2, out hv_Number);
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_ObjectsReduced.Dispose();
                            HOperatorSet.RemoveObj(ho_SortedRegions2, out ho_ObjectsReduced, (new HTuple(1)).TupleConcat(
                                hv_Number));
                        }

                        //-----找出上、下區域的斷線區域-----*
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_RegionUnion7.Dispose();
                            SearchMissingLine(ho_ObjectsReduced, out ho_RegionUnion7, hv_GridWidthPixel * 1000,
                                hv_GridHeightPixel);
                        }

                        break;

                    //-----左、右邊檢測-----*
                    case 1:
                    case 3:
                        //-----找出左、右區域的斷線區域-----*
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_RegionUnion7.Dispose();
                            SearchMissingLine(ho_ConnectedRegions3, out ho_RegionUnion7, hv_GridWidthPixel,
                                hv_GridHeightPixel * 1000);
                        }

                        break;

                }

                //-----合併四邊斷線區域-----*
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.Union2(ho_OuterMissingLineRegions, ho_RegionUnion7, out ExpTmpOutVar_0
                        );
                    ho_OuterMissingLineRegions.Dispose();
                    ho_OuterMissingLineRegions = ExpTmpOutVar_0;
                }

            }

            //-----以矩形標記斷線區域-----*
            ho_RegionUnion2.Dispose();
            HOperatorSet.Union2(ho_OuterMissingLineRegions, ho_InnerMissingLineRegions, out ho_RegionUnion2
                );
            ho_MissingLineRegion.Dispose();
            HOperatorSet.Connection(ho_RegionUnion2, out ho_MissingLineRegion);

            ho_RegionBorder1.Dispose();
            MarkDefectArea(ho_MissingLineRegion, out ho_RegionBorder1, 5, 30);
            //-----計算斷線距離-----
            hv_FinalResult.Dispose();
            CalculateMissingLineDistance(ho_ImageReduced, ho_MissingLineRegion, ho_RegionBorder,
                ho_RegionDynThresh, hv_ZoomFactor, hv_GridWidthPixel, hv_GridHeightPixel,
                out hv_FinalResult);

            ho_Region.Dispose();
            ho_RegionTrans.Dispose();
            ho_RegionBorder.Dispose();
            ho_ImageReduced.Dispose();
            ho_RegionTrans2.Dispose();
            ho_RegionErosion2.Dispose();
            ho_ImageReduced1.Dispose();
            ho_ImageMean.Dispose();
            ho_RegionDynThresh.Dispose();
            ho_ImageReduced2.Dispose();
            ho_RegionUnionX.Dispose();
            ho_RegionUnionY.Dispose();
            ho_RegionDilationX.Dispose();
            ho_BorderRegionDilation.Dispose();
            ho_RegionDifferenceX.Dispose();
            ho_RegionIntersectionX.Dispose();
            ho_ImageReducedX.Dispose();
            ho_ImageMeanX.Dispose();
            ho_RegionDynThreshX.Dispose();
            ho_RegionDilationY.Dispose();
            ho_RegionDifferenceY.Dispose();
            ho_RegionIntersectionY.Dispose();
            ho_ImageReducedY.Dispose();
            ho_ImageMeanY.Dispose();
            ho_RegionDynThreshY.Dispose();
            ho_RegionDilation.Dispose();
            ho_RegionUnion.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_SelectedRegions.Dispose();
            ho_RegionFillUp.Dispose();
            ho_RegionDifference.Dispose();
            ho_AllRegions.Dispose();
            ho_RegionTrans4.Dispose();
            ho_RegionErosion1.Dispose();
            ho_RegionBorder3.Dispose();
            ho_RegionUnion5.Dispose();
            ho_ConnectedRegions6.Dispose();
            ho_InnerRegions.Dispose();
            ho_InnerMissingLineRegions.Dispose();
            ho_OuterMissingLineRegions.Dispose();
            ho_AllRegionsUnion.Dispose();
            ho_InnerRegionsUnion.Dispose();
            ho_OuterRegions.Dispose();
            ho_OuterRegionLines.Dispose();
            ho_RegionLineSelected.Dispose();
            ho_RegionUnion6.Dispose();
            ho_ConnectedRegions2.Dispose();
            ho_RegionOut.Dispose();
            ho_RegionIntersection1.Dispose();
            ho_ConnectedRegions3.Dispose();
            ho_SortedRegions2.Dispose();
            ho_ObjectsReduced.Dispose();
            ho_RegionUnion7.Dispose();
            ho_RegionUnion2.Dispose();
            ho_MissingLineRegion.Dispose();

            hv_Width.Dispose();
            hv_Height.Dispose();
            hv_Min1.Dispose();
            hv_Max1.Dispose();
            hv_Range2.Dispose();
            hv_Energy1.Dispose();
            hv_Correlation1.Dispose();
            hv_Homogeneity1.Dispose();
            hv_Contrast1.Dispose();
            hv_Area6.Dispose();
            hv_Row6.Dispose();
            hv_Column6.Dispose();
            hv_Area2.Dispose();
            hv_Row2.Dispose();
            hv_Column2.Dispose();
            hv_Index.Dispose();
            hv_Number.Dispose();

            return;
        }

        /// <summary>
        /// 針對0402的Line Scan影像找出斷線區域及長度
        /// </summary>
        /// <param name="ho_Image00011"></param>
        /// <param name="ho_RegionBorder1"></param>
        /// <param name="hv_IgnorePixelSize"></param>
        /// <param name="hv_GridWidthPixel"></param>
        /// <param name="hv_GridHeightPixel"></param>
        /// <param name="hv_ZoomFactor"></param>
        /// <param name="hv_FinalResult"></param>
        private void DetectMissingLine_0402(HObject ho_Image00011, out HObject ho_RegionBorder1,
      HTuple hv_GridWidthPixel, HTuple hv_GridHeightPixel, HTuple hv_ZoomFactor, HTuple hv_Factor,
      out HTuple hv_FinalResult)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_Region, ho_RegionTrans, ho_RegionBorder;
            HObject ho_ImageReduced, ho_RegionTrans2, ho_RegionErosion2;
            HObject ho_ImageReduced1, ho_ImageMean, ho_RegionDynThresh1;
            HObject ho_ImageReduced2, ho_RegionDynThresh, ho_RegionDilation;
            HObject ho_RegionUnion, ho_ConnectedRegions, ho_SelectedRegions;
            HObject ho_RegionFillUp, ho_RegionDifference, ho_AllRegions;
            HObject ho_RegionTrans4, ho_RegionErosion1, ho_RegionBorder3;
            HObject ho_RegionUnion5, ho_ConnectedRegions6, ho_InnerRegions;
            HObject ho_InnerMissingLineRegions, ho_OuterMissingLineRegions;
            HObject ho_AllRegionsUnion, ho_InnerRegionsUnion, ho_OuterRegions;
            HObject ho_OuterRegionLines, ho_RegionLineSelected = null;
            HObject ho_RegionUnion6 = null, ho_ConnectedRegions2 = null;
            HObject ho_RegionOut = null, ho_RegionIntersection1 = null;
            HObject ho_ConnectedRegions3 = null, ho_SortedRegions2 = null;
            HObject ho_ObjectsReduced = null, ho_RegionUnion7 = null, ho_RegionUnion2;
            HObject ho_MissingLineRegion;

            // Local control variables 

            HTuple hv_Width = new HTuple(), hv_Height = new HTuple();
            HTuple hv_Min1 = new HTuple(), hv_Max1 = new HTuple();
            HTuple hv_Range2 = new HTuple(), hv_Energy1 = new HTuple();
            HTuple hv_Correlation1 = new HTuple(), hv_Homogeneity1 = new HTuple();
            HTuple hv_Contrast1 = new HTuple(), hv_Area6 = new HTuple();
            HTuple hv_Row6 = new HTuple(), hv_Column6 = new HTuple();
            HTuple hv_Area2 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Column2 = new HTuple(), hv_Index = new HTuple();
            HTuple hv_Number = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_RegionBorder1);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans2);
            HOperatorSet.GenEmptyObj(out ho_RegionErosion2);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced1);
            HOperatorSet.GenEmptyObj(out ho_ImageMean);
            HOperatorSet.GenEmptyObj(out ho_RegionDynThresh1);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced2);
            HOperatorSet.GenEmptyObj(out ho_RegionDynThresh);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference);
            HOperatorSet.GenEmptyObj(out ho_AllRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans4);
            HOperatorSet.GenEmptyObj(out ho_RegionErosion1);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder3);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion5);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions6);
            HOperatorSet.GenEmptyObj(out ho_InnerRegions);
            HOperatorSet.GenEmptyObj(out ho_InnerMissingLineRegions);
            HOperatorSet.GenEmptyObj(out ho_OuterMissingLineRegions);
            HOperatorSet.GenEmptyObj(out ho_AllRegionsUnion);
            HOperatorSet.GenEmptyObj(out ho_InnerRegionsUnion);
            HOperatorSet.GenEmptyObj(out ho_OuterRegions);
            HOperatorSet.GenEmptyObj(out ho_OuterRegionLines);
            HOperatorSet.GenEmptyObj(out ho_RegionLineSelected);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion6);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions2);
            HOperatorSet.GenEmptyObj(out ho_RegionOut);
            HOperatorSet.GenEmptyObj(out ho_RegionIntersection1);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions3);
            HOperatorSet.GenEmptyObj(out ho_SortedRegions2);
            HOperatorSet.GenEmptyObj(out ho_ObjectsReduced);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion7);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion2);
            HOperatorSet.GenEmptyObj(out ho_MissingLineRegion);
            hv_FinalResult = new HTuple();
            //=====找出待測物範圍=====*
            ho_Region.Dispose(); ho_RegionTrans.Dispose(); ho_RegionBorder.Dispose();
            FindFrontRegion(ho_Image00011, out ho_Region, out ho_RegionTrans, out ho_RegionBorder
                );
            ho_ImageReduced.Dispose();
            HOperatorSet.ReduceDomain(ho_Image00011, ho_RegionTrans, out ho_ImageReduced);

            //-----找出板子灰度區間-----*
            ho_RegionTrans2.Dispose();
            HOperatorSet.ShapeTrans(ho_Region, out ho_RegionTrans2, "rectangle1");
            hv_Width.Dispose();
            HOperatorSet.RegionFeatures(ho_RegionTrans2, "width", out hv_Width);
            hv_Height.Dispose();
            HOperatorSet.RegionFeatures(ho_RegionTrans2, "height", out hv_Height);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionErosion2.Dispose();
                HOperatorSet.ErosionRectangle1(ho_RegionTrans2, out ho_RegionErosion2, hv_Width / 15,
                    hv_Height / 15);
            }
            ho_ImageReduced1.Dispose();
            HOperatorSet.ReduceDomain(ho_Image00011, ho_RegionErosion2, out ho_ImageReduced1
                );
            //-----決定Local threshold的Range-----*
            hv_Min1.Dispose(); hv_Max1.Dispose(); hv_Range2.Dispose();
            HOperatorSet.MinMaxGray(ho_ImageReduced1, ho_ImageReduced1, 0, out hv_Min1, out hv_Max1,
                out hv_Range2);

            //-----取得影像對比度-----*
            hv_Energy1.Dispose(); hv_Correlation1.Dispose(); hv_Homogeneity1.Dispose(); hv_Contrast1.Dispose();
            HOperatorSet.CoocFeatureImage(ho_RegionTrans, ho_Image00011, 6, 0, out hv_Energy1,
                out hv_Correlation1, out hv_Homogeneity1, out hv_Contrast1);

            //-----先大略篩出線段區域，以加快local_threshold的速度-----*
            ho_ImageMean.Dispose();
            HOperatorSet.MeanImage(ho_ImageReduced, out ho_ImageMean, 31, 31);
            ho_RegionDynThresh1.Dispose();
            HOperatorSet.DynThreshold(ho_ImageReduced, ho_ImageMean, out ho_RegionDynThresh1,
                5, "dark");
            ho_ImageReduced2.Dispose();
            HOperatorSet.ReduceDomain(ho_ImageReduced, ho_RegionDynThresh1, out ho_ImageReduced2
                );

            //-----0.1~2 (鬆~嚴)-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionDynThresh.Dispose();
                HOperatorSet.LocalThreshold(ho_ImageReduced2, out ho_RegionDynThresh, "adapted_std_deviation",
                    "dark", ((new HTuple("mask_size")).TupleConcat("scale")).TupleConcat("range"),
                    ((((25 * hv_ZoomFactor)).TupleConcat((hv_Contrast1 / 10) * hv_Factor))).TupleConcat(
                    hv_Range2 / 2));
            }
            //scale: 調整線段與底色對比，對比高數值越高，反之則反

            //-----4邊往內縮一段不做判斷的區域-----*
            ho_RegionDilation.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionBorder, out ho_RegionDilation, 25, 25);

            //-----合併邊界與二值化結果-----*
            ho_RegionUnion.Dispose();
            HOperatorSet.Union2(ho_RegionDynThresh, ho_RegionDilation, out ho_RegionUnion
                );

            //-----去除線段以外的雜點-----*
            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_RegionUnion, out ho_ConnectedRegions);
            hv_Area6.Dispose(); hv_Row6.Dispose(); hv_Column6.Dispose();
            HOperatorSet.AreaCenter(ho_ConnectedRegions, out hv_Area6, out hv_Row6, out hv_Column6);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_SelectedRegions, "area",
                    "and", 150, (hv_Area6.TupleMax()) + 1);
            }

            //-----將線段填滿-----*
            ho_RegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_SelectedRegions, out ho_RegionFillUp);

            //-----找出填滿區域與二值化區域不同處，即可找出所有方塊區域-----*
            ho_RegionDifference.Dispose();
            HOperatorSet.Difference(ho_RegionFillUp, ho_ConnectedRegions, out ho_RegionDifference
                );

            //-----分離所有方塊區域-----*
            ho_AllRegions.Dispose();
            HOperatorSet.Connection(ho_RegionDifference, out ho_AllRegions);

            //=====四周往內縮一格=====*
            //-----根據基板形狀建立一個矩形-----*
            ho_RegionTrans4.Dispose();
            HOperatorSet.ShapeTrans(ho_Region, out ho_RegionTrans4, "rectangle2");

            //-----將矩形往內縮小-----*
            ho_RegionErosion1.Dispose();
            HOperatorSet.ErosionRectangle1(ho_RegionTrans4, out ho_RegionErosion1, 120, 120);

            //-----找出矩陣縮小後的邊框-----*
            ho_RegionBorder3.Dispose();
            HOperatorSet.Boundary(ho_RegionErosion1, out ho_RegionBorder3, "inner");

            //-----與所有方塊區域合併-----*
            ho_RegionUnion5.Dispose();
            HOperatorSet.Union2(ho_RegionBorder3, ho_RegionDifference, out ho_RegionUnion5
                );

            //-----分離方塊後，將最大面積移除，即可得到內圈方塊-----*
            ho_ConnectedRegions6.Dispose();
            HOperatorSet.Connection(ho_RegionUnion5, out ho_ConnectedRegions6);
            hv_Area2.Dispose(); hv_Row2.Dispose(); hv_Column2.Dispose();
            HOperatorSet.AreaCenter(ho_ConnectedRegions6, out hv_Area2, out hv_Row2, out hv_Column2);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_InnerRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions6, out ho_InnerRegions, "area", "and",
                    1, (hv_Area2.TupleMax()) - 1);
            }

            //=====內圈方塊處理=====*
            //-----找出內圈斷線區域-----*
            ho_InnerMissingLineRegions.Dispose();
            SearchMissingLine(ho_InnerRegions, out ho_InnerMissingLineRegions, hv_GridWidthPixel,
                hv_GridHeightPixel);

            //=====處理外圈方塊區域=====*
            ho_OuterMissingLineRegions.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_OuterMissingLineRegions);

            //-----篩出外圈方塊區域-----*
            ho_AllRegionsUnion.Dispose();
            HOperatorSet.Union1(ho_AllRegions, out ho_AllRegionsUnion);
            ho_InnerRegionsUnion.Dispose();
            HOperatorSet.Union1(ho_InnerRegions, out ho_InnerRegionsUnion);
            ho_OuterRegions.Dispose();
            HOperatorSet.Difference(ho_AllRegionsUnion, ho_InnerRegionsUnion, out ho_OuterRegions
                );

            //-----產生邊界內縮線段-----*
            ho_OuterRegionLines.Dispose();
            FindOuterLines(ho_RegionBorder3, out ho_OuterRegionLines);

            for (hv_Index = 0; (int)hv_Index <= 3; hv_Index = (int)hv_Index + 1)
            {
                //-----對應方向的線段-----*
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_RegionLineSelected.Dispose();
                    HOperatorSet.SelectObj(ho_OuterRegionLines, out ho_RegionLineSelected, hv_Index + 1);
                }

                //-----將四周方格區域與對應的邊合併-----*
                ho_RegionUnion6.Dispose();
                HOperatorSet.Union2(ho_OuterRegions, ho_RegionLineSelected, out ho_RegionUnion6
                    );
                //-----將合併後的區域分開，找出最大面積就是對應邊的方塊區域+線段-----*
                ho_ConnectedRegions2.Dispose();
                HOperatorSet.Connection(ho_RegionUnion6, out ho_ConnectedRegions2);
                ho_RegionOut.Dispose();
                RemoveRegionByArea(ho_ConnectedRegions2, out ho_RegionOut, 15);
                //-----因為方塊區域有合併線段，所以找出與方塊重疊區域，即為對應方塊區域-----*
                ho_RegionIntersection1.Dispose();
                HOperatorSet.Intersection(ho_OuterRegions, ho_RegionOut, out ho_RegionIntersection1
                    );
                //-----將方塊區域連通-----*
                ho_ConnectedRegions3.Dispose();
                HOperatorSet.Connection(ho_RegionIntersection1, out ho_ConnectedRegions3);

                //=====判斷上下左右區域=====*
                switch (hv_Index.I)
                {
                    //-----上、下邊檢測-----*
                    case 0:
                    case 2:
                        //-----左至右排序-----*
                        ho_SortedRegions2.Dispose();
                        HOperatorSet.SortRegion(ho_ConnectedRegions3, out ho_SortedRegions2, "character",
                            "true", "row");

                        //-----移除頭尾兩個Region-----*
                        hv_Number.Dispose();
                        HOperatorSet.CountObj(ho_SortedRegions2, out hv_Number);
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_ObjectsReduced.Dispose();
                            HOperatorSet.RemoveObj(ho_SortedRegions2, out ho_ObjectsReduced, (new HTuple(1)).TupleConcat(
                                hv_Number));
                        }

                        //-----找出上、下區域的斷線區域；不看Y軸線段漏線，因此將寬度設大-----*
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_RegionUnion7.Dispose();
                            SearchMissingLine(ho_ObjectsReduced, out ho_RegionUnion7, hv_GridWidthPixel * 1000,
                                hv_GridHeightPixel * 1.5);
                        }

                        break;

                    //-----左、右邊檢測-----*
                    case 1:
                    case 3:
                        //-----找出左、右區域的斷線區域；不看X軸線段漏線，因此將高度設大-----*
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_RegionUnion7.Dispose();
                            SearchMissingLine(ho_ConnectedRegions3, out ho_RegionUnion7, hv_GridWidthPixel * 2,
                                hv_GridHeightPixel * 1000);
                        }

                        break;

                }

                //-----合併四邊斷線區域-----*
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.Union2(ho_OuterMissingLineRegions, ho_RegionUnion7, out ExpTmpOutVar_0
                        );
                    ho_OuterMissingLineRegions.Dispose();
                    ho_OuterMissingLineRegions = ExpTmpOutVar_0;
                }

            }

            //-----以矩形標記斷線區域-----*
            ho_RegionUnion2.Dispose();
            HOperatorSet.Union2(ho_OuterMissingLineRegions, ho_InnerMissingLineRegions, out ho_RegionUnion2
                );
            ho_MissingLineRegion.Dispose();
            HOperatorSet.Connection(ho_RegionUnion2, out ho_MissingLineRegion);
            ho_RegionBorder1.Dispose();
            MarkDefectArea(ho_MissingLineRegion, out ho_RegionBorder1, 5, 30);

            //-----計算斷線距離-----*
            hv_FinalResult.Dispose();
            CalculateMissingLineDistance(ho_ImageReduced, ho_MissingLineRegion, ho_RegionBorder,
                ho_RegionDynThresh, hv_ZoomFactor, hv_GridWidthPixel, hv_GridHeightPixel,
                out hv_FinalResult);

            ho_Region.Dispose();
            ho_RegionTrans.Dispose();
            ho_RegionBorder.Dispose();
            ho_ImageReduced.Dispose();
            ho_RegionTrans2.Dispose();
            ho_RegionErosion2.Dispose();
            ho_ImageReduced1.Dispose();
            ho_ImageMean.Dispose();
            ho_RegionDynThresh1.Dispose();
            ho_ImageReduced2.Dispose();
            ho_RegionDynThresh.Dispose();
            ho_RegionDilation.Dispose();
            ho_RegionUnion.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_SelectedRegions.Dispose();
            ho_RegionFillUp.Dispose();
            ho_RegionDifference.Dispose();
            ho_AllRegions.Dispose();
            ho_RegionTrans4.Dispose();
            ho_RegionErosion1.Dispose();
            ho_RegionBorder3.Dispose();
            ho_RegionUnion5.Dispose();
            ho_ConnectedRegions6.Dispose();
            ho_InnerRegions.Dispose();
            ho_InnerMissingLineRegions.Dispose();
            ho_OuterMissingLineRegions.Dispose();
            ho_AllRegionsUnion.Dispose();
            ho_InnerRegionsUnion.Dispose();
            ho_OuterRegions.Dispose();
            ho_OuterRegionLines.Dispose();
            ho_RegionLineSelected.Dispose();
            ho_RegionUnion6.Dispose();
            ho_ConnectedRegions2.Dispose();
            ho_RegionOut.Dispose();
            ho_RegionIntersection1.Dispose();
            ho_ConnectedRegions3.Dispose();
            ho_SortedRegions2.Dispose();
            ho_ObjectsReduced.Dispose();
            ho_RegionUnion7.Dispose();
            ho_RegionUnion2.Dispose();
            ho_MissingLineRegion.Dispose();

            hv_Width.Dispose();
            hv_Height.Dispose();
            hv_Min1.Dispose();
            hv_Max1.Dispose();
            hv_Range2.Dispose();
            hv_Energy1.Dispose();
            hv_Correlation1.Dispose();
            hv_Homogeneity1.Dispose();
            hv_Contrast1.Dispose();
            hv_Area6.Dispose();
            hv_Row6.Dispose();
            hv_Column6.Dispose();
            hv_Area2.Dispose();
            hv_Row2.Dispose();
            hv_Column2.Dispose();
            hv_Index.Dispose();
            hv_Number.Dispose();

        }

        /// <summary>
        /// 找出斷線區域
        /// </summary>
        /// <param name="ho_Regions"></param>
        /// <param name="ho_MissingLineRegions"></param>
        /// <param name="hv_IgnoreWidth"></param>
        /// <param name="hv_IgnoreHeight"></param>
        /// <param name="hv_GridWidthPixel"></param>
        /// <param name="hv_GridHeightPixel"></param>
        private void SearchMissingLine(HObject ho_Regions, out HObject ho_MissingLineRegions,
      HTuple hv_GridWidthPixel, HTuple hv_GridHeightPixel)
        {




            // Local iconic variables 

            HObject ho_ConnectedRegions;
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_MissingLineRegions);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            //-----將經過侵蝕後斷開的方塊區域分離-----*
            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_Regions, out ho_ConnectedRegions);

            //-----找出格子寬、高大於格子尺寸的區域-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_MissingLineRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_MissingLineRegions, (new HTuple("width")).TupleConcat(
                    "height"), "or", ((hv_GridWidthPixel * 1.5)).TupleConcat(hv_GridHeightPixel * 1.5),
                    (new HTuple(9999999)).TupleConcat(9999999));
            }

            ho_ConnectedRegions.Dispose();


            return;
        }

        /// <summary>
        /// 根據面積大小篩選出對應區域
        /// </summary>
        /// <param name="ho_Region"></param>
        /// <param name="ho_RegionOut"></param>
        /// <param name="hv_Factor"></param>
        private void RemoveRegionByArea(HObject ho_Region, out HObject ho_RegionOut, HTuple hv_Factor)
        {




            // Local iconic variables 

            HObject ho_SelectedRegions;

            // Local control variables 

            HTuple hv_Area2 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Column2 = new HTuple(), hv_Int1 = new HTuple();
            HTuple hv_IntMean = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_RegionOut);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            //-----將雜點過濾-----*
            ho_SelectedRegions.Dispose();
            HOperatorSet.SelectShape(ho_Region, out ho_SelectedRegions, "area", "and", 500,
                9999999);

            //-----找出所有方塊位置及面積-----*
            hv_Area2.Dispose(); hv_Row2.Dispose(); hv_Column2.Dispose();
            HOperatorSet.AreaCenter(ho_SelectedRegions, out hv_Area2, out hv_Row2, out hv_Column2);

            if ((int)(new HTuple((new HTuple(hv_Area2.TupleLength())).TupleGreater(0))) != 0)
            {
                //-----算出所有方塊面積的均值-----*
                hv_Int1.Dispose();
                HOperatorSet.TupleInt(hv_Area2, out hv_Int1);
                hv_IntMean.Dispose();
                HOperatorSet.TupleMean(hv_Int1, out hv_IntMean);

                //-----篩出較大面積的方塊，即為斷線區域-----*
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_RegionOut.Dispose();
                    HOperatorSet.SelectShape(ho_SelectedRegions, out ho_RegionOut, "area", "and",
                        hv_IntMean + (hv_IntMean * hv_Factor), 99999999);
                }
            }
            else
            {
                ho_RegionOut.Dispose();
                HOperatorSet.GenEmptyRegion(out ho_RegionOut);
            }

            ho_SelectedRegions.Dispose();

            hv_Area2.Dispose();
            hv_Row2.Dispose();
            hv_Column2.Dispose();
            hv_Int1.Dispose();
            hv_IntMean.Dispose();

            return;
        }

        /// <summary>
        /// 計算漏線距離，要分離的次數改為用算的
        /// </summary>
        /// <param name="ho_MissingLineRegion"></param>
        /// <param name="hv_ZoomFactor"></param>
        /// <param name="hv_GridWidth"></param>
        /// <param name="hv_GridHeight"></param>
        /// <param name="hv_MissingLinePixel"></param>
        private void CalculateMissingLineDistance(HObject ho_Image, HObject ho_MissingLineRegion,
      HObject ho_RegionBorder, HObject ho_GridRegion, HTuple hv_ZoomFactor, HTuple hv_GridWidth,
      HTuple hv_GridHeight, out HTuple hv_MissingLinePixel)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_EmptyRegion, ho_RegionBorderDilation;
            HObject ho_ObjectSelected = null, ho_MissingRegion = null, ho_RegionDilation = null;
            HObject ho_RegionClipped1 = null, ho_RegionUnion1 = null, ho_RegionUnion = null;
            HObject ho_RegionUnion4 = null, ho_RegionClipped = null, ho_RegionFillUp = null;
            HObject ho_RegionDilation1 = null, ho_RegionDifference1 = null;
            HObject ho_RegionUnion2 = null, ho_RegionOpening = null, ho_ConnectedRegions1 = null;
            HObject ho_RegionSelected = null, ho_ObjectsReduced = null;

            // Local control variables 

            HTuple hv_MLNumber = new HTuple(), hv_i = new HTuple();
            HTuple hv_Area1 = new HTuple(), hv_Row1 = new HTuple();
            HTuple hv_Column1 = new HTuple(), hv_Row11 = new HTuple();
            HTuple hv_Column11 = new HTuple(), hv_Row22 = new HTuple();
            HTuple hv_Column22 = new HTuple(), hv_Row21 = new HTuple();
            HTuple hv_Column21 = new HTuple(), hv_NumConnected = new HTuple();
            HTuple hv_PartitionNumber = new HTuple(), hv_OpeningSize = new HTuple();
            HTuple hv_Number = new HTuple(), hv_j = new HTuple(), hv_Height = new HTuple();
            HTuple hv_Width = new HTuple(), hv_Ratio = new HTuple();
            HTuple hv_SelectIndex = new HTuple(), hv_Index = new HTuple();
            HTuple hv_Heightk = new HTuple(), hv_Widthk = new HTuple();
            HTuple hv_Ratiok = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_EmptyRegion);
            HOperatorSet.GenEmptyObj(out ho_RegionBorderDilation);
            HOperatorSet.GenEmptyObj(out ho_ObjectSelected);
            HOperatorSet.GenEmptyObj(out ho_MissingRegion);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_RegionClipped1);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion1);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion4);
            HOperatorSet.GenEmptyObj(out ho_RegionClipped);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation1);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference1);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion2);
            HOperatorSet.GenEmptyObj(out ho_RegionOpening);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_RegionSelected);
            HOperatorSet.GenEmptyObj(out ho_ObjectsReduced);
            hv_MissingLinePixel = new HTuple();
            //-----變數建立-----*
            hv_MissingLinePixel.Dispose();
            hv_MissingLinePixel = new HTuple();
            ho_EmptyRegion.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_EmptyRegion);

            //-----外框-----*
            ho_RegionBorderDilation.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionBorder, out ho_RegionBorderDilation,
                31, 31);
            //-----若沒斷線就不做判斷-----*
            if ((int)((new HTuple(ho_MissingLineRegion.TestEqualObj(ho_EmptyRegion)).TupleNot())) != 0)
            {
                //-----統計斷線區域數量-----*
                hv_MLNumber.Dispose();
                HOperatorSet.CountObj(ho_MissingLineRegion, out hv_MLNumber);
                HTuple end_val10 = hv_MLNumber;
                HTuple step_val10 = 1;
                for (hv_i = 1; hv_i.Continue(end_val10, step_val10); hv_i = hv_i.TupleAdd(step_val10))
                {
                    //-----依序選擇斷線區域-----*
                    ho_ObjectSelected.Dispose();
                    HOperatorSet.SelectObj(ho_MissingLineRegion, out ho_ObjectSelected, hv_i);
                    //-----計算斷線處的區域-----*
                    hv_Area1.Dispose(); hv_Row1.Dispose(); hv_Column1.Dispose();
                    HOperatorSet.AreaCenter(ho_ObjectSelected, out hv_Area1, out hv_Row1, out hv_Column1);

                    //=====新方法找出要分割成幾個區塊=====*
                    ho_MissingRegion.Dispose();
                    HOperatorSet.DilationRectangle1(ho_ObjectSelected, out ho_MissingRegion,
                        15, 15);

                    //-----根據格子尺寸，擴大判斷區域-----*
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        ho_RegionDilation.Dispose();
                        HOperatorSet.DilationRectangle1(ho_ObjectSelected, out ho_RegionDilation,
                            hv_GridWidth / 2, hv_GridHeight / 2);
                    }

                    //-----將NG範圍從二值化後的區域中裁出-----*
                    hv_Row11.Dispose(); hv_Column11.Dispose(); hv_Row22.Dispose(); hv_Column22.Dispose();
                    HOperatorSet.SmallestRectangle1(ho_RegionDilation, out hv_Row11, out hv_Column11,
                        out hv_Row22, out hv_Column22);
                    ho_RegionClipped1.Dispose();
                    HOperatorSet.ClipRegion(ho_GridRegion, out ho_RegionClipped1, hv_Row11, hv_Column11,
                        hv_Row22, hv_Column22);
                    //*從線段找出直線橫線
                    ho_RegionUnion1.Dispose(); ho_RegionUnion.Dispose();
                    ExtractRowAndColLines(ho_RegionClipped1, out ho_RegionUnion1, out ho_RegionUnion,
                        hv_GridHeight, hv_GridWidth, hv_ZoomFactor);
                    //-----濾掉較短線段-----*
                    //connection (RegionClipped1, ConnectedRegions2)
                    //select_shape (ConnectedRegions2, SelectedRegions, 'max_diameter', 'and', GridHeight/2, 99999)
                    //skeleton (SelectedRegions, Skeleton)

                    //-----找出每個線段交點，並加大交點區域-----*
                    //junctions_skeleton (Skeleton, EndPoints, JuncPoints)
                    //dilation_circle (JuncPoints, RegionDilation, 2)

                    //-----找出交點區域外的線段區域-----*
                    //difference (Skeleton, RegionDilation, RegionDifference)
                    //connection (RegionDifference, ConnectedRegions)

                    //-----避免X與Y線段相連造成後續誤判，將線段拆成不同區域-----*
                    //split_skeleton_region (ConnectedRegions, RegionLines, 5)
                    //select_shape (RegionLines, SelectedRegions1, 'max_diameter', 'and', 12 * ZoomFactor, 99999)

                    //-----找XY線段方式-----*
                    //orientation_region (SelectedRegions1, Phi)
                    //select_shape (SelectedRegions1, SelectedRegions3, ['orientation','orientation'], 'or', [rad(80),rad(-100)], [rad(100),rad(-80)])
                    //select_shape (SelectedRegions1, SelectedRegions4, ['orientation','orientation','orientation'], 'or', [rad(-10),rad(170),rad(-190)], [rad(10),rad(190),rad(-170)])

                    //-----將Y方向線段做延申，將其連接在一起-----*
                    //dilation_rectangle1 (SelectedRegions3, RegionDilation3, 2, GridHeight * 5)
                    //union1 (RegionDilation3, RegionUnion)

                    //-----將X方向線段做延申，將其連接在一起-----*
                    //dilation_rectangle1 (SelectedRegions4, RegionDilation5, GridWidth * 5, 2)
                    //union1 (RegionDilation5, RegionUnion1)

                    //-----連接XY方向線段-----*
                    ho_RegionUnion4.Dispose();
                    HOperatorSet.Union2(ho_RegionUnion, ho_RegionUnion1, out ho_RegionUnion4);

                    //-----將XY及外框方向聯通-----*
                    ho_RegionUnion.Dispose();
                    HOperatorSet.Union2(ho_RegionUnion4, ho_RegionBorderDilation, out ho_RegionUnion
                        );

                    //-----取得斷線區域4角座標-----*
                    hv_Row1.Dispose(); hv_Column1.Dispose(); hv_Row21.Dispose(); hv_Column21.Dispose();
                    HOperatorSet.SmallestRectangle1(ho_MissingRegion, out hv_Row1, out hv_Column1,
                        out hv_Row21, out hv_Column21);

                    //-----將斷線區域裁出-----*
                    //--將斷線區域補滿--*
                    {
                        HObject ExpTmpOutVar_0;
                        HOperatorSet.ShapeTrans(ho_MissingRegion, out ExpTmpOutVar_0, "rectangle2");
                        ho_MissingRegion.Dispose();
                        ho_MissingRegion = ExpTmpOutVar_0;
                    }
                    ho_RegionClipped.Dispose();
                    HOperatorSet.Intersection(ho_RegionUnion, ho_MissingRegion, out ho_RegionClipped
                        );

                    //-----計算洞數-----*
                    hv_NumConnected.Dispose(); hv_PartitionNumber.Dispose();
                    HOperatorSet.ConnectAndHoles(ho_RegionClipped, out hv_NumConnected, out hv_PartitionNumber);

                    //-----Opening變數-----*
                    hv_OpeningSize.Dispose();
                    hv_OpeningSize = 1;
                    hv_Number.Dispose();
                    hv_Number = 1;
                    //-----計數變數歸0-----*
                    hv_j.Dispose();
                    hv_j = 0;

                    //-----把斷線區域線段以外的區域做填滿，避免有汙染影響長度計算-----*
                    ho_RegionFillUp.Dispose();
                    HOperatorSet.FillUp(ho_RegionClipped, out ho_RegionFillUp);
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        ho_RegionDilation1.Dispose();
                        HOperatorSet.DilationRectangle1(ho_RegionClipped, out ho_RegionDilation1,
                            hv_GridWidth * 0.1, hv_GridHeight * 0.1);
                    }
                    ho_RegionDifference1.Dispose();
                    HOperatorSet.Difference(ho_RegionFillUp, ho_RegionDilation1, out ho_RegionDifference1
                        );
                    ho_RegionUnion2.Dispose();
                    HOperatorSet.Union2(ho_RegionDifference1, ho_ObjectSelected, out ho_RegionUnion2
                        );
                    //*補追加未漏線的部分,因灰階或其他原因造成空隙
                    //*全填滿-漏線區域-線段=未填滿部分
                    //fill_up (Image, RegionFill)
                    //*fill-格線=格線內區域
                    //difference (RegionFill, GridRegion, RegionDifference4)
                    //*全填滿-漏線區域=格線+格線內未填滿的線段和區域
                    //difference (RegionFill, ObjectSelected, RegionDifference2)
                    //*留下面積大的
                    //connection (RegionDifference2, ConnectedRegions3)
                    //area_center (ConnectedRegions3, AreaMax, RowMax, ColumnMax)
                    //select_shape (ConnectedRegions3, SelectedRegionsMax, 'area', 'and', max(AreaMax), max(AreaMax)+1)
                    //*全填滿-漏線區域-留下面積大的部分=格線+格線內未填滿的線段和區域
                    //difference (RegionDifference2, SelectedRegionsMax, RegionDifference3)
                    //*將格線+格線內未填滿的線段和區域加回漏線區域
                    //union2 (RegionUnion2, ObjectSelected, RegionUnion2)
                    //union2 (RegionDifference3, RegionUnion2, RegionUnion2)
                    //-----逐漸增加Opening尺寸，直到分離出孔洞數量區域-----*
                    if ((int)(new HTuple(hv_PartitionNumber.TupleEqual(1))) != 0)
                    {
                        hv_Height.Dispose(); hv_Width.Dispose(); hv_Ratio.Dispose();
                        HOperatorSet.HeightWidthRatio(ho_RegionUnion2, out hv_Height, out hv_Width,
                            out hv_Ratio);
                        hv_OpeningSize.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_OpeningSize = hv_Width + 1;
                        }
                    }
                    else
                    {

                        while ((int)((new HTuple(hv_Number.TupleLess(hv_PartitionNumber))).TupleAnd(
                            new HTuple(hv_j.TupleLess(hv_GridWidth)))) != 0)
                        {
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_OpeningSize = hv_OpeningSize + 1;
                                    hv_OpeningSize.Dispose();
                                    hv_OpeningSize = ExpTmpLocalVar_OpeningSize;
                                }
                            }
                            ho_RegionOpening.Dispose();
                            HOperatorSet.OpeningRectangle1(ho_RegionUnion2, out ho_RegionOpening,
                                hv_OpeningSize, hv_OpeningSize);
                            ho_ConnectedRegions1.Dispose();
                            HOperatorSet.Connection(ho_RegionOpening, out ho_ConnectedRegions1);
                            hv_Number.Dispose();
                            HOperatorSet.CountObj(ho_ConnectedRegions1, out hv_Number);
                            //*檢查分離後全部區域長寬,若太小就忽略
                            hv_SelectIndex.Dispose();
                            hv_SelectIndex = new HTuple();
                            HTuple end_val114 = hv_Number;
                            HTuple step_val114 = 1;
                            for (hv_Index = 1; hv_Index.Continue(end_val114, step_val114); hv_Index = hv_Index.TupleAdd(step_val114))
                            {
                                ho_RegionSelected.Dispose();
                                HOperatorSet.SelectObj(ho_ConnectedRegions1, out ho_RegionSelected,
                                    hv_Index);

                                hv_Heightk.Dispose(); hv_Widthk.Dispose(); hv_Ratiok.Dispose();
                                HOperatorSet.HeightWidthRatio(ho_RegionSelected, out hv_Heightk, out hv_Widthk,
                                    out hv_Ratiok);
                                if ((int)((new HTuple(hv_Heightk.TupleLess(hv_GridHeight * 0.6))).TupleOr(
                                    new HTuple(hv_Widthk.TupleLess(hv_GridWidth * 0.6)))) != 0)
                                {
                                    hv_SelectIndex.Dispose();
                                    hv_SelectIndex = new HTuple(hv_Index);
                                }
                            }
                            ho_ObjectsReduced.Dispose();
                            HOperatorSet.RemoveObj(ho_ConnectedRegions1, out ho_ObjectsReduced, hv_SelectIndex);
                            //確認完長寬,在更新Number
                            hv_Number.Dispose();
                            HOperatorSet.CountObj(ho_ObjectsReduced, out hv_Number);
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_j = hv_j + 1;
                                    hv_j.Dispose();
                                    hv_j = ExpTmpLocalVar_j;
                                }
                            }
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_MissingLinePixel = hv_MissingLinePixel.TupleConcat(
                                (hv_OpeningSize - 1) / hv_ZoomFactor);
                            hv_MissingLinePixel.Dispose();
                            hv_MissingLinePixel = ExpTmpLocalVar_MissingLinePixel;
                        }
                    }
                }
            }

            //select_obj (MissingLineRegion, ObjectSelected1, 58)

            ho_EmptyRegion.Dispose();
            ho_RegionBorderDilation.Dispose();
            ho_ObjectSelected.Dispose();
            ho_MissingRegion.Dispose();
            ho_RegionDilation.Dispose();
            ho_RegionClipped1.Dispose();
            ho_RegionUnion1.Dispose();
            ho_RegionUnion.Dispose();
            ho_RegionUnion4.Dispose();
            ho_RegionClipped.Dispose();
            ho_RegionFillUp.Dispose();
            ho_RegionDilation1.Dispose();
            ho_RegionDifference1.Dispose();
            ho_RegionUnion2.Dispose();
            ho_RegionOpening.Dispose();
            ho_ConnectedRegions1.Dispose();
            ho_RegionSelected.Dispose();
            ho_ObjectsReduced.Dispose();

            hv_MLNumber.Dispose();
            hv_i.Dispose();
            hv_Area1.Dispose();
            hv_Row1.Dispose();
            hv_Column1.Dispose();
            hv_Row11.Dispose();
            hv_Column11.Dispose();
            hv_Row22.Dispose();
            hv_Column22.Dispose();
            hv_Row21.Dispose();
            hv_Column21.Dispose();
            hv_NumConnected.Dispose();
            hv_PartitionNumber.Dispose();
            hv_OpeningSize.Dispose();
            hv_Number.Dispose();
            hv_j.Dispose();
            hv_Height.Dispose();
            hv_Width.Dispose();
            hv_Ratio.Dispose();
            hv_SelectIndex.Dispose();
            hv_Index.Dispose();
            hv_Heightk.Dispose();
            hv_Widthk.Dispose();
            hv_Ratiok.Dispose();

            return;
        }

        /// <summary>
        /// 找到內縮後的四個邊界
        /// </summary>
        /// <param name="ho_RegionBorder"></param>
        /// <param name="ho_OuterRegionLines"></param>
        private void FindOuterLines(HObject ho_RegionBorder, out HObject ho_OuterRegionLines)
        {



            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_Contours, ho_ContoursSplit, ho_Region1;
            HObject ho_SelectedRegions1, ho_RegionUnion3, ho_ConnectedRegions4;
            HObject ho_SortedRegions1, ho_UpperRegionLine, ho_BottomRegionLine;
            HObject ho_SelectedRegions2, ho_RegionUnion1, ho_ConnectedRegions1;
            HObject ho_SortedRegions, ho_LeftRegionLine, ho_RightRegionLine;
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_OuterRegionLines);
            HOperatorSet.GenEmptyObj(out ho_Contours);
            HOperatorSet.GenEmptyObj(out ho_ContoursSplit);
            HOperatorSet.GenEmptyObj(out ho_Region1);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion3);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions4);
            HOperatorSet.GenEmptyObj(out ho_SortedRegions1);
            HOperatorSet.GenEmptyObj(out ho_UpperRegionLine);
            HOperatorSet.GenEmptyObj(out ho_BottomRegionLine);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions2);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion1);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_SortedRegions);
            HOperatorSet.GenEmptyObj(out ho_LeftRegionLine);
            HOperatorSet.GenEmptyObj(out ho_RightRegionLine);
            //-----產生邊界的XLD-----*
            ho_Contours.Dispose();
            HOperatorSet.GenContourRegionXld(ho_RegionBorder, out ho_Contours, "center");

            //-----將邊界分成4段XLD，並轉成Region-----*
            ho_ContoursSplit.Dispose();
            HOperatorSet.SegmentContoursXld(ho_Contours, out ho_ContoursSplit, "lines", 5,
                10, 8);
            ho_Region1.Dispose();
            HOperatorSet.GenRegionContourXld(ho_ContoursSplit, out ho_Region1, "margin");


            //-----篩出上、下兩條線-----*
            ho_SelectedRegions1.Dispose();
            HOperatorSet.SelectShape(ho_Region1, out ho_SelectedRegions1, "ratio", "and",
                0, 1);
            ho_RegionUnion3.Dispose();
            HOperatorSet.Union1(ho_SelectedRegions1, out ho_RegionUnion3);
            ho_ConnectedRegions4.Dispose();
            HOperatorSet.Connection(ho_RegionUnion3, out ho_ConnectedRegions4);
            ho_SortedRegions1.Dispose();
            HOperatorSet.SortRegion(ho_ConnectedRegions4, out ho_SortedRegions1, "upper_left",
                "true", "row");
            ho_UpperRegionLine.Dispose();
            HOperatorSet.SelectObj(ho_SortedRegions1, out ho_UpperRegionLine, 1);
            ho_BottomRegionLine.Dispose();
            HOperatorSet.SelectObj(ho_SortedRegions1, out ho_BottomRegionLine, 2);

            //-----篩出左、右兩條線-----*
            ho_SelectedRegions2.Dispose();
            HOperatorSet.SelectShape(ho_Region1, out ho_SelectedRegions2, "ratio", "and",
                10, 9999999);
            ho_RegionUnion1.Dispose();
            HOperatorSet.Union1(ho_SelectedRegions2, out ho_RegionUnion1);
            ho_ConnectedRegions1.Dispose();
            HOperatorSet.Connection(ho_RegionUnion1, out ho_ConnectedRegions1);
            ho_SortedRegions.Dispose();
            HOperatorSet.SortRegion(ho_ConnectedRegions1, out ho_SortedRegions, "upper_left",
                "true", "column");
            ho_LeftRegionLine.Dispose();
            HOperatorSet.SelectObj(ho_SortedRegions, out ho_LeftRegionLine, 1);
            ho_RightRegionLine.Dispose();
            HOperatorSet.SelectObj(ho_SortedRegions, out ho_RightRegionLine, 2);

            //-----將4邊線段合併-----*
            ho_OuterRegionLines.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_OuterRegionLines);
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_OuterRegionLines, ho_UpperRegionLine, out ExpTmpOutVar_0
                    );
                ho_OuterRegionLines.Dispose();
                ho_OuterRegionLines = ExpTmpOutVar_0;
            }
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_OuterRegionLines, ho_LeftRegionLine, out ExpTmpOutVar_0
                    );
                ho_OuterRegionLines.Dispose();
                ho_OuterRegionLines = ExpTmpOutVar_0;
            }
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_OuterRegionLines, ho_BottomRegionLine, out ExpTmpOutVar_0
                    );
                ho_OuterRegionLines.Dispose();
                ho_OuterRegionLines = ExpTmpOutVar_0;
            }
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_OuterRegionLines, ho_RightRegionLine, out ExpTmpOutVar_0
                    );
                ho_OuterRegionLines.Dispose();
                ho_OuterRegionLines = ExpTmpOutVar_0;
            }
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.RemoveObj(ho_OuterRegionLines, out ExpTmpOutVar_0, 1);
                ho_OuterRegionLines.Dispose();
                ho_OuterRegionLines = ExpTmpOutVar_0;
            }

            ho_Contours.Dispose();
            ho_ContoursSplit.Dispose();
            ho_Region1.Dispose();
            ho_SelectedRegions1.Dispose();
            ho_RegionUnion3.Dispose();
            ho_ConnectedRegions4.Dispose();
            ho_SortedRegions1.Dispose();
            ho_UpperRegionLine.Dispose();
            ho_BottomRegionLine.Dispose();
            ho_SelectedRegions2.Dispose();
            ho_RegionUnion1.Dispose();
            ho_ConnectedRegions1.Dispose();
            ho_SortedRegions.Dispose();
            ho_LeftRegionLine.Dispose();
            ho_RightRegionLine.Dispose();


        }

        private void ExtractRowAndColLines(HObject ho_RegionClipped, out HObject ho_SelectedRegions4,
      out HObject ho_SelectedRegions3, HTuple hv_GridHeight, HTuple hv_GridWidth,
      HTuple hv_ZoomFactor)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_ConnectedRegions2, ho_SelectedRegions;
            HObject ho_Skeleton, ho_EndPoints, ho_JuncPoints, ho_RegionDilation;
            HObject ho_RegionDifference, ho_ConnectedRegions, ho_RegionLines;
            HObject ho_SelectedRegions1, ho_RegionDilation3, ho_RegionDilation5;
            HObject ho_ConnectedRegions4, ho_ConnectedRegions3;

            // Local control variables 

            HTuple hv_Phi = new HTuple(), hv_Height4 = new HTuple();
            HTuple hv_Width4 = new HTuple(), hv_Ratio4 = new HTuple();
            HTuple hv_Height3 = new HTuple(), hv_Width3 = new HTuple();
            HTuple hv_Ratio3 = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions4);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions3);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions2);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_Skeleton);
            HOperatorSet.GenEmptyObj(out ho_EndPoints);
            HOperatorSet.GenEmptyObj(out ho_JuncPoints);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionLines);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation3);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation5);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions4);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions3);

            ho_ConnectedRegions2.Dispose();
            HOperatorSet.Connection(ho_RegionClipped, out ho_ConnectedRegions2);

            //-----濾掉較短線段-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions2, out ho_SelectedRegions, "max_diameter",
                    "and", hv_GridHeight / 2, 99999);
            }
            ho_Skeleton.Dispose();
            HOperatorSet.Skeleton(ho_SelectedRegions, out ho_Skeleton);

            //-----找出每個線段交點，並加大交點區域-----*
            ho_EndPoints.Dispose(); ho_JuncPoints.Dispose();
            HOperatorSet.JunctionsSkeleton(ho_Skeleton, out ho_EndPoints, out ho_JuncPoints
                );
            ho_RegionDilation.Dispose();
            HOperatorSet.DilationCircle(ho_JuncPoints, out ho_RegionDilation, 2);

            //-----找出交點區域外的線段區域-----*
            ho_RegionDifference.Dispose();
            HOperatorSet.Difference(ho_Skeleton, ho_RegionDilation, out ho_RegionDifference
                );
            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_RegionDifference, out ho_ConnectedRegions);

            //-----避免X與Y線段相連造成後續誤判，將線段拆成不同區域-----*
            ho_RegionLines.Dispose();
            HOperatorSet.SplitSkeletonRegion(ho_ConnectedRegions, out ho_RegionLines, 5);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions1.Dispose();
                HOperatorSet.SelectShape(ho_RegionLines, out ho_SelectedRegions1, "max_diameter",
                    "and", 12 * hv_ZoomFactor, 99999);
            }

            //-----找XY線段方式-----*
            hv_Phi.Dispose();
            HOperatorSet.OrientationRegion(ho_SelectedRegions1, out hv_Phi);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions3.Dispose();
                HOperatorSet.SelectShape(ho_SelectedRegions1, out ho_SelectedRegions3, (new HTuple("orientation")).TupleConcat(
                    "orientation"), "or", (new HTuple((new HTuple(80)).TupleRad())).TupleConcat(
                    (new HTuple(-100)).TupleRad()), (new HTuple((new HTuple(100)).TupleRad())).TupleConcat(
                    (new HTuple(-80)).TupleRad()));
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions4.Dispose();
                HOperatorSet.SelectShape(ho_SelectedRegions1, out ho_SelectedRegions4, ((new HTuple("orientation")).TupleConcat(
                    "orientation")).TupleConcat("orientation"), "or", (((new HTuple((new HTuple(-10)).TupleRad()
                    )).TupleConcat((new HTuple(170)).TupleRad()))).TupleConcat((new HTuple(-190)).TupleRad()
                    ), (((new HTuple((new HTuple(10)).TupleRad())).TupleConcat((new HTuple(190)).TupleRad()
                    ))).TupleConcat((new HTuple(-170)).TupleRad()));
            }

            //--去除線段短的部分--*
            //area_center (SelectedRegions4, AreaX, RowX, ColumnX)
            //area_center (SelectedRegions3, AreaY, RowY, ColumnY)
            //select_shape (SelectedRegions4, SelectedRegions4, 'area', 'and', mean(AreaX), max(AreaX))
            //select_shape (SelectedRegions3, SelectedRegions3, 'area', 'and', mean(AreaY), max(AreaY))
            //-----將Y方向線段做延申，將其連接在一起-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionDilation3.Dispose();
                HOperatorSet.DilationRectangle1(ho_SelectedRegions3, out ho_RegionDilation3,
                    2, hv_GridHeight * 5);
            }
            ho_SelectedRegions3.Dispose();
            HOperatorSet.Union1(ho_RegionDilation3, out ho_SelectedRegions3);

            //-----將X方向線段做延申，將其連接在一起-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionDilation5.Dispose();
                HOperatorSet.DilationRectangle1(ho_SelectedRegions4, out ho_RegionDilation5,
                    hv_GridWidth * 5, 2);
            }
            ho_SelectedRegions4.Dispose();
            HOperatorSet.Union1(ho_RegionDilation5, out ho_SelectedRegions4);
            //--去除線段短的部分--*
            ho_ConnectedRegions4.Dispose();
            HOperatorSet.Connection(ho_SelectedRegions4, out ho_ConnectedRegions4);
            ho_ConnectedRegions3.Dispose();
            HOperatorSet.Connection(ho_SelectedRegions3, out ho_ConnectedRegions3);
            hv_Height4.Dispose(); hv_Width4.Dispose(); hv_Ratio4.Dispose();
            HOperatorSet.HeightWidthRatio(ho_ConnectedRegions4, out hv_Height4, out hv_Width4,
                out hv_Ratio4);
            hv_Height3.Dispose(); hv_Width3.Dispose(); hv_Ratio3.Dispose();
            HOperatorSet.HeightWidthRatio(ho_ConnectedRegions3, out hv_Height3, out hv_Width3,
                out hv_Ratio3);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions4.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions4, out ho_SelectedRegions4, "width",
                    "and", 0.85 * (hv_Width4.TupleMean()), hv_Width4.TupleMax());
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions3.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions3, out ho_SelectedRegions3, "height",
                    "and", 0.85 * (hv_Height3.TupleMean()), hv_Height3.TupleMax());
            }
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.Union1(ho_SelectedRegions3, out ExpTmpOutVar_0);
                ho_SelectedRegions3.Dispose();
                ho_SelectedRegions3 = ExpTmpOutVar_0;
            }
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.Union1(ho_SelectedRegions4, out ExpTmpOutVar_0);
                ho_SelectedRegions4.Dispose();
                ho_SelectedRegions4 = ExpTmpOutVar_0;
            }


            ho_ConnectedRegions2.Dispose();
            ho_SelectedRegions.Dispose();
            ho_Skeleton.Dispose();
            ho_EndPoints.Dispose();
            ho_JuncPoints.Dispose();
            ho_RegionDilation.Dispose();
            ho_RegionDifference.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_RegionLines.Dispose();
            ho_SelectedRegions1.Dispose();
            ho_RegionDilation3.Dispose();
            ho_RegionDilation5.Dispose();
            ho_ConnectedRegions4.Dispose();
            ho_ConnectedRegions3.Dispose();

            hv_Phi.Dispose();
            hv_Height4.Dispose();
            hv_Width4.Dispose();
            hv_Ratio4.Dispose();
            hv_Height3.Dispose();
            hv_Width3.Dispose();
            hv_Ratio3.Dispose();

            return;
        }

    }
}

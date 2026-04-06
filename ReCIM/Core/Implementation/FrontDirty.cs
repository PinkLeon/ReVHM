using Core.Interface;
using Core.Model;
using HalconDotNet;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Implementation
{

    public class FrontDirty : FrontMeasurement
    {
        public MeasurementTable measurement { get; set; }

        //private CoreParameter coreParameter { get; set; }


        public async override Task<Result> Do(ICoreParameter coreParameter, AOICore aOICore)
        {
            // 先呼叫 FrontMeasurement.Do，確保 Image 被設定
            var baseResult = await base.Do(coreParameter, aOICore);
            Result result = new Result();
            result.ClassNumber = 3;
            result.ItemName = "FrontDirty";
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
            HObject ZoomRegion = new HObject();
            if (Image != null && Image.IsInitialized())
            {
                result.CurrentImage = Image;

                try
                {

                    //降影像解析度
                    HOperatorSet.ZoomImageFactor(Image, out ho_Image, Data.ZoomFactor, Data.ZoomFactor, "constant");

                    //檢測正面汙染
                    DetectFrontDirty(ho_Image, out DefectRegions, out HObject _DefectBorder, Data.ZoomFactor, Data.GridWidth, Data.GridHeight, out Values);

                    //根據公差做篩選
                    FilterResult(_DefectBorder, out ZoomRegion, data.tolerance_type, Values, data.tolerance, out DefectValues);

                    if (DefectValues.Length != 0 && (int)(new HTuple(((DefectValues.TupleSelect(0))).TupleNotEqual(999))) != 0)
                    {
                        //輸出最大與最小NG區域影像
                        FilterMinMaxResult(ho_Image, _DefectBorder, ZoomRegion, out HObject ho_MinDefectImage, out HObject ho_MaxDefectImage, out ZoomRegion, data.tolerance_type, DefectValues, data.tolerance);
                        FrontMinDefectImage = ho_MinDefectImage;
                        //if (FrontMinDefectImage.IsInitialized())
                        //    HOperatorSet.WriteImage(FrontMinDefectImage, "png", 0, @"C:\Users\User\Desktop\FrontMinDefectImage.png");
                        FrontMaxDefectImage = ho_MaxDefectImage;
                        //if (FrontMaxDefectImage.IsInitialized())
                        //    HOperatorSet.WriteImage(FrontMaxDefectImage, "png", 0, @"C:\Users\User\Desktop\FrontMaxDefectImage.png");


                    }
                    else
                    {
                        HOperatorSet.GenEmptyRegion(out _DefectBorder);
                        //HOperatorSet.GenEmptyObj(out HObject ho_MaxDefectImage);
                        //HOperatorSet.GenEmptyObj(out HObject ho_MinDefectImage);
                        HObject ho_MaxDefectImage = new HObject();
                        HObject ho_MinDefectImage = new HObject();
                        FrontMaxDefectImage = ho_MaxDefectImage;
                        FrontMinDefectImage = ho_MinDefectImage;
                    }
                }
                catch (Exception ex)
                {
                    await coreParameter.WriteLog("HALCON", "MissingLine Error", ex.Message + ex.StackTrace);
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
        /// 正面汙染判斷演算法
        /// </summary>
        /// <param name="ho_Image"></param>
        /// <param name="ho_DefectRegion"></param>
        /// <param name="ho_DefectBorder"></param>
        /// <param name="hv_DefectValues"></param>
        protected void DetectFrontDirty(HObject ho_Image, out HObject ho_DefectRegion, out HObject ho_DefectBorder,
      HTuple hv_ZoomFactor, HTuple hv_GridWidth, HTuple hv_GridHeight, out HTuple hv_DefectValues)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_LogImage, ho_ImageScaled, ho_ImageConverted;
            HObject ho_Region, ho_ConnectedRegions5, ho_RegionOpening1;
            HObject ho_RegionTrans, ho_RegionBorder, ho_ImageReduced;
            HObject ho_RegionBorderDilation, ho_ImageMean, ho_Region1;
            HObject ho_ImageReduced1, ho_Skeleton, ho_EndPoints, ho_JuncPoints;
            HObject ho_RegionDilation, ho_RegionDifference, ho_ConnectedRegions;
            HObject ho_RegionLines, ho_SelectedRegions1, ho_SelectedRegions3;
            HObject ho_SelectedRegions4, ho_RegionDilation3, ho_RegionUnionY;
            HObject ho_RegionDilation5, ho_RegionUnionX, ho_RegionUnionXY;
            HObject ho_RegionDilation8, ho_RegionUnion3, ho_RegionFillUp;
            HObject ho_RegionDifference1, ho_ConnectedRegions3, ho_RegionUnion4;
            HObject ho_ImageReduced2, ho_ImageMean1, ho_RegionDynThresh1;

            // Local control variables 

            HTuple hv_Min = new HTuple(), hv_Max = new HTuple();
            HTuple hv_Range = new HTuple(), hv_UsedThreshold = new HTuple();
            HTuple hv_Area3 = new HTuple(), hv_Row4 = new HTuple();
            HTuple hv_Column4 = new HTuple(), hv_Phi = new HTuple();
            HTuple hv_Row11 = new HTuple(), hv_Column11 = new HTuple();
            HTuple hv_Row2 = new HTuple(), hv_Column2 = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_DefectRegion);
            HOperatorSet.GenEmptyObj(out ho_DefectBorder);
            HOperatorSet.GenEmptyObj(out ho_LogImage);
            HOperatorSet.GenEmptyObj(out ho_ImageScaled);
            HOperatorSet.GenEmptyObj(out ho_ImageConverted);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions5);
            HOperatorSet.GenEmptyObj(out ho_RegionOpening1);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_RegionBorderDilation);
            HOperatorSet.GenEmptyObj(out ho_ImageMean);
            HOperatorSet.GenEmptyObj(out ho_Region1);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced1);
            HOperatorSet.GenEmptyObj(out ho_Skeleton);
            HOperatorSet.GenEmptyObj(out ho_EndPoints);
            HOperatorSet.GenEmptyObj(out ho_JuncPoints);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionLines);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions3);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions4);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation3);
            HOperatorSet.GenEmptyObj(out ho_RegionUnionY);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation5);
            HOperatorSet.GenEmptyObj(out ho_RegionUnionX);
            HOperatorSet.GenEmptyObj(out ho_RegionUnionXY);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation8);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion3);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference1);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions3);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion4);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced2);
            HOperatorSet.GenEmptyObj(out ho_ImageMean1);
            HOperatorSet.GenEmptyObj(out ho_RegionDynThresh1);
            hv_DefectValues = new HTuple();
            //=====找出待測物範圍=====*
            //----將待測物與背景對比拉高-----*
            ho_LogImage.Dispose();
            HOperatorSet.LogImage(ho_Image, out ho_LogImage, "e");
            //-----轉成U8影像-----*/
            hv_Min.Dispose(); hv_Max.Dispose(); hv_Range.Dispose();
            HOperatorSet.MinMaxGray(ho_LogImage, ho_LogImage, 0, out hv_Min, out hv_Max,
                out hv_Range);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_ImageScaled.Dispose();
                HOperatorSet.ScaleImage(ho_LogImage, out ho_ImageScaled, 255 / hv_Max, 0);
            }
            ho_ImageConverted.Dispose();
            HOperatorSet.ConvertImageType(ho_ImageScaled, out ho_ImageConverted, "byte");
            //-----先粗略找出待測物範圍-----*
            ho_Region.Dispose(); hv_UsedThreshold.Dispose();
            HOperatorSet.BinaryThreshold(ho_ImageConverted, out ho_Region, "smooth_histo",
                "light", out hv_UsedThreshold);
            //-----分離各區域-----*
            ho_ConnectedRegions5.Dispose();
            HOperatorSet.Connection(ho_Region, out ho_ConnectedRegions5);
            //-----去除與待測物邊緣粘在一起的雜點-----*
            ho_RegionOpening1.Dispose();
            HOperatorSet.OpeningRectangle1(ho_ConnectedRegions5, out ho_RegionOpening1, 100,
                100);
            //-----算出所以區域面積-----*
            hv_Area3.Dispose(); hv_Row4.Dispose(); hv_Column4.Dispose();
            HOperatorSet.AreaCenter(ho_RegionOpening1, out hv_Area3, out hv_Row4, out hv_Column4);
            //-----保留最大面積，即為待測物區域-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_Region.Dispose();
                HOperatorSet.SelectShape(ho_RegionOpening1, out ho_Region, "area", "and", (hv_Area3.TupleMax()
                    ) - 1, (hv_Area3.TupleMax()) + 1);
            }
            //-----找出待測物外框-----*
            ho_RegionTrans.Dispose();
            HOperatorSet.ShapeTrans(ho_Region, out ho_RegionTrans, "convex");
            ho_RegionBorder.Dispose();
            HOperatorSet.Boundary(ho_RegionTrans, out ho_RegionBorder, "inner");
            ho_ImageReduced.Dispose();
            HOperatorSet.ReduceDomain(ho_Image, ho_RegionTrans, out ho_ImageReduced);

            ho_RegionBorderDilation.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionBorder, out ho_RegionBorderDilation,
                15, 15);


            //=====裁出線段區域=====*
            //-----大略找出線段區域-----*
            ho_ImageMean.Dispose();
            HOperatorSet.MeanImage(ho_ImageReduced, out ho_ImageMean, 7, 7);
            ho_Region1.Dispose();
            HOperatorSet.DynThreshold(ho_ImageReduced, ho_ImageMean, out ho_Region1, 7, "dark");

            //-----從原圖裁只有出線段區域的影像-----*
            ho_ImageReduced1.Dispose();
            HOperatorSet.ReduceDomain(ho_ImageReduced, ho_Region1, out ho_ImageReduced1);

            ho_Skeleton.Dispose();
            HOperatorSet.Skeleton(ho_Region1, out ho_Skeleton);

            //-----  X C ӽu q   I A å[ j   I ϰ -----*
            ho_EndPoints.Dispose(); ho_JuncPoints.Dispose();
            HOperatorSet.JunctionsSkeleton(ho_Skeleton, out ho_EndPoints, out ho_JuncPoints
                );
            ho_RegionDilation.Dispose();
            HOperatorSet.DilationCircle(ho_JuncPoints, out ho_RegionDilation, 2);

            //-----  X   I ϰ ~   u q ϰ -----*
            ho_RegionDifference.Dispose();
            HOperatorSet.Difference(ho_Skeleton, ho_RegionDilation, out ho_RegionDifference
                );
            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_RegionDifference, out ho_ConnectedRegions);

            //----- קKX PY u q ۳s y      ~ P A N u q    P ϰ -----*
            ho_RegionLines.Dispose();
            HOperatorSet.SplitSkeletonRegion(ho_ConnectedRegions, out ho_RegionLines, 5);
            ho_SelectedRegions1.Dispose();
            HOperatorSet.SelectShape(ho_RegionLines, out ho_SelectedRegions1, "max_diameter",
                "and", 5, 99999);

            //-----  XY u q 覡-----*
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

            //----- NY  V u q     ӡA N  s   b @ _-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionDilation3.Dispose();
                HOperatorSet.DilationRectangle1(ho_SelectedRegions3, out ho_RegionDilation3,
                    5, hv_GridHeight * 5);
            }
            ho_RegionUnionY.Dispose();
            HOperatorSet.Union1(ho_RegionDilation3, out ho_RegionUnionY);

            //----- NX  V u q     ӡA N  s   b @ _-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RegionDilation5.Dispose();
                HOperatorSet.DilationRectangle1(ho_SelectedRegions4, out ho_RegionDilation5,
                    hv_GridWidth * 5, 5);
            }
            ho_RegionUnionX.Dispose();
            HOperatorSet.Union1(ho_RegionDilation5, out ho_RegionUnionX);

            //-----Merge XY線段-----*
            ho_RegionUnionXY.Dispose();
            HOperatorSet.Union2(ho_RegionUnionY, ho_RegionUnionX, out ho_RegionUnionXY);

            //-----找出所有格子區域-----*
            ho_RegionDilation8.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionBorder, out ho_RegionDilation8, 21,
                21);
            ho_RegionUnion3.Dispose();
            HOperatorSet.Union2(ho_RegionUnionXY, ho_RegionDilation8, out ho_RegionUnion3
                );
            ho_RegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_RegionUnion3, out ho_RegionFillUp);
            ho_RegionDifference1.Dispose();
            HOperatorSet.Difference(ho_RegionFillUp, ho_RegionUnion3, out ho_RegionDifference1
                );
            ho_ConnectedRegions3.Dispose();
            HOperatorSet.Connection(ho_RegionDifference1, out ho_ConnectedRegions3);
            ho_RegionUnion4.Dispose();
            HOperatorSet.Union1(ho_ConnectedRegions3, out ho_RegionUnion4);

            //-----從原使影像中將格子區域裁出-----*
            ho_ImageReduced2.Dispose();
            HOperatorSet.ReduceDomain(ho_ImageReduced, ho_RegionUnion4, out ho_ImageReduced2
                );

            //-----找出較黑區域、即為汙染或微裂-----*
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.Emphasize(ho_ImageReduced2, out ExpTmpOutVar_0, 7, 7, 3);
                ho_ImageReduced2.Dispose();
                ho_ImageReduced2 = ExpTmpOutVar_0;
            }
            ho_ImageMean1.Dispose();
            HOperatorSet.MeanImage(ho_ImageReduced2, out ho_ImageMean1, 21, 21);
            ho_RegionDynThresh1.Dispose();
            HOperatorSet.DynThreshold(ho_ImageReduced2, ho_ImageMean1, out ho_RegionDynThresh1,
                15, "dark");

            //-----分離汙染區域-----*
            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_RegionDynThresh1, out ho_ConnectedRegions);

            //-----篩選汙染長度大於1的區域-----*
            ho_DefectRegion.Dispose();
            HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_DefectRegion, "max_diameter",
                "and", 1, 99999);

            //-----將汙染區域轉成原圖比例-----*
            hv_Row11.Dispose(); hv_Column11.Dispose(); hv_Row2.Dispose(); hv_Column2.Dispose(); hv_DefectValues.Dispose();
            HOperatorSet.DiameterRegion(ho_DefectRegion, out hv_Row11, out hv_Column11, out hv_Row2,
                out hv_Column2, out hv_DefectValues);

            //-----標記直線度不良區域-----*
            ho_DefectBorder.Dispose();
            MarkDefectArea(ho_DefectRegion, out ho_DefectBorder, 5, 50);

            ho_LogImage.Dispose();
            ho_ImageScaled.Dispose();
            ho_ImageConverted.Dispose();
            ho_Region.Dispose();
            ho_ConnectedRegions5.Dispose();
            ho_RegionOpening1.Dispose();
            ho_RegionTrans.Dispose();
            ho_RegionBorder.Dispose();
            ho_ImageReduced.Dispose();
            ho_RegionBorderDilation.Dispose();
            ho_ImageMean.Dispose();
            ho_Region1.Dispose();
            ho_ImageReduced1.Dispose();
            ho_Skeleton.Dispose();
            ho_EndPoints.Dispose();
            ho_JuncPoints.Dispose();
            ho_RegionDilation.Dispose();
            ho_RegionDifference.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_RegionLines.Dispose();
            ho_SelectedRegions1.Dispose();
            ho_SelectedRegions3.Dispose();
            ho_SelectedRegions4.Dispose();
            ho_RegionDilation3.Dispose();
            ho_RegionUnionY.Dispose();
            ho_RegionDilation5.Dispose();
            ho_RegionUnionX.Dispose();
            ho_RegionUnionXY.Dispose();
            ho_RegionDilation8.Dispose();
            ho_RegionUnion3.Dispose();
            ho_RegionFillUp.Dispose();
            ho_RegionDifference1.Dispose();
            ho_ConnectedRegions3.Dispose();
            ho_RegionUnion4.Dispose();
            ho_ImageReduced2.Dispose();
            ho_ImageMean1.Dispose();
            ho_RegionDynThresh1.Dispose();

            hv_Min.Dispose();
            hv_Max.Dispose();
            hv_Range.Dispose();
            hv_UsedThreshold.Dispose();
            hv_Area3.Dispose();
            hv_Row4.Dispose();
            hv_Column4.Dispose();
            hv_Phi.Dispose();
            hv_Row11.Dispose();
            hv_Column11.Dispose();
            hv_Row2.Dispose();
            hv_Column2.Dispose();

            return;
        }

    }
}

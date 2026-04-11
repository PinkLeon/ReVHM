using Core.Interface;
using Core.Model;
using HalconDotNet;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Implementation
{

    public class BackDirty : BackMeasurement
    {


        public override async Task<Result> Do(ICoreParameter coreParameter, AOICore aOICore)
        {
            var baseResult = await base.Do(coreParameter, aOICore);
            Result result = new Result();
            result.ClassNumber = 2;
            result.ItemName = "BackDirty";
            result.CurrentImage = new HObject();
            AOICore Data = aOICore;
            var data = aOICore.TestSpectations.Where(n => n.item_name == result.ItemName).ToList().First();
            HObject DefectBorder;
            HTuple DefectValues;
            HTuple ClassNumber;
            if (Image != null && Image.IsInitialized())
            {
                result.CurrentImage = Image;
                //檢測背面汙染
                DetectBackDirty(Image, out HObject _DefectRegions, out HObject _DefectBorder, out HTuple Values);

                //根據公差做篩選
                FilterResult(_DefectBorder, out DefectBorder, data.tolerance_type, Values, data.tolerance, out DefectValues);

                if (DefectValues.Length != 0 && (int)(new HTuple(((DefectValues.TupleSelect(0))).TupleNotEqual(999))) != 0)
                {
                    //輸出最大與最小NG區域影像
                    FilterMinMaxResult(Image, _DefectBorder, DefectBorder, out HObject ho_MinDefectImage, out HObject ho_MaxDefectImage, out DefectBorder, data.tolerance_type, Values, data.tolerance);
                    //BackMinDefectImage = ho_MinDefectImage;
                    //if (BackMinDefectImage.IsInitialized())
                    //    HOperatorSet.WriteImage(BackMinDefectImage, "png", 0, @"C:\Users\user\Desktop\BackMinDefectImage.png");
                    //BackMaxDefectImage = ho_MaxDefectImage;
                    //if (BackMaxDefectImage.IsInitialized())
                    //    HOperatorSet.WriteImage(BackMaxDefectImage, "png", 0, @"C:\Users\user\Desktop\BackMaxDefectImage.png");

                }
                else
                {
                    HOperatorSet.GenEmptyRegion(out DefectBorder);
                    //HOperatorSet.GenEmptyObj(out HObject ho_MaxDefectImage);
                    //HOperatorSet.GenEmptyObj(out HObject ho_MinDefectImage);
                    HObject ho_MaxDefectImage = new HObject();
                    HObject ho_MinDefectImage = new HObject();
                    BackMaxDefectImage = ho_MaxDefectImage;
                    BackMinDefectImage = ho_MinDefectImage;
                }

                //將結果已result傳出或以context傳出難以抉擇?
                var task = await Task.Run(async () =>
                {
                    //不良區域
                    result.DefectRegions = _DefectRegions;
                    //不良數值
                    result.DefectValues = DefectValues;
                    return result;
                });

                return task;

            }

            return new Result();
        }

        /// <summary>
        /// 背面汙染判斷演算法
        /// </summary>
        /// <param name="ho_Image"></param>
        /// <param name="ho_BackDirtyRegion"></param>
        /// <param name="ho_BackDirtyBorder"></param>
        /// <param name="hv_BackDirtyValues"></param>
        private void DetectBackDirty(HObject ho_Image, out HObject ho_BackDirtyRegion,
      out HObject ho_BackDirtyBorder, out HTuple hv_BackDirtyValues)
        {



            // Local iconic variables 

            HObject ho_Region, ho_RegionFillUp, ho_ConnectedRegions1;
            HObject ho_SelectedRegions, ho_RegionClosing2, ho_ImageReduced;
            HObject ho_ImageMean, ho_Region1, ho_RegionClosing, ho_ConnectedRegions;
            HObject ho_RegionDilation1, ho_RegionBorder;

            // Local control variables 

            HTuple hv_UsedThreshold = new HTuple(), hv_Area = new HTuple();
            HTuple hv_Row = new HTuple(), hv_Column = new HTuple();
            HTuple hv_Row11 = new HTuple(), hv_Column11 = new HTuple();
            HTuple hv_Row2 = new HTuple(), hv_Column2 = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_BackDirtyRegion);
            HOperatorSet.GenEmptyObj(out ho_BackDirtyBorder);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionClosing2);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_ImageMean);
            HOperatorSet.GenEmptyObj(out ho_Region1);
            HOperatorSet.GenEmptyObj(out ho_RegionClosing);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation1);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder);
            hv_BackDirtyValues = new HTuple();
            //-----找出待測物範圍-----*
            ho_Region.Dispose(); hv_UsedThreshold.Dispose();
            HOperatorSet.BinaryThreshold(ho_Image, out ho_Region, "max_separability", "light",
                out hv_UsedThreshold);
            ho_RegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_Region, out ho_RegionFillUp);
            ho_ConnectedRegions1.Dispose();
            HOperatorSet.Connection(ho_RegionFillUp, out ho_ConnectedRegions1);
            hv_Area.Dispose(); hv_Row.Dispose(); hv_Column.Dispose();
            HOperatorSet.AreaCenter(ho_ConnectedRegions1, out hv_Area, out hv_Row, out hv_Column);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions1, out ho_SelectedRegions, "area",
                    "and", hv_Area.TupleMax(), (hv_Area.TupleMax()) + 1);
            }
            ho_RegionClosing2.Dispose();
            HOperatorSet.ErosionRectangle1(ho_SelectedRegions, out ho_RegionClosing2, 20,
                20);
            ho_ImageReduced.Dispose();
            HOperatorSet.ReduceDomain(ho_Image, ho_RegionClosing2, out ho_ImageReduced);

            //-----二值化找出暗色區域-----*
            ho_ImageMean.Dispose();
            HOperatorSet.MeanImage(ho_ImageReduced, out ho_ImageMean, 71, 71);
            ho_Region1.Dispose();
            HOperatorSet.DynThreshold(ho_ImageReduced, ho_ImageMean, out ho_Region1, 5, "dark");

            //-----去除黏在一起的雜點-----*
            ho_RegionClosing.Dispose();
            HOperatorSet.OpeningCircle(ho_Region1, out ho_RegionClosing, 2);

            //-----分離及篩選汙染區域-----*
            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_RegionClosing, out ho_ConnectedRegions);
            ho_BackDirtyRegion.Dispose();
            HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_BackDirtyRegion, "max_diameter",
                "and", 1, 9999999);

            //-----計算出各汙染尺寸-----*
            hv_Row11.Dispose(); hv_Column11.Dispose(); hv_Row2.Dispose(); hv_Column2.Dispose(); hv_BackDirtyValues.Dispose();
            HOperatorSet.DiameterRegion(ho_BackDirtyRegion, out hv_Row11, out hv_Column11,
                out hv_Row2, out hv_Column2, out hv_BackDirtyValues);


            //-----標記直線度不良區域-----*
            ho_BackDirtyBorder.Dispose();
            MarkDefectArea(ho_BackDirtyRegion, out ho_BackDirtyBorder, 20, 200);

            ho_Region.Dispose();
            ho_RegionFillUp.Dispose();
            ho_ConnectedRegions1.Dispose();
            ho_SelectedRegions.Dispose();
            ho_RegionClosing2.Dispose();
            ho_ImageReduced.Dispose();
            ho_ImageMean.Dispose();
            ho_Region1.Dispose();
            ho_RegionClosing.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_RegionDilation1.Dispose();
            ho_RegionBorder.Dispose();

            hv_UsedThreshold.Dispose();
            hv_Area.Dispose();
            hv_Row.Dispose();
            hv_Column.Dispose();
            hv_Row11.Dispose();
            hv_Column11.Dispose();
            hv_Row2.Dispose();
            hv_Column2.Dispose();

        }
    }
}

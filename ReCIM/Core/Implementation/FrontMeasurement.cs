using Core.Interface;
using Core.Model;
using HalconDotNet;
using System.Threading.Tasks;

namespace Core.Implementation
{
    /// <summary>
    /// 正面量測算法 直線性,歪斜,髒污,水平垂直邊距,找四邊,找正面區域,
    /// 共同:(找出並標示缺陷位置,以閥值篩選)
    /// 除了方法,要利用組合class 顯示量測值和顯示類別的屬性
    /// 先把大項目,拆成小項目,IMeasurement 應該改成抽象類別,可共用屬性,方法
    /// 正面先挑漏線,髒污來試試
    /// 多個檢測項目怎辦? 利用FrontEnum和BackEnum ? 用stringlist 方便閱讀
    /// </summary>
    /// 

    public class FrontMeasurement : Measurement
    {
        /// <summary>
        /// 正面量測應該要有自己ID,string ID = FrontSide,後續sort可拿來用
        /// </summary>

        //private AOICore _aOICore;

        public MeasurementTable measurement { get; set; }

        //private ICoreParameter coreParameter;


        //public FrontMeasurement(ICoreParameter coreParameter, AOICore aOICore) : base(coreParameter, aOICore)
        //{
        //    this.coreParameter = coreParameter;
        //    _aOICore = aOICore;
        //    Image = _aOICore?.FrontImage;
        //}

        public async override Task<Result> Do(ICoreParameter coreParameter, AOICore aOICore)
        {
            this.Image = aOICore.FrontImage;
            await Task.Delay(10);
            return new Result();
        }

        protected void CalculateGridPixel(HTuple hv_ModelWidth, HTuple hv_ModelHeight, HTuple hv_PixelSize,
      HTuple hv_ZoomFactor, out HTuple hv_GridWidthPixel, out HTuple hv_GridHeightPixel)
        {



            // Local iconic variables 
            // Initialize local and output iconic variables 
            //int hv_GridWidthPixel = 0;
            hv_GridWidthPixel = new HTuple();
            hv_GridHeightPixel = new HTuple();
            hv_GridWidthPixel.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_GridWidthPixel = ((((hv_ModelWidth * 1000) / hv_PixelSize) * hv_ZoomFactor)).TupleInt();
            }
            hv_GridHeightPixel.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_GridHeightPixel = ((((hv_ModelHeight * 1000) / hv_PixelSize) * hv_ZoomFactor)).TupleInt();
            }



            return;
        }

        /// <summary>
        /// 找出正面影像的四個邊界
        /// </summary>
        /// <param name="ho_RegionLines"></param>
        /// <param name="ho_AroundRegionLines"></param>
        protected void FindAroundLines(HObject ho_RegionLines, out HObject ho_AroundRegionLines)
        {



            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_SelectedRegionLines, ho_UBRegionLines;
            HObject ho_LRRegionLines, ho_ObjectSelected = null, ho_Contours = null;
            HObject ho_ConnectedUBRegionLines, ho_SortedUBRegionLines;
            HObject ho_ConnectedLRRegionLines, ho_SortedLRRegionLines;
            HObject ho_UpperRegionLine, ho_BottomRegionLine, ho_LeftRegionLine;
            HObject ho_RightRegionLine;

            // Local control variables 

            HTuple hv_Number = new HTuple(), hv_Index = new HTuple();
            HTuple hv_Area = new HTuple(), hv_Row = new HTuple(), hv_Column = new HTuple();
            HTuple hv_RowBegin = new HTuple(), hv_ColBegin = new HTuple();
            HTuple hv_RowEnd = new HTuple(), hv_ColEnd = new HTuple();
            HTuple hv_Nr = new HTuple(), hv_Nc = new HTuple(), hv_Dist = new HTuple();
            HTuple hv_Angle = new HTuple(), hv_LineAngle = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_AroundRegionLines);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegionLines);
            HOperatorSet.GenEmptyObj(out ho_UBRegionLines);
            HOperatorSet.GenEmptyObj(out ho_LRRegionLines);
            HOperatorSet.GenEmptyObj(out ho_ObjectSelected);
            HOperatorSet.GenEmptyObj(out ho_Contours);
            HOperatorSet.GenEmptyObj(out ho_ConnectedUBRegionLines);
            HOperatorSet.GenEmptyObj(out ho_SortedUBRegionLines);
            HOperatorSet.GenEmptyObj(out ho_ConnectedLRRegionLines);
            HOperatorSet.GenEmptyObj(out ho_SortedLRRegionLines);
            HOperatorSet.GenEmptyObj(out ho_UpperRegionLine);
            HOperatorSet.GenEmptyObj(out ho_BottomRegionLine);
            HOperatorSet.GenEmptyObj(out ho_LeftRegionLine);
            HOperatorSet.GenEmptyObj(out ho_RightRegionLine);
            //=====篩出上下左右邊線=====*
            ho_SelectedRegionLines.Dispose();
            HOperatorSet.SelectShape(ho_RegionLines, out ho_SelectedRegionLines, "contlength",
                "and", 100, "max");
            hv_Number.Dispose();
            HOperatorSet.CountObj(ho_SelectedRegionLines, out hv_Number);
            ho_UBRegionLines.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_UBRegionLines);
            ho_LRRegionLines.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_LRRegionLines);
            HTuple end_val5 = hv_Number;
            HTuple step_val5 = 1;
            for (hv_Index = 1; hv_Index.Continue(end_val5, step_val5); hv_Index = hv_Index.TupleAdd(step_val5))
            {
                ho_ObjectSelected.Dispose();
                HOperatorSet.SelectObj(ho_SelectedRegionLines, out ho_ObjectSelected, hv_Index);
                hv_Area.Dispose(); hv_Row.Dispose(); hv_Column.Dispose();
                HOperatorSet.AreaCenter(ho_ObjectSelected, out hv_Area, out hv_Row, out hv_Column);
                ho_Contours.Dispose();
                HOperatorSet.GenContoursSkeletonXld(ho_ObjectSelected, out ho_Contours, 1,
                    "filter");
                hv_RowBegin.Dispose(); hv_ColBegin.Dispose(); hv_RowEnd.Dispose(); hv_ColEnd.Dispose(); hv_Nr.Dispose(); hv_Nc.Dispose(); hv_Dist.Dispose();
                HOperatorSet.FitLineContourXld(ho_Contours, "tukey", -1, 0, 5, 2, out hv_RowBegin,
                    out hv_ColBegin, out hv_RowEnd, out hv_ColEnd, out hv_Nr, out hv_Nc, out hv_Dist);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Angle.Dispose();
                    HOperatorSet.AngleLl(hv_Row - 50, hv_Column, hv_Row + 50, hv_Column, hv_RowBegin,
                        hv_ColBegin, hv_RowEnd, hv_ColEnd, out hv_Angle);
                }
                hv_LineAngle.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_LineAngle = ((hv_Angle.TupleDeg()
                        )).TupleAbs();
                }

                if ((int)((new HTuple(hv_LineAngle.TupleGreater(80))).TupleAnd(new HTuple(hv_LineAngle.TupleLess(
                    100)))) != 0)
                {
                    {
                        HObject ExpTmpOutVar_0;
                        HOperatorSet.Union2(ho_UBRegionLines, ho_ObjectSelected, out ExpTmpOutVar_0
                            );
                        ho_UBRegionLines.Dispose();
                        ho_UBRegionLines = ExpTmpOutVar_0;
                    }
                }
                else if ((int)((new HTuple((new HTuple(hv_LineAngle.TupleGreaterEqual(
                    0))).TupleAnd(new HTuple(hv_LineAngle.TupleLess(10))))).TupleOr(new HTuple(hv_LineAngle.TupleGreater(
                    170)))) != 0)
                {
                    {
                        HObject ExpTmpOutVar_0;
                        HOperatorSet.Union2(ho_LRRegionLines, ho_ObjectSelected, out ExpTmpOutVar_0
                            );
                        ho_LRRegionLines.Dispose();
                        ho_LRRegionLines = ExpTmpOutVar_0;
                    }
                }
            }

            //-----篩出上下邊線-----*
            ho_ConnectedUBRegionLines.Dispose();
            HOperatorSet.Connection(ho_UBRegionLines, out ho_ConnectedUBRegionLines);
            ho_SortedUBRegionLines.Dispose();
            HOperatorSet.SortRegion(ho_ConnectedUBRegionLines, out ho_SortedUBRegionLines,
                "character", "true", "row");

            //-----篩出左右邊線-----*
            ho_ConnectedLRRegionLines.Dispose();
            HOperatorSet.Connection(ho_LRRegionLines, out ho_ConnectedLRRegionLines);
            ho_SortedLRRegionLines.Dispose();
            HOperatorSet.SortRegion(ho_ConnectedLRRegionLines, out ho_SortedLRRegionLines,
                "character", "true", "column");

            //-----上下左右邊線-----*
            ho_UpperRegionLine.Dispose();
            HOperatorSet.SelectObj(ho_SortedUBRegionLines, out ho_UpperRegionLine, 1);
            ho_BottomRegionLine.Dispose();
            HOperatorSet.SelectObj(ho_SortedUBRegionLines, out ho_BottomRegionLine, 2);
            ho_LeftRegionLine.Dispose();
            HOperatorSet.SelectObj(ho_SortedLRRegionLines, out ho_LeftRegionLine, 1);
            ho_RightRegionLine.Dispose();
            HOperatorSet.SelectObj(ho_SortedLRRegionLines, out ho_RightRegionLine, 2);

            //----將上下左右邊線依序串一起，方便迴圈使用-----*
            ho_AroundRegionLines.Dispose();
            HOperatorSet.ConcatObj(ho_UpperRegionLine, ho_BottomRegionLine, out ho_AroundRegionLines
                );
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_AroundRegionLines, ho_LeftRegionLine, out ExpTmpOutVar_0
                    );
                ho_AroundRegionLines.Dispose();
                ho_AroundRegionLines = ExpTmpOutVar_0;
            }
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_AroundRegionLines, ho_RightRegionLine, out ExpTmpOutVar_0
                    );
                ho_AroundRegionLines.Dispose();
                ho_AroundRegionLines = ExpTmpOutVar_0;
            }

            ho_SelectedRegionLines.Dispose();
            ho_UBRegionLines.Dispose();
            ho_LRRegionLines.Dispose();
            ho_ObjectSelected.Dispose();
            ho_Contours.Dispose();
            ho_ConnectedUBRegionLines.Dispose();
            ho_SortedUBRegionLines.Dispose();
            ho_ConnectedLRRegionLines.Dispose();
            ho_SortedLRRegionLines.Dispose();
            ho_UpperRegionLine.Dispose();
            ho_BottomRegionLine.Dispose();
            ho_LeftRegionLine.Dispose();
            ho_RightRegionLine.Dispose();

            hv_Number.Dispose();
            hv_Index.Dispose();
            hv_Area.Dispose();
            hv_Row.Dispose();
            hv_Column.Dispose();
            hv_RowBegin.Dispose();
            hv_ColBegin.Dispose();
            hv_RowEnd.Dispose();
            hv_ColEnd.Dispose();
            hv_Nr.Dispose();
            hv_Nc.Dispose();
            hv_Dist.Dispose();
            hv_Angle.Dispose();
            hv_LineAngle.Dispose();

        }

        /// <summary>
        /// 粗略的找出正面區域
        /// </summary>
        /// <param name="ho_Image"></param>
        /// <param name="ho_FindRegion"></param>
        /// <param name="ho_FindRegionTrans"></param>
        /// <param name="ho_FindBorder"></param>
        protected void FindFrontRegion(HObject ho_Image, out HObject ho_FindRegion, out HObject ho_FindRegionTrans,
      out HObject ho_FindBorder)
        {



            // Local iconic variables 

            HObject ho_ImageMean, ho_Region, ho_ConnectedRegions;

            // Local control variables 

            HTuple hv_UsedThreshold = new HTuple(), hv_Area = new HTuple();
            HTuple hv_Row = new HTuple(), hv_Column = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_FindRegion);
            HOperatorSet.GenEmptyObj(out ho_FindRegionTrans);
            HOperatorSet.GenEmptyObj(out ho_FindBorder);
            HOperatorSet.GenEmptyObj(out ho_ImageMean);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            try
            {
                //=====找出待測物範圍=====*
                //----將影像模糊化再取二值化-----*
                ho_ImageMean.Dispose();
                HOperatorSet.MeanImage(ho_Image, out ho_ImageMean, 15, 15);
                ho_Region.Dispose(); hv_UsedThreshold.Dispose();
                HOperatorSet.BinaryThreshold(ho_ImageMean, out ho_Region, "smooth_histo", "light",
                    out hv_UsedThreshold);
                //-----分離各區域-----*
                ho_ConnectedRegions.Dispose();
                HOperatorSet.Connection(ho_Region, out ho_ConnectedRegions);
                //-----算出區域面積-----*
                hv_Area.Dispose(); hv_Row.Dispose(); hv_Column.Dispose();
                HOperatorSet.AreaCenter(ho_ConnectedRegions, out hv_Area, out hv_Row, out hv_Column);
                //-----保留最大面積，即為待測物區域-----*
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_FindRegion.Dispose();
                    HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_FindRegion, "area", "and",
                        (hv_Area.TupleMax()) - 1, hv_Area.TupleMax());
                }
                //-----保留待測物外框-----*
                ho_FindRegionTrans.Dispose();
                HOperatorSet.ShapeTrans(ho_FindRegion, out ho_FindRegionTrans, "convex");
                ho_FindBorder.Dispose();
                HOperatorSet.Boundary(ho_FindRegionTrans, out ho_FindBorder, "inner");

                ho_ImageMean.Dispose();
                ho_Region.Dispose();
                ho_ConnectedRegions.Dispose();
                hv_UsedThreshold.Dispose();
                hv_Area.Dispose();
                hv_Row.Dispose();
                hv_Column.Dispose();

                return;
            }
            catch (HalconException)
            {
                ho_ImageMean.Dispose();
                ho_Region.Dispose();
                ho_ConnectedRegions.Dispose();
                hv_UsedThreshold.Dispose();
                hv_Area.Dispose();
                hv_Row.Dispose();
                hv_Column.Dispose();

            }
        }
    }
}

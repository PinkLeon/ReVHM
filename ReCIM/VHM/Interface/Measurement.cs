using HalconDotNet;
using System.Threading.Tasks;
using VHM.Model;

namespace VHM.Interface
{
    /// <summary>
    /// 量測class為抽象類別,可共用屬性,方法
    /// 1.找到正面還是背面,看圖的尺寸
    /// 2.分離前景和背景
    /// 3.找出四邊
    /// 4.量測
    /// 5.找出瑕疵區域
    /// 6.要有各量測值?或正背面分開,應該要憶起
    /// </summary>
    public abstract class Measurement
    {
        /// <summary>
        /// 量測的應該是有瑕疵的部分(量化)
        /// </summary>
        InspectionCore inspectionCore;

        public HObject DefectRegionImage { get; set; }

        public HTuple DefectValue { get; set; }

        public MeasurementTable measurement { get; set; }

        public HObject FrontMaxDefectImage { get; protected set; } = new HObject();

        /// <summary>
        /// 最小缺陷區域影像
        /// </summary>
        public HObject FrontMinDefectImage { get; protected set; } = new HObject();

        /// <summary>
        /// 最大缺陷區域影像
        /// </summary>
        public HObject BackMaxDefectImage { get; protected set; } = new HObject();

        /// <summary>
        /// 最小缺陷區域影像
        /// </summary>
        public HObject BackMinDefectImage { get; protected set; } = new HObject();


        public int classNumber { get; set; }

        public Measurement()
        {

        }

        public abstract Task<Result> Do(HObject image, AOICore aOICore);


        ////1.找到正面還是背面,看圖的尺寸
        //public void GetHObject(HObject hObject)
        //{
        //    Image = hObject;
        //}
        //2.分離前景和背景

        //3.找出四邊

        //4.量測

        //5.找出瑕疵區域

        /// <summary>
        /// 根據公差篩選演算法的結果
        /// </summary>
        /// <param name="ho_RegionBorder"></param>
        /// <param name="ho_FinalBorder"></param>
        /// <param name="hv_ToleranceType"></param>
        /// <param name="hv_TestResultTuple"></param>
        /// <param name="hv_Tolerance"></param>
        /// <param name="hv_FinalResult"></param>
        protected void FilterResult(HObject ho_RegionBorder, out HObject ho_FinalBorder,
      HTuple hv_ToleranceType, HTuple hv_TestResultTuple, HTuple hv_Tolerance, out HTuple hv_FinalResult)
        {
            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_RemoveBorder;

            // Local control variables 

            HTuple hv_RemoveResultIndex = new HTuple();
            HTuple hv_RemoveBorderIndex = new HTuple(), hv_TupleResult = new HTuple();
            HTuple hv_Index = new HTuple(), hv_RemoveResult = new HTuple();
            HTuple hv_Length = new HTuple(), hv_MaxIndex = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_FinalBorder);
            HOperatorSet.GenEmptyObj(out ho_RemoveBorder);
            hv_FinalResult = new HTuple();
            //-----變數建立-----*
            hv_RemoveResultIndex.Dispose();
            hv_RemoveResultIndex = new HTuple();
            hv_RemoveBorderIndex.Dispose();
            hv_RemoveBorderIndex = new HTuple();
            hv_TupleResult.Dispose();
            hv_TupleResult = new HTuple();

            switch (hv_ToleranceType.I)
            {
                //-----比較出大於等於的結果-----*
                case 1:
                    hv_TupleResult.Dispose();
                    HOperatorSet.TupleGreaterEqualElem(hv_TestResultTuple, hv_Tolerance, out hv_TupleResult);
                    break;

                //-----比較出小於等於的結果-----*
                case 2:
                    hv_TupleResult.Dispose();
                    HOperatorSet.TupleLessEqualElem(hv_TestResultTuple, hv_Tolerance, out hv_TupleResult);
                    break;

                //-----目前無其它Type-----*
                default:
                    break;

            }

            //-----篩出要移除Region的Index,是1就移除-----*
            for (hv_Index = 1; (int)hv_Index <= (int)(new HTuple(hv_TupleResult.TupleLength()
                )); hv_Index = (int)hv_Index + 1)
            {
                if ((int)(new HTuple(((hv_TupleResult.TupleSelect(hv_Index - 1))).TupleEqual(
                    1))) != 0)
                {
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_RemoveResultIndex = hv_RemoveResultIndex.TupleConcat(
                                hv_Index - 1);
                            hv_RemoveResultIndex.Dispose();
                            hv_RemoveResultIndex = ExpTmpLocalVar_RemoveResultIndex;
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_RemoveBorderIndex = hv_RemoveBorderIndex.TupleConcat(
                                hv_Index);
                            hv_RemoveBorderIndex.Dispose();
                            hv_RemoveBorderIndex = ExpTmpLocalVar_RemoveBorderIndex;
                        }
                    }
                }
            }

            //-----移除不符合公差範圍的Index數據及區域-----*
            hv_RemoveResult.Dispose();
            HOperatorSet.TupleRemove(hv_TestResultTuple, hv_RemoveResultIndex, out hv_RemoveResult);
            ho_RemoveBorder.Dispose();
            HOperatorSet.RemoveObj(ho_RegionBorder, out ho_RemoveBorder, hv_RemoveBorderIndex);

            //-----找出最大的瑕疵數值-----*
            hv_FinalResult.Dispose();
            //HOperatorSet.TupleSum(hv_RemoveResult, out hv_FinalResult);
            if ((int)(new HTuple(hv_TestResultTuple.TupleNotEqual(new HTuple()))) != 0)
            {
                hv_FinalResult.Dispose();
                HOperatorSet.TupleMax(hv_TestResultTuple, out hv_FinalResult);
            }
            else
            {
                hv_FinalResult.Dispose();
                hv_FinalResult = 0;
            }
            //union1 (RemoveBorder, FinalBorder)

            //--移除最大瑕疵的區域--*
            hv_Length.Dispose();
            HOperatorSet.TupleLength(hv_RemoveResult, out hv_Length);
            if ((int)(new HTuple(hv_Length.TupleGreater(1))) != 0)
            {
                //-----找出最大數值對應的區域-----*
                hv_FinalResult.Dispose();
                HOperatorSet.TupleMax(hv_RemoveResult, out hv_FinalResult);
                hv_MaxIndex.Dispose();
                HOperatorSet.TupleFindFirst(hv_RemoveResult, hv_FinalResult, out hv_MaxIndex);
                //-----移除最大瑕疵的區域-----*
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_FinalBorder.Dispose();
                    HOperatorSet.RemoveObj(ho_RemoveBorder, out ho_FinalBorder, hv_MaxIndex + 1);
                }
            }
            else
            {
                ho_FinalBorder.Dispose();
                HOperatorSet.GenEmptyRegion(out ho_FinalBorder);
                //FinalResult := 0
            }


            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.Union1(ho_FinalBorder, out ExpTmpOutVar_0);
                ho_FinalBorder.Dispose();
                ho_FinalBorder = ExpTmpOutVar_0;
            }



            ho_RemoveBorder.Dispose();

            hv_RemoveResultIndex.Dispose();
            hv_RemoveBorderIndex.Dispose();
            hv_TupleResult.Dispose();
            hv_Index.Dispose();
            hv_RemoveResult.Dispose();
            hv_Length.Dispose();
            hv_MaxIndex.Dispose();

            return;
        }

        /// <summary>
        /// 計算Line Gauss要用的參數(Sigma, Low, High)
        /// </summary>
        /// <param name="hv_MaxLineWidth"></param>
        /// <param name="hv_Contrast"></param>
        /// <param name="hv_Sigma"></param>
        /// <param name="hv_Low"></param>
        /// <param name="hv_High"></param>
        protected void calculate_lines_gauss_parameters(HTuple hv_MaxLineWidth, HTuple hv_Contrast,
      out HTuple hv_Sigma, out HTuple hv_Low, out HTuple hv_High)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_ContrastHigh = new HTuple(), hv_ContrastLow = new HTuple();
            HTuple hv_HalfWidth = new HTuple(), hv_Help = new HTuple();
            HTuple hv_MaxLineWidth_COPY_INP_TMP = new HTuple(hv_MaxLineWidth);

            // Initialize local and output iconic variables 
            hv_Sigma = new HTuple();
            hv_Low = new HTuple();
            hv_High = new HTuple();
            try
            {
                //Check control parameters
                if ((int)(new HTuple((new HTuple(hv_MaxLineWidth_COPY_INP_TMP.TupleLength()
                    )).TupleNotEqual(1))) != 0)
                {
                    throw new HalconException("Wrong number of values of control parameter: 1");
                }
                if ((int)(((hv_MaxLineWidth_COPY_INP_TMP.TupleIsNumber())).TupleNot()) != 0)
                {
                    throw new HalconException("Wrong type of control parameter: 1");
                }
                if ((int)(new HTuple(hv_MaxLineWidth_COPY_INP_TMP.TupleLessEqual(0))) != 0)
                {
                    throw new HalconException("Wrong value of control parameter: 1");
                }
                if ((int)((new HTuple((new HTuple(hv_Contrast.TupleLength())).TupleNotEqual(
                    1))).TupleAnd(new HTuple((new HTuple(hv_Contrast.TupleLength())).TupleNotEqual(
                    2)))) != 0)
                {
                    throw new HalconException("Wrong number of values of control parameter: 2");
                }
                if ((int)(new HTuple(((((hv_Contrast.TupleIsNumber())).TupleMin())).TupleEqual(
                    0))) != 0)
                {
                    throw new HalconException("Wrong type of control parameter: 2");
                }
                //Set and check ContrastHigh
                hv_ContrastHigh.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_ContrastHigh = hv_Contrast.TupleSelect(
                        0);
                }
                if ((int)(new HTuple(hv_ContrastHigh.TupleLess(0))) != 0)
                {
                    throw new HalconException("Wrong value of control parameter: 2");
                }
                //Set or derive ContrastLow
                if ((int)(new HTuple((new HTuple(hv_Contrast.TupleLength())).TupleEqual(2))) != 0)
                {
                    hv_ContrastLow.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_ContrastLow = hv_Contrast.TupleSelect(
                            1);
                    }
                }
                else
                {
                    hv_ContrastLow.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_ContrastLow = hv_ContrastHigh / 3.0;
                    }
                }
                //Check ContrastLow
                if ((int)(new HTuple(hv_ContrastLow.TupleLess(0))) != 0)
                {
                    throw new HalconException("Wrong value of control parameter: 2");
                }
                if ((int)(new HTuple(hv_ContrastLow.TupleGreater(hv_ContrastHigh))) != 0)
                {
                    throw new HalconException("Wrong value of control parameter: 2");
                }
                //
                //Calculate the parameters Sigma, Low, and High for lines_gauss
                if ((int)(new HTuple(hv_MaxLineWidth_COPY_INP_TMP.TupleLess((new HTuple(3.0)).TupleSqrt()
                    ))) != 0)
                {
                    //Note that LineWidthMax < sqrt(3.0) would result in a Sigma < 0.5,
                    //which does not make any sense, because the corresponding smoothing
                    //filter mask would be of size 1x1.
                    //To avoid this, LineWidthMax is restricted to values greater or equal
                    //to sqrt(3.0) and the contrast values are adapted to reflect the fact
                    //that lines that are thinner than sqrt(3.0) pixels have a lower contrast
                    //in the smoothed image (compared to lines that are sqrt(3.0) pixels wide).
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_ContrastLow = (hv_ContrastLow * hv_MaxLineWidth_COPY_INP_TMP) / ((new HTuple(3.0)).TupleSqrt()
                                );
                            hv_ContrastLow.Dispose();
                            hv_ContrastLow = ExpTmpLocalVar_ContrastLow;
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_ContrastHigh = (hv_ContrastHigh * hv_MaxLineWidth_COPY_INP_TMP) / ((new HTuple(3.0)).TupleSqrt()
                                );
                            hv_ContrastHigh.Dispose();
                            hv_ContrastHigh = ExpTmpLocalVar_ContrastHigh;
                        }
                    }
                    hv_MaxLineWidth_COPY_INP_TMP.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_MaxLineWidth_COPY_INP_TMP = (new HTuple(3.0)).TupleSqrt()
                            ;
                    }
                }
                //Convert LineWidthMax and the given contrast values into the input parameters
                //Sigma, Low, and High required by lines_gauss
                hv_HalfWidth.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_HalfWidth = hv_MaxLineWidth_COPY_INP_TMP / 2.0;
                }
                hv_Sigma.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Sigma = hv_HalfWidth / ((new HTuple(3.0)).TupleSqrt()
                        );
                }
                hv_Help.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Help = ((-2.0 * hv_HalfWidth) / (((new HTuple(6.283185307178)).TupleSqrt()
                        ) * (hv_Sigma.TuplePow(3.0)))) * (((-0.5 * (((hv_HalfWidth / hv_Sigma)).TuplePow(
                        2.0)))).TupleExp());
                }
                hv_High.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_High = ((hv_ContrastHigh * hv_Help)).TupleFabs()
                        ;
                }
                hv_Low.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Low = ((hv_ContrastLow * hv_Help)).TupleFabs()
                        ;
                }

                hv_MaxLineWidth_COPY_INP_TMP.Dispose();
                hv_ContrastHigh.Dispose();
                hv_ContrastLow.Dispose();
                hv_HalfWidth.Dispose();
                hv_Help.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {

                hv_MaxLineWidth_COPY_INP_TMP.Dispose();
                hv_ContrastHigh.Dispose();
                hv_ContrastLow.Dispose();
                hv_HalfWidth.Dispose();
                hv_Help.Dispose();

            }
        }

        /// <summary>
        /// 將缺陷區域做矩形外框標記
        /// </summary>
        /// <param name="ho_Regions"></param>
        /// <param name="ho_MarkBorder"></param>
        /// <param name="hv_BorderWidth"></param>
        protected void MarkDefectArea(HObject ho_Regions, out HObject ho_MarkBorder, HTuple hv_BorderWidth,
      HTuple hv_MarkRange)
        {




            // Local iconic variables 

            HObject ho_RegionTrans, ho_RegionDilation;
            HObject ho_RegionBorder;
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_MarkBorder);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder);
            //-----將Region轉成矩形-----*
            ho_RegionTrans.Dispose();
            HOperatorSet.ShapeTrans(ho_Regions, out ho_RegionTrans, "rectangle2");

            //-----增加矩形尺寸-----*
            ho_RegionDilation.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionTrans, out ho_RegionDilation, hv_MarkRange,
                hv_MarkRange);

            //-----取得矩形外框-----*
            ho_RegionBorder.Dispose();
            HOperatorSet.Boundary(ho_RegionDilation, out ho_RegionBorder, "outer");

            //-----根據設定值加粗外框-----*
            ho_MarkBorder.Dispose();
            HOperatorSet.DilationRectangle1(ho_RegionBorder, out ho_MarkBorder, hv_BorderWidth,
                hv_BorderWidth);

            ho_RegionTrans.Dispose();
            ho_RegionDilation.Dispose();
            ho_RegionBorder.Dispose();


        }

        /// <summary>
        /// 根據數值最大區域，裁出對應的影像並輸出
        /// </summary>
        /// <param name="ho_Image"></param>
        /// <param name="ho_RegionBorder"></param>
        /// <param name="ho_FinalMaxImage"></param>
        /// <param name="hv_ToleranceType"></param>
        /// <param name="hv_TestResultTuple"></param>
        /// <param name="hv_Tolerance"></param>
        protected void FilterMaxResult(HObject ho_Image, HObject ho_RegionBorder, out HObject ho_FinalMaxImage,
      HTuple hv_ToleranceType, HTuple hv_TestResultTuple, HTuple hv_Tolerance)
        {




            // Local iconic variables 

            HObject ho_SelectedMaxBorder, ho_MaxRegionFillUp;
            HObject ho_ImageReduced;

            // Local control variables 

            HTuple hv_RemoveResultIndex = new HTuple();
            HTuple hv_RemoveBorderIndex = new HTuple(), hv_TupleResult = new HTuple();
            HTuple hv_Index = new HTuple(), hv_RemoveResult = new HTuple();
            HTuple hv_Max = new HTuple(), hv_MaxIndex = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_FinalMaxImage);
            HOperatorSet.GenEmptyObj(out ho_SelectedMaxBorder);
            HOperatorSet.GenEmptyObj(out ho_MaxRegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            //-----變數建立-----*
            hv_RemoveResultIndex.Dispose();
            hv_RemoveResultIndex = new HTuple();
            hv_RemoveBorderIndex.Dispose();
            hv_RemoveBorderIndex = new HTuple();
            hv_TupleResult.Dispose();
            hv_TupleResult = new HTuple();

            switch (hv_ToleranceType.I)
            {
                //-----比較出大於等於的結果-----*
                case 1:
                    hv_TupleResult.Dispose();
                    HOperatorSet.TupleGreaterEqualElem(hv_TestResultTuple, hv_Tolerance, out hv_TupleResult);
                    break;

                //-----比較出小於等於的結果-----*
                case 2:
                    hv_TupleResult.Dispose();
                    HOperatorSet.TupleLessEqualElem(hv_TestResultTuple, hv_Tolerance, out hv_TupleResult);
                    break;

                //-----目前無其它Type-----*
                default:
                    break;

            }

            //-----篩出要移除Region的Index-----*
            for (hv_Index = 1; (int)hv_Index <= (int)(new HTuple(hv_TupleResult.TupleLength()
                )); hv_Index = (int)hv_Index + 1)
            {
                if ((int)(new HTuple(((hv_TupleResult.TupleSelect(hv_Index - 1))).TupleEqual(
                    1))) != 0)
                {
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_RemoveResultIndex = hv_RemoveResultIndex.TupleConcat(
                                hv_Index - 1);
                            hv_RemoveResultIndex.Dispose();
                            hv_RemoveResultIndex = ExpTmpLocalVar_RemoveResultIndex;
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_RemoveBorderIndex = hv_RemoveBorderIndex.TupleConcat(
                                hv_Index);
                            hv_RemoveBorderIndex.Dispose();
                            hv_RemoveBorderIndex = ExpTmpLocalVar_RemoveBorderIndex;
                        }
                    }
                }
            }

            //-----移除對應的Index數據及區域-----*
            hv_RemoveResult.Dispose();
            HOperatorSet.TupleRemove(hv_TestResultTuple, hv_RemoveResultIndex, out hv_RemoveResult);


            //-----找出最大數值對應的區域-----*
            hv_Max.Dispose();
            HOperatorSet.TupleMax(hv_RemoveResult, out hv_Max);
            hv_MaxIndex.Dispose();
            HOperatorSet.TupleFindFirst(hv_RemoveResult, hv_Max, out hv_MaxIndex);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedMaxBorder.Dispose();
                HOperatorSet.SelectObj(ho_RegionBorder, out ho_SelectedMaxBorder, hv_MaxIndex + 1);
            }
            ho_MaxRegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_SelectedMaxBorder, out ho_MaxRegionFillUp);
            ho_ImageReduced.Dispose();
            HOperatorSet.ReduceDomain(ho_Image, ho_MaxRegionFillUp, out ho_ImageReduced);
            ho_FinalMaxImage.Dispose();
            HOperatorSet.CropDomain(ho_ImageReduced, out ho_FinalMaxImage);


            ho_SelectedMaxBorder.Dispose();
            ho_MaxRegionFillUp.Dispose();
            ho_ImageReduced.Dispose();

            hv_RemoveResultIndex.Dispose();
            hv_RemoveBorderIndex.Dispose();
            hv_TupleResult.Dispose();
            hv_Index.Dispose();
            hv_RemoveResult.Dispose();
            hv_Max.Dispose();
            hv_MaxIndex.Dispose();

        }

        /// <summary>
        /// 根據數值結果，裁出對應最大與最小的影像並輸出
        /// </summary>
        /// <param name="ho_Image"></param>
        /// <param name="ho_RegionBorder"></param>
        /// <param name="ho_FinalMinImage"></param>
        /// <param name="ho_FinalMaxImage"></param>
        /// <param name="hv_ToleranceType"></param>
        /// <param name="hv_TestResultTuple"></param>
        /// <param name="hv_Tolerance"></param>
        protected void FilterMinMaxResult(HObject ho_Image, HObject ho_RegionBorder, HObject ho_FilterRegionBorder,
      out HObject ho_FinalMinImage, out HObject ho_FinalMaxImage, out HObject ho_MaxRegionFillUp,
      HTuple hv_ToleranceType, HTuple hv_TestResultTuple, HTuple hv_Tolerance)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_RemoveRegionBorder, ho_SelectedMaxBorder = null;
            HObject ho_ImageReducedMax = null, ho_SelectedMinBorder = null;
            HObject ho_MinRegionFillUp = null, ho_ImageReducedMin = null;
            HObject ho_MaxRegionTrans, ho_RegionDilation, ho_MaxRegionBorder;

            // Local control variables 

            HTuple hv_RemoveResultIndex = new HTuple();
            HTuple hv_RemoveBorderIndex = new HTuple(), hv_TupleResult = new HTuple();
            HTuple hv_Index = new HTuple(), hv_RemoveResult = new HTuple();
            HTuple hv_Length = new HTuple(), hv_Max = new HTuple();
            HTuple hv_MaxIndex = new HTuple(), hv_Min = new HTuple();
            HTuple hv_MinIndex = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_FinalMinImage);
            HOperatorSet.GenEmptyObj(out ho_FinalMaxImage);
            HOperatorSet.GenEmptyObj(out ho_MaxRegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_RemoveRegionBorder);
            HOperatorSet.GenEmptyObj(out ho_SelectedMaxBorder);
            HOperatorSet.GenEmptyObj(out ho_ImageReducedMax);
            HOperatorSet.GenEmptyObj(out ho_SelectedMinBorder);
            HOperatorSet.GenEmptyObj(out ho_MinRegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_ImageReducedMin);
            HOperatorSet.GenEmptyObj(out ho_MaxRegionTrans);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_MaxRegionBorder);
            //-----變數建立-----*
            hv_RemoveResultIndex.Dispose();
            hv_RemoveResultIndex = new HTuple();
            hv_RemoveBorderIndex.Dispose();
            hv_RemoveBorderIndex = new HTuple();
            hv_TupleResult.Dispose();
            hv_TupleResult = new HTuple();

            switch (hv_ToleranceType.I)
            {
                //-----比較出大於等於的結果-----*
                case 1:
                    hv_TupleResult.Dispose();
                    HOperatorSet.TupleGreaterEqualElem(hv_TestResultTuple, hv_Tolerance, out hv_TupleResult);
                    break;

                //-----比較出小於等於的結果-----*
                case 2:
                    hv_TupleResult.Dispose();
                    HOperatorSet.TupleLessEqualElem(hv_TestResultTuple, hv_Tolerance, out hv_TupleResult);
                    break;

                //-----目前無其它Type-----*
                default:
                    break;

            }

            //-----篩出要移除Region的Index-----*
            for (hv_Index = 1; (int)hv_Index <= (int)(new HTuple(hv_TupleResult.TupleLength()
                )); hv_Index = (int)hv_Index + 1)
            {
                if ((int)(new HTuple(((hv_TupleResult.TupleSelect(hv_Index - 1))).TupleEqual(
                    1))) != 0)
                {
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_RemoveResultIndex = hv_RemoveResultIndex.TupleConcat(
                                hv_Index - 1);
                            hv_RemoveResultIndex.Dispose();
                            hv_RemoveResultIndex = ExpTmpLocalVar_RemoveResultIndex;
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_RemoveBorderIndex = hv_RemoveBorderIndex.TupleConcat(
                                hv_Index);
                            hv_RemoveBorderIndex.Dispose();
                            hv_RemoveBorderIndex = ExpTmpLocalVar_RemoveBorderIndex;
                        }
                    }
                }
            }

            //-----移除對應的Index數據及區域-----*
            hv_RemoveResult.Dispose();
            HOperatorSet.TupleRemove(hv_TestResultTuple, hv_RemoveResultIndex, out hv_RemoveResult);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_RemoveRegionBorder.Dispose();
                HOperatorSet.RemoveObj(ho_RegionBorder, out ho_RemoveRegionBorder, hv_RemoveResultIndex + 1);
            }

            hv_Length.Dispose();
            HOperatorSet.TupleLength(hv_RemoveResult, out hv_Length);

            if ((int)(new HTuple(hv_Length.TupleGreater(0))) != 0)
            {
                //-----找出最大數值對應的區域-----*
                hv_Max.Dispose();
                HOperatorSet.TupleMax(hv_RemoveResult, out hv_Max);
                hv_MaxIndex.Dispose();
                HOperatorSet.TupleFindFirst(hv_RemoveResult, hv_Max, out hv_MaxIndex);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_SelectedMaxBorder.Dispose();
                    HOperatorSet.SelectObj(ho_RemoveRegionBorder, out ho_SelectedMaxBorder, hv_MaxIndex + 1);
                }
                ho_MaxRegionFillUp.Dispose();
                HOperatorSet.FillUp(ho_SelectedMaxBorder, out ho_MaxRegionFillUp);
                ho_ImageReducedMax.Dispose();
                HOperatorSet.ReduceDomain(ho_Image, ho_MaxRegionFillUp, out ho_ImageReducedMax
                    );
                ho_FinalMaxImage.Dispose();
                HOperatorSet.CropDomain(ho_ImageReducedMax, out ho_FinalMaxImage);
            }
            else
            {
                ho_FinalMaxImage.Dispose();
                HOperatorSet.GenEmptyObj(out ho_FinalMaxImage);
                ho_MaxRegionFillUp.Dispose();
                HOperatorSet.GenEmptyRegion(out ho_MaxRegionFillUp);
            }

            if ((int)(new HTuple(hv_Length.TupleGreater(1))) != 0)
            {
                //-----找出最小數值對應的區域-----*
                hv_Min.Dispose();
                HOperatorSet.TupleMin(hv_RemoveResult, out hv_Min);
                hv_MinIndex.Dispose();
                HOperatorSet.TupleFindFirst(hv_RemoveResult, hv_Min, out hv_MinIndex);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_SelectedMinBorder.Dispose();
                    HOperatorSet.SelectObj(ho_RemoveRegionBorder, out ho_SelectedMinBorder, hv_MinIndex + 1);
                }
                ho_MinRegionFillUp.Dispose();
                HOperatorSet.FillUp(ho_SelectedMinBorder, out ho_MinRegionFillUp);
                ho_ImageReducedMin.Dispose();
                HOperatorSet.ReduceDomain(ho_Image, ho_MinRegionFillUp, out ho_ImageReducedMin
                    );
                ho_FinalMinImage.Dispose();
                HOperatorSet.CropDomain(ho_ImageReducedMin, out ho_FinalMinImage);
            }
            else
            {
                ho_FinalMinImage.Dispose();
                HOperatorSet.GenEmptyObj(out ho_FinalMinImage);
                //ho_MaxRegionFillUp.Dispose();
                //HOperatorSet.GenEmptyRegion(out ho_MaxRegionFillUp);

            }


            //-----將Region轉成矩形-----*
            ho_MaxRegionTrans.Dispose();
            HOperatorSet.ShapeTrans(ho_MaxRegionFillUp, out ho_MaxRegionTrans, "outer_circle");

            //-----增加矩形尺寸-----*
            //dilation_rectangle1 (MaxRegionTrans, RegionDilation, 30, 30)
            //dilation_circle (MaxRegionTrans, RegionDilation, 3.5)
            ho_RegionDilation.Dispose();
            HOperatorSet.ErosionCircle(ho_MaxRegionTrans, out ho_RegionDilation, 3.5);
            //-----取得矩形外框-----*
            ho_MaxRegionBorder.Dispose();
            HOperatorSet.Boundary(ho_RegionDilation, out ho_MaxRegionBorder, "outer");

            //-----根據設定值加粗外框-----*

            ho_MaxRegionFillUp.Dispose();
            HOperatorSet.DilationCircle(ho_MaxRegionBorder, out ho_MaxRegionFillUp, 3.5);
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.Union2(ho_FilterRegionBorder, ho_MaxRegionFillUp, out ExpTmpOutVar_0
                    );
                ho_MaxRegionFillUp.Dispose();
                ho_MaxRegionFillUp = ExpTmpOutVar_0;
            }
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.Union1(ho_MaxRegionFillUp, out ExpTmpOutVar_0);
                ho_MaxRegionFillUp.Dispose();
                ho_MaxRegionFillUp = ExpTmpOutVar_0;
            }
            ho_RemoveRegionBorder.Dispose();
            ho_SelectedMaxBorder.Dispose();
            ho_ImageReducedMax.Dispose();
            ho_SelectedMinBorder.Dispose();
            ho_MinRegionFillUp.Dispose();
            ho_ImageReducedMin.Dispose();
            ho_MaxRegionTrans.Dispose();
            ho_RegionDilation.Dispose();
            ho_MaxRegionBorder.Dispose();

            hv_RemoveResultIndex.Dispose();
            hv_RemoveBorderIndex.Dispose();
            hv_TupleResult.Dispose();
            hv_Index.Dispose();
            hv_RemoveResult.Dispose();
            hv_Length.Dispose();
            hv_Max.Dispose();
            hv_MaxIndex.Dispose();
            hv_Min.Dispose();
            hv_MinIndex.Dispose();

            return;
        }

    }
}

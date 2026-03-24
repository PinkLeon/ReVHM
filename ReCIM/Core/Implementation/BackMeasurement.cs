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

    public class BackMeasurement : Measurement
    {
        /// <summary>
        /// 正面量測應該要有自己ID,string ID = FrontSide,後續sort可拿來用
        /// </summary>


        //private AOICore _aOICore;

        public MeasurementTable measurement { get; set; }

        //private ICoreParameter coreParameter;
        //public BackMeasurement(ICoreParameter coreParameter, AOICore aOICore) : base(coreParameter, aOICore)
        //{
        //    this.coreParameter = coreParameter;
        //    _aOICore = aOICore;
        //    Image = aOICore?.BackImage;
        //}

        public async override Task<Result> Do(ICoreParameter coreParameter, AOICore aOICore)
        {
            this.Image = aOICore.BackImage;
            await Task.Delay(10);
            return new Result();
        }

        /// <summary>
        /// 根據FindCornerRegion得到的四個角落範圍，篩選出NG的位置在邊上或角落
        /// </summary>
        /// <param name="ho_Image"></param>
        /// <param name="ho_DefectRegion"></param>
        /// <param name="ho_DefectBorder"></param>
        /// <param name="ho_DefectBorderOut"></param>
        /// <param name="hv_DefectValues"></param>
        /// <param name="hv_JudgeType"></param>
        /// <param name="hv_CornerSize"></param>
        /// <param name="hv_DefectValuesOut"></param>
        ///修改版-修正倒角角落範圍設定
        public void RegionFilter(HObject ho_Image, HObject ho_DefectRegion, HObject ho_DefectBorder,
      out HObject ho_DefectBorderOut, HTuple hv_DefectValues, HTuple hv_JudgeType,
      HTuple hv_CornerSize, HTuple hv_ChamferSize, out HTuple hv_DefectValuesOut)
        {

            // Local iconic variables 

            HObject ho_EmptyRegion, ho_CornerRegion, ho_ChamferRegion;
            HObject ho_EdgeRegion, ho_ObjectSelected = null, ho_RegionIntersection = null;
            HObject ho_RegionIntersection2 = null, ho_RegionIntersection3 = null;
            HObject ho_DefectRegionOut = null;

            // Local control variables 

            HTuple hv_iReservedCorner = new HTuple(), hv_iReservedEdge = new HTuple();
            HTuple hv_iReservedChamfer = new HTuple(), hv_i = new HTuple();
            HTuple hv_iReserved = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_DefectBorderOut);
            HOperatorSet.GenEmptyObj(out ho_EmptyRegion);
            HOperatorSet.GenEmptyObj(out ho_CornerRegion);
            HOperatorSet.GenEmptyObj(out ho_ChamferRegion);
            HOperatorSet.GenEmptyObj(out ho_EdgeRegion);
            HOperatorSet.GenEmptyObj(out ho_ObjectSelected);
            HOperatorSet.GenEmptyObj(out ho_RegionIntersection);
            HOperatorSet.GenEmptyObj(out ho_RegionIntersection2);
            HOperatorSet.GenEmptyObj(out ho_RegionIntersection3);
            HOperatorSet.GenEmptyObj(out ho_DefectRegionOut);
            hv_DefectValuesOut = new HTuple();
            //-----變數建立-----*
            hv_iReservedCorner.Dispose();
            hv_iReservedCorner = new HTuple();
            hv_iReservedEdge.Dispose();
            hv_iReservedEdge = new HTuple();
            hv_iReservedChamfer.Dispose();
            hv_iReservedChamfer = new HTuple();
            ho_EmptyRegion.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_EmptyRegion);
            //-----找出角落區域-----*
            ho_CornerRegion.Dispose(); ho_ChamferRegion.Dispose(); ho_EdgeRegion.Dispose();
            FindCornerRegion(ho_Image, out ho_CornerRegion, out ho_ChamferRegion, out ho_EdgeRegion,
                hv_CornerSize, hv_ChamferSize);

            //=====判斷區域在角落或四邊上=====*

            for (hv_i = 1; (int)hv_i <= (int)(new HTuple(hv_DefectValues.TupleLength())); hv_i = (int)hv_i + 1)
            {
                ho_ObjectSelected.Dispose();
                HOperatorSet.SelectObj(ho_DefectRegion, out ho_ObjectSelected, hv_i);
                //-----檢查Defect區域是否跟角落區域重疊-----*
                ho_RegionIntersection.Dispose();
                HOperatorSet.Intersection(ho_CornerRegion, ho_ObjectSelected, out ho_RegionIntersection
                    );
                //-----檢查Defect區域是否同時和邊緣跟導角區域重疊,若全部區域都位於倒角才算-----*
                ho_RegionIntersection2.Dispose();
                HOperatorSet.Difference(ho_ObjectSelected, ho_ChamferRegion, out ho_RegionIntersection2
                    );
                //intersection (ChamferRegion, ObjectSelected, RegionIntersection2)
                //-----檢查Defect區域是否跟邊緣區域重疊-----*
                ho_RegionIntersection3.Dispose();
                HOperatorSet.Intersection(ho_EdgeRegion, ho_ObjectSelected, out ho_RegionIntersection3
                    );
                if ((int)((new HTuple(ho_RegionIntersection.TestEqualObj(ho_EmptyRegion)).TupleNot())) != 0)
                {
                    //-----Defect區域在角落-----*
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_iReservedCorner = hv_iReservedCorner.TupleConcat(
                                hv_i);
                            hv_iReservedCorner.Dispose();
                            hv_iReservedCorner = ExpTmpLocalVar_iReservedCorner;
                        }
                    }

                }
                else if ((int)(new HTuple(ho_RegionIntersection2.TestEqualObj(ho_EmptyRegion))) != 0)
                {
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_iReservedChamfer = hv_iReservedChamfer.TupleConcat(
                                hv_i);
                            hv_iReservedChamfer.Dispose();
                            hv_iReservedChamfer = ExpTmpLocalVar_iReservedChamfer;
                        }
                    }
                }
                else if ((int)((new HTuple(ho_RegionIntersection3.TestEqualObj(ho_EmptyRegion)).TupleNot())) != 0)
                {
                    //-----Defect區域橫跨在倒角和四邊上-----*
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_iReservedEdge = hv_iReservedEdge.TupleConcat(
                                hv_i);
                            hv_iReservedEdge.Dispose();
                            hv_iReservedEdge = ExpTmpLocalVar_iReservedEdge;
                        }
                    }
                }
            }

            //iReservedRemain := []
            //=====將在四邊範圍的區域資料篩出=====*
            if ((int)(new HTuple(hv_JudgeType.TupleEqual("Corner"))) != 0)
            {
                hv_iReserved.Dispose();
                hv_iReserved = new HTuple(hv_iReservedCorner);
            }
            else if ((int)(new HTuple(hv_JudgeType.TupleEqual("Chamfer"))) != 0)
            {
                hv_iReserved.Dispose();
                hv_iReserved = new HTuple(hv_iReservedChamfer);
                //*iReservedRemain:= iReservedEdge
            }
            else
            {
                hv_iReserved.Dispose();
                hv_iReserved = new HTuple(hv_iReservedEdge);
            }
            //*判斷Tuple是否已初始化
            //tuple_length (iReserved, Len1)
            //tuple_length (iReservedRemain, Len2)

            //if (Len2!=0)
            //select_obj (DefectRegion, DefectRegionOut, iReservedRemain)
            //remove_obj (DefectRegion, ChamferDefectRegion, iReservedRemain)
            //select_obj (DefectBorder, DefectBorderOut, iReservedRemain)
            //*若有邊緣鳥嘴,則從缺陷區域去除,輸出剩下倒角區域
            //remove_obj (DefectBorder, ChamferDefectBorderOut, iReservedRemain)
            //*輸出有鳥嘴的region和border的影像
            //tuple_select (DefectValues, iReservedRemain-1, DefectValuesOut)
            //else
            //*輸出有鳥嘴的region和border的Tuple(index)值
            ho_DefectRegionOut.Dispose();
            HOperatorSet.SelectObj(ho_DefectRegion, out ho_DefectRegionOut, hv_iReserved);
            ho_DefectBorderOut.Dispose();
            HOperatorSet.SelectObj(ho_DefectBorder, out ho_DefectBorderOut, hv_iReserved);
            //*輸出有鳥嘴的region和border的影像
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_DefectValuesOut.Dispose();
                HOperatorSet.TupleSelect(hv_DefectValues, hv_iReserved - 1, out hv_DefectValuesOut);
            }
            //endif

            ho_EmptyRegion.Dispose();
            ho_CornerRegion.Dispose();
            ho_ChamferRegion.Dispose();
            ho_EdgeRegion.Dispose();
            ho_ObjectSelected.Dispose();
            ho_RegionIntersection.Dispose();
            ho_RegionIntersection2.Dispose();
            ho_RegionIntersection3.Dispose();
            ho_DefectRegionOut.Dispose();

            hv_iReservedCorner.Dispose();
            hv_iReservedEdge.Dispose();
            hv_iReservedChamfer.Dispose();
            hv_i.Dispose();
            hv_iReserved.Dispose();

            return;
        }


        /// <summary>
        /// 找出基板四個角落，並根據長度建立一個四方形的範圍
        /// </summary>
        /// <param name="ho_Image"></param>
        /// <param name="ho_CornerRegion"></param>
        /// <param name="hv_CornerLength"></param>
        ///修正角落和倒角設定範圍
        // Procedures 
        public void FindCornerRegion(HObject ho_Image, out HObject ho_CornerRegion, out HObject ho_ChamferRegion,
      out HObject ho_EdgeRegion, HTuple hv_CornerSize, HTuple hv_ChamferSize)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_ImageZoomed, ho_SelectedRegions;
            HObject ho_RegionBorder, ho_RegionTrans, ho_RegionClosing;
            HObject ho_BinImage, ho_Rectangle, ho_Rectangle2, ho_Contours;
            HObject ho_ContoursSplit, ho_RegionLines, ho_AroundRegions;
            HObject ho_AroundXld, ho_UpLine, ho_DownLine, ho_LeftLine;
            HObject ho_RightLine, ho_AroundRegionLines, ho_SelectedRegions1;
            HObject ho_SelectedRegions2, ho_ChamferLineRegion, ho_ChamferRegionDilation;
            HObject ho_RegionIntersection, ho_RegionUnion1, ho_ChamferRegionTemp;
            HObject ho_ObjectSelected1 = null, ho_ChamferRegionIntersection = null;
            HObject ho_ChamferRegionRectangle = null, ho_CornerRegionTemp = null;
            HObject ho_ChamferRegionZoomed, ho_CornerRegionZoomed, ho_EdgeRegionUnion;

            // Local control variables 

            HTuple hv_ZoomFactor = new HTuple(), hv_CNS = new HTuple();
            HTuple hv_CFS = new HTuple(), hv_Width = new HTuple();
            HTuple hv_Height = new HTuple(), hv_Row1 = new HTuple();
            HTuple hv_Column1 = new HTuple(), hv_Tol = new HTuple();
            HTuple hv_Angle_Plus45_Upper = new HTuple(), hv_Angle_Plus45_Lower = new HTuple();
            HTuple hv_Angle_Plus135_Upper = new HTuple(), hv_Angle_Plus135_Lower = new HTuple();
            HTuple hv_Angle_Minus45_Upper = new HTuple(), hv_Angle_Minus45_Lower = new HTuple();
            HTuple hv_Angle_Minus135_Upper = new HTuple(), hv_Angle_Minus135_Lower = new HTuple();
            HTuple hv_MaxDis = new HTuple(), hv_SelectPt = new HTuple();
            HTuple hv_Index = new HTuple(), hv_DistanceMin = new HTuple();
            HTuple hv_DistanceMax = new HTuple(), hv_Number1 = new HTuple();
            HTuple hv_Index1 = new HTuple(), hv_Area1 = new HTuple();
            HTuple hv_Row2 = new HTuple(), hv_Column2 = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_CornerRegion);
            HOperatorSet.GenEmptyObj(out ho_ChamferRegion);
            HOperatorSet.GenEmptyObj(out ho_EdgeRegion);
            HOperatorSet.GenEmptyObj(out ho_ImageZoomed);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder);
            HOperatorSet.GenEmptyObj(out ho_RegionTrans);
            HOperatorSet.GenEmptyObj(out ho_RegionClosing);
            HOperatorSet.GenEmptyObj(out ho_BinImage);
            HOperatorSet.GenEmptyObj(out ho_Rectangle);
            HOperatorSet.GenEmptyObj(out ho_Rectangle2);
            HOperatorSet.GenEmptyObj(out ho_Contours);
            HOperatorSet.GenEmptyObj(out ho_ContoursSplit);
            HOperatorSet.GenEmptyObj(out ho_RegionLines);
            HOperatorSet.GenEmptyObj(out ho_AroundRegions);
            HOperatorSet.GenEmptyObj(out ho_AroundXld);
            HOperatorSet.GenEmptyObj(out ho_UpLine);
            HOperatorSet.GenEmptyObj(out ho_DownLine);
            HOperatorSet.GenEmptyObj(out ho_LeftLine);
            HOperatorSet.GenEmptyObj(out ho_RightLine);
            HOperatorSet.GenEmptyObj(out ho_AroundRegionLines);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions2);
            HOperatorSet.GenEmptyObj(out ho_ChamferLineRegion);
            HOperatorSet.GenEmptyObj(out ho_ChamferRegionDilation);
            HOperatorSet.GenEmptyObj(out ho_RegionIntersection);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion1);
            HOperatorSet.GenEmptyObj(out ho_ChamferRegionTemp);
            HOperatorSet.GenEmptyObj(out ho_ObjectSelected1);
            HOperatorSet.GenEmptyObj(out ho_ChamferRegionIntersection);
            HOperatorSet.GenEmptyObj(out ho_ChamferRegionRectangle);
            HOperatorSet.GenEmptyObj(out ho_CornerRegionTemp);
            HOperatorSet.GenEmptyObj(out ho_ChamferRegionZoomed);
            HOperatorSet.GenEmptyObj(out ho_CornerRegionZoomed);
            HOperatorSet.GenEmptyObj(out ho_EdgeRegionUnion);
            hv_ZoomFactor.Dispose();
            hv_ZoomFactor = 1;
            hv_CNS.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_CNS = hv_CornerSize * hv_ZoomFactor;
            }
            hv_CFS.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_CFS = hv_ChamferSize * hv_ZoomFactor;
            }
            //-----縮小影像尺寸-----*
            ho_ImageZoomed.Dispose();
            HOperatorSet.ZoomImageFactor(ho_Image, out ho_ImageZoomed, hv_ZoomFactor, hv_ZoomFactor,
                "constant");
            //旋轉圖片
            //rotate_image (ImageZoomed, ImageZoomed, 2, 'constant')
            //-----取得目前影像尺寸-----*
            hv_Width.Dispose(); hv_Height.Dispose();
            HOperatorSet.GetImageSize(ho_ImageZoomed, out hv_Width, out hv_Height);
            //-----找出樣品區域-----*
            ho_SelectedRegions.Dispose();
            FindForeGround(ho_ImageZoomed, out ho_SelectedRegions);

            ho_RegionBorder.Dispose();
            HOperatorSet.Boundary(ho_SelectedRegions, out ho_RegionBorder, "inner");
            //-----將其轉為矩形-----*
            ho_RegionTrans.Dispose();
            HOperatorSet.ShapeTrans(ho_SelectedRegions, out ho_RegionTrans, "rectangle2");
            //-----消除板邊雜點-----*
            ho_RegionClosing.Dispose();
            HOperatorSet.ClosingRectangle1(ho_RegionTrans, out ho_RegionClosing, 100, 100);
            //-----轉成Binary影像-----*
            ho_BinImage.Dispose();
            HOperatorSet.RegionToBin(ho_RegionClosing, out ho_BinImage, 255, 0, hv_Width,
                hv_Height);
            //-----找出4個角落位置-----*
            hv_Row1.Dispose(); hv_Column1.Dispose();
            HOperatorSet.PointsHarris(ho_BinImage, 0.7, 2, 0.08, 1000, out hv_Row1, out hv_Column1);
            //-----以角落位置為中心，向外產生一角落範圍矩形-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_Rectangle.Dispose();
                HOperatorSet.GenRectangle1(out ho_Rectangle, hv_Row1 - hv_CNS, hv_Column1 - hv_CNS,
                    hv_Row1 + hv_CNS, hv_Column1 + hv_CNS);
            }
            //-----以角落位置為中心，向外產生一導角範圍矩形-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_Rectangle2.Dispose();
                HOperatorSet.GenRectangle1(out ho_Rectangle2, hv_Row1 - hv_CFS, hv_Column1 - hv_CFS,
                    hv_Row1 + hv_CFS, hv_Column1 + hv_CFS);
            }
            //-----分割外框線段-----*
            //*將一個骨架分割成多條獨立的線段且限制分割距離
            ho_Contours.Dispose();
            HOperatorSet.GenContourRegionXld(ho_RegionBorder, out ho_Contours, "border");
            //*將骨架轉xld,避免分割失敗
            ho_ContoursSplit.Dispose();
            HOperatorSet.SegmentContoursXld(ho_Contours, out ho_ContoursSplit, "lines", 5,
                4, 2);
            //*再轉回region
            ho_RegionLines.Dispose();
            HOperatorSet.GenRegionContourXld(ho_ContoursSplit, out ho_RegionLines, "margin");
            //=====篩出上下左右邊線=====*
            //找出四邊檢測區域
            ho_AroundRegions.Dispose();
            FindAroundRegions(ho_RegionClosing, out ho_AroundRegions, 100, 100);
            //-----按四邊檢測區域排列順序,找出四邊線段-----*
            ho_AroundXld.Dispose();
            FindAroundLines(ho_RegionLines, ho_AroundRegions, out ho_AroundXld);
            //*將四個邊分成上下左右排列
            ho_UpLine.Dispose(); ho_DownLine.Dispose(); ho_LeftLine.Dispose(); ho_RightLine.Dispose();
            SortLines(ho_ImageZoomed, ho_AroundRegions, ho_AroundXld, out ho_UpLine, out ho_DownLine,
                out ho_LeftLine, out ho_RightLine);
            //*全部區域減去角落和倒角區域即為邊緣
            //-----從分開的外框移除較短線段-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions1.Dispose();
                HOperatorSet.SelectShape(ho_RegionLines, out ho_SelectedRegions1, "max_diameter",
                    "and", 8 * hv_ZoomFactor, 99999);
            }

            //-----設定要判斷的導角角度-----*
            hv_Tol.Dispose();
            hv_Tol = 15;
            hv_Angle_Plus45_Upper.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Angle_Plus45_Upper = ((45 + hv_Tol)).TupleRad()
                    ;
            }
            hv_Angle_Plus45_Lower.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Angle_Plus45_Lower = ((45 - hv_Tol)).TupleRad()
                    ;
            }
            hv_Angle_Plus135_Upper.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Angle_Plus135_Upper = ((135 + hv_Tol)).TupleRad()
                    ;
            }
            hv_Angle_Plus135_Lower.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Angle_Plus135_Lower = ((135 - hv_Tol)).TupleRad()
                    ;
            }
            hv_Angle_Minus45_Upper.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Angle_Minus45_Upper = ((-45 + hv_Tol)).TupleRad()
                    ;
            }
            hv_Angle_Minus45_Lower.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Angle_Minus45_Lower = ((-45 - hv_Tol)).TupleRad()
                    ;
            }
            hv_Angle_Minus135_Upper.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Angle_Minus135_Upper = ((-135 + hv_Tol)).TupleRad()
                    ;
            }
            hv_Angle_Minus135_Lower.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Angle_Minus135_Lower = ((-135 - hv_Tol)).TupleRad()
                    ;
            }

            //*從全部區域找到與倒角角度範圍符合區域
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions2.Dispose();
                HOperatorSet.SelectShape(ho_SelectedRegions1, out ho_SelectedRegions2, (((new HTuple("orientation")).TupleConcat(
                    "orientation")).TupleConcat("orientation")).TupleConcat("orientation"), "or",
                    ((((hv_Angle_Plus45_Lower.TupleConcat(hv_Angle_Plus135_Lower))).TupleConcat(
                    hv_Angle_Minus45_Lower))).TupleConcat(hv_Angle_Minus135_Lower), ((((hv_Angle_Plus45_Upper.TupleConcat(
                    hv_Angle_Plus135_Upper))).TupleConcat(hv_Angle_Minus45_Upper))).TupleConcat(
                    hv_Angle_Minus135_Upper));
            }
            //*若倒角包含線段有兩種以上的角度,挑角落點和基板距離最長的那條線,再合併
            hv_MaxDis.Dispose();
            hv_MaxDis = 0;
            hv_SelectPt.Dispose();
            hv_SelectPt = new HTuple();
            for (hv_Index = 0; (int)hv_Index <= 3; hv_Index = (int)hv_Index + 1)
            {
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_DistanceMin.Dispose(); hv_DistanceMax.Dispose();
                    HOperatorSet.DistancePr(ho_SelectedRegions, hv_Row1.TupleSelect(hv_Index),
                        hv_Column1.TupleSelect(hv_Index), out hv_DistanceMin, out hv_DistanceMax);
                }
                if ((int)(new HTuple(hv_DistanceMin.TupleGreater(hv_MaxDis))) != 0)
                {
                    hv_MaxDis.Dispose();
                    hv_MaxDis = new HTuple(hv_DistanceMin);
                    hv_SelectPt.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_SelectPt = new HTuple();
                        hv_SelectPt = hv_SelectPt.TupleConcat(hv_Row1.TupleSelect(
                            hv_Index));
                        hv_SelectPt = hv_SelectPt.TupleConcat(hv_Column1.TupleSelect(
                            hv_Index));
                    }
                }
            }
            //*把最大角落點加大,找到與他干涉的線,即為倒角
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_ChamferLineRegion.Dispose();
                HOperatorSet.GenRegionPoints(out ho_ChamferLineRegion, hv_SelectPt.TupleSelect(
                    0), hv_SelectPt.TupleSelect(1));
            }
            ho_ChamferRegionDilation.Dispose();
            HOperatorSet.DilationRectangle1(ho_ChamferLineRegion, out ho_ChamferRegionDilation,
                300, 300);
            ho_RegionIntersection.Dispose();
            HOperatorSet.Intersection(ho_ChamferRegionDilation, ho_SelectedRegions2, out ho_RegionIntersection
                );
            //*與倒角角度範圍符合區域合併
            ho_RegionUnion1.Dispose();
            HOperatorSet.Union1(ho_RegionIntersection, out ho_RegionUnion1);
            //*找到倒角後,產生對應矩形
            ho_Rectangle2.Dispose();
            HOperatorSet.ShapeTrans(ho_RegionUnion1, out ho_Rectangle2, "rectangle1");
            //*放大
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.DilationRectangle1(ho_Rectangle2, out ExpTmpOutVar_0, hv_CFS * 2,
                    hv_CFS * 2);
                ho_Rectangle2.Dispose();
                ho_Rectangle2 = ExpTmpOutVar_0;
            }
            //*全部區域減去角落和倒角區域即為邊緣
            ho_EdgeRegion.Dispose();
            HOperatorSet.Difference(ho_RegionLines, ho_Rectangle2, out ho_EdgeRegion);
            ho_AroundRegionLines.Dispose();
            HOperatorSet.ConcatObj(ho_UpLine, ho_DownLine, out ho_AroundRegionLines);
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_AroundRegionLines, ho_LeftLine, out ExpTmpOutVar_0);
                ho_AroundRegionLines.Dispose();
                ho_AroundRegionLines = ExpTmpOutVar_0;
            }
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_AroundRegionLines, ho_RightLine, out ExpTmpOutVar_0
                    );
                ho_AroundRegionLines.Dispose();
                ho_AroundRegionLines = ExpTmpOutVar_0;
            }
            //-----與原角落位置比對，找出導角位置-----*
            ho_ChamferRegionTemp.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_ChamferRegionTemp);
            //找4個角落中哪個是導角
            hv_Number1.Dispose();
            HOperatorSet.CountObj(ho_Rectangle, out hv_Number1);
            HTuple end_val89 = hv_Number1;
            HTuple step_val89 = 1;
            for (hv_Index1 = 1; hv_Index1.Continue(end_val89, step_val89); hv_Index1 = hv_Index1.TupleAdd(step_val89))
            {
                ho_ObjectSelected1.Dispose();
                HOperatorSet.SelectObj(ho_Rectangle, out ho_ObjectSelected1, hv_Index1);
                //*從四角落矩形找到有與篩選之斜線角度干涉的區域
                ho_ChamferRegionIntersection.Dispose();
                HOperatorSet.Intersection(ho_Rectangle2, ho_ObjectSelected1, out ho_ChamferRegionIntersection
                    );
                hv_Area1.Dispose(); hv_Row2.Dispose(); hv_Column2.Dispose();
                HOperatorSet.AreaCenter(ho_ChamferRegionIntersection, out hv_Area1, out hv_Row2,
                    out hv_Column2);
                if ((int)(new HTuple(hv_Area1.TupleGreater(0))) != 0)
                {
                    //*干涉面積>0,即為找到區域
                    ho_ChamferRegionTemp.Dispose();
                    ho_ChamferRegionTemp = new HObject(ho_ObjectSelected1);
                    ho_ChamferRegionRectangle.Dispose();
                    HOperatorSet.Intersection(ho_ChamferRegionTemp, ho_Rectangle2, out ho_ChamferRegionRectangle
                        );
                    ho_CornerRegionTemp.Dispose();
                    HOperatorSet.RemoveObj(ho_Rectangle, out ho_CornerRegionTemp, hv_Index1);
                }
                else
                {
                }

            }

            //-----將矩形合成同Region-----*
            ho_ChamferRegionZoomed.Dispose();
            HOperatorSet.Union1(ho_ChamferRegionRectangle, out ho_ChamferRegionZoomed);
            ho_CornerRegionZoomed.Dispose();
            HOperatorSet.Union1(ho_CornerRegionTemp, out ho_CornerRegionZoomed);
            //*全部外框減去角落和倒角即為邊緣
            ho_EdgeRegionUnion.Dispose();
            HOperatorSet.Union1(ho_EdgeRegion, out ho_EdgeRegionUnion);
            //-----將Region轉成原影像尺寸比例-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_ChamferRegion.Dispose();
                HOperatorSet.ZoomRegion(ho_ChamferRegionZoomed, out ho_ChamferRegion, 1 / hv_ZoomFactor,
                    1 / hv_ZoomFactor);
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_CornerRegion.Dispose();
                HOperatorSet.ZoomRegion(ho_CornerRegionZoomed, out ho_CornerRegion, 1 / hv_ZoomFactor,
                    1 / hv_ZoomFactor);
            }
            //*將原本屬於邊緣區域輸出
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_EdgeRegion.Dispose();
                HOperatorSet.ZoomRegion(ho_EdgeRegionUnion, out ho_EdgeRegion, 1 / hv_ZoomFactor,
                    1 / hv_ZoomFactor);
            }

            if (HDevWindowStack.IsOpen())
            {
                HOperatorSet.ClearWindow(HDevWindowStack.GetActive());
            }
            if (HDevWindowStack.IsOpen())
            {
                HOperatorSet.DispObj(ho_Image, HDevWindowStack.GetActive());
            }
            if (HDevWindowStack.IsOpen())
            {
                //dev_set_color ('red')
            }
            if (HDevWindowStack.IsOpen())
            {
                //dev_display (ChamferRegion)
            }
            if (HDevWindowStack.IsOpen())
            {
                HOperatorSet.SetColor(HDevWindowStack.GetActive(), "green");
            }
            if (HDevWindowStack.IsOpen())
            {
                HOperatorSet.DispObj(ho_CornerRegion, HDevWindowStack.GetActive());
            }
            if (HDevWindowStack.IsOpen())
            {
                HOperatorSet.SetColor(HDevWindowStack.GetActive(), "yellow");
            }
            if (HDevWindowStack.IsOpen())
            {
                HOperatorSet.DispObj(ho_EdgeRegion, HDevWindowStack.GetActive());
            }

            ho_ImageZoomed.Dispose();
            ho_SelectedRegions.Dispose();
            ho_RegionBorder.Dispose();
            ho_RegionTrans.Dispose();
            ho_RegionClosing.Dispose();
            ho_BinImage.Dispose();
            ho_Rectangle.Dispose();
            ho_Rectangle2.Dispose();
            ho_Contours.Dispose();
            ho_ContoursSplit.Dispose();
            ho_RegionLines.Dispose();
            ho_AroundRegions.Dispose();
            ho_AroundXld.Dispose();
            ho_UpLine.Dispose();
            ho_DownLine.Dispose();
            ho_LeftLine.Dispose();
            ho_RightLine.Dispose();
            ho_AroundRegionLines.Dispose();
            ho_SelectedRegions1.Dispose();
            ho_SelectedRegions2.Dispose();
            ho_ChamferLineRegion.Dispose();
            ho_ChamferRegionDilation.Dispose();
            ho_RegionIntersection.Dispose();
            ho_RegionUnion1.Dispose();
            ho_ChamferRegionTemp.Dispose();
            ho_ObjectSelected1.Dispose();
            ho_ChamferRegionIntersection.Dispose();
            ho_ChamferRegionRectangle.Dispose();
            ho_CornerRegionTemp.Dispose();
            ho_ChamferRegionZoomed.Dispose();
            ho_CornerRegionZoomed.Dispose();
            ho_EdgeRegionUnion.Dispose();

            hv_ZoomFactor.Dispose();
            hv_CNS.Dispose();
            hv_CFS.Dispose();
            hv_Width.Dispose();
            hv_Height.Dispose();
            hv_Row1.Dispose();
            hv_Column1.Dispose();
            hv_Tol.Dispose();
            hv_Angle_Plus45_Upper.Dispose();
            hv_Angle_Plus45_Lower.Dispose();
            hv_Angle_Plus135_Upper.Dispose();
            hv_Angle_Plus135_Lower.Dispose();
            hv_Angle_Minus45_Upper.Dispose();
            hv_Angle_Minus45_Lower.Dispose();
            hv_Angle_Minus135_Upper.Dispose();
            hv_Angle_Minus135_Lower.Dispose();
            hv_MaxDis.Dispose();
            hv_SelectPt.Dispose();
            hv_Index.Dispose();
            hv_DistanceMin.Dispose();
            hv_DistanceMax.Dispose();
            hv_Number1.Dispose();
            hv_Index1.Dispose();
            hv_Area1.Dispose();
            hv_Row2.Dispose();
            hv_Column2.Dispose();

            return;
        }

        /// <summary>
        /// 將四個邊分成上下左右排列
        /// </summary>
        /// <param name="ho_Image"></param>
        /// <param name="ho_AroundRegions"></param>
        /// <param name="ho_AroundXld"></param>
        /// <param name="ho_UpLine"></param>
        /// <param name="ho_DownLine"></param>
        /// <param name="ho_LeftLine"></param>
        /// <param name="ho_RightLine"></param>
        public void SortLines(HObject ho_Image, HObject ho_AroundRegions, HObject ho_AroundXld,
      out HObject ho_UpLine, out HObject ho_DownLine, out HObject ho_LeftLine, out HObject ho_RightLine)
        {



            // Local control variables 

            HTuple hv_iAround = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_UpLine);
            HOperatorSet.GenEmptyObj(out ho_DownLine);
            HOperatorSet.GenEmptyObj(out ho_LeftLine);
            HOperatorSet.GenEmptyObj(out ho_RightLine);
            //=====依序處理每個邊=====*

            for (hv_iAround = 1; (int)hv_iAround <= 4; hv_iAround = (int)hv_iAround + 1)
            {
                //-----篩出同一個邊的線段及檢測區域-----*


                switch (hv_iAround.I)
                {
                    //-----上下區域-----*
                    case 1:
                        ho_UpLine.Dispose();
                        HOperatorSet.SelectObj(ho_AroundXld, out ho_UpLine, hv_iAround);
                        goto case 2;
                    case 2:
                        //找到與垂直或水平板邊平行,最小的線距


                        ho_DownLine.Dispose();
                        HOperatorSet.SelectObj(ho_AroundXld, out ho_DownLine, hv_iAround);
                        //-----如果對應的平行線段小於設定值，就認為規格錯誤，輸出0-----*

                        break;

                    case 3:
                        ho_LeftLine.Dispose();
                        HOperatorSet.SelectObj(ho_AroundXld, out ho_LeftLine, hv_iAround);
                        goto case 4;
                    case 4:

                        ho_RightLine.Dispose();
                        HOperatorSet.SelectObj(ho_AroundXld, out ho_RightLine, hv_iAround);

                        break;
                }
            }

            hv_iAround.Dispose();

            return;
        }

        /// <summary>
        /// 按四邊檢測區域排列順序,找出四邊線段
        /// </summary>
        /// <param name="ho_RegionLines"></param>
        /// <param name="ho_DetectRangeRegions"></param>
        /// <param name="ho_FitLineXld"></param>
        public void FindAroundLines(HObject ho_RegionLines, HObject ho_DetectRangeRegions,
          out HObject ho_FitLineXld)
        {



            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_FitLineRegions = null, ho_ObjectSelected1 = null;
            HObject ho_RegionDilation = null, ho_ObjectSelected2 = null;
            HObject ho_RegionDifference = null, ho_RegionUnion = null, ho_Contours1 = null;

            // Local control variables 

            HTuple hv_DetectNo = new HTuple(), hv_SktNo = new HTuple();
            HTuple hv_iDetect = new HTuple(), hv_iSkt = new HTuple();
            HTuple hv_Area1 = new HTuple(), hv_Row1 = new HTuple();
            HTuple hv_Column1 = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_FitLineXld);
            HOperatorSet.GenEmptyObj(out ho_FitLineRegions);
            HOperatorSet.GenEmptyObj(out ho_ObjectSelected1);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_ObjectSelected2);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion);
            HOperatorSet.GenEmptyObj(out ho_Contours1);
            //-----計算檢測區域的數量-----*
            hv_DetectNo.Dispose();
            HOperatorSet.CountObj(ho_DetectRangeRegions, out hv_DetectNo);
            //-----計算邊界的Skeleton線段數量-----*
            hv_SktNo.Dispose();
            HOperatorSet.CountObj(ho_RegionLines, out hv_SktNo);
            //-----建立最後要擬線的XLD-----*
            ho_FitLineXld.Dispose();
            HOperatorSet.GenEmptyObj(out ho_FitLineXld);
            HTuple end_val6 = hv_DetectNo;
            HTuple step_val6 = 1;
            for (hv_iDetect = 1; hv_iDetect.Continue(end_val6, step_val6); hv_iDetect = hv_iDetect.TupleAdd(step_val6))
            {
                //-----建立暫存要用來擬線的線段-----*
                ho_FitLineRegions.Dispose();
                HOperatorSet.GenEmptyRegion(out ho_FitLineRegions);
                //-----依序選擇上下左右檢測區域-----*
                ho_ObjectSelected1.Dispose();
                HOperatorSet.SelectObj(ho_DetectRangeRegions, out ho_ObjectSelected1, hv_iDetect);
                //-----擴大檢測區域----*
                ho_RegionDilation.Dispose();
                HOperatorSet.DilationRectangle1(ho_ObjectSelected1, out ho_RegionDilation,
                    200, 200);
                //-----找出重疊檢測區域的邊緣線段-----*
                HTuple end_val14 = hv_SktNo;
                HTuple step_val14 = 1;
                for (hv_iSkt = 1; hv_iSkt.Continue(end_val14, step_val14); hv_iSkt = hv_iSkt.TupleAdd(step_val14))
                {
                    //-----依序選擇邊緣的線段-----*
                    ho_ObjectSelected2.Dispose();
                    HOperatorSet.SelectObj(ho_RegionLines, out ho_ObjectSelected2, hv_iSkt);
                    //-----找出線段與檢測區域的相異處-----*
                    ho_RegionDifference.Dispose();
                    HOperatorSet.Difference(ho_ObjectSelected2, ho_RegionDilation, out ho_RegionDifference
                        );
                    //-----算出相異處的面積-----*
                    hv_Area1.Dispose(); hv_Row1.Dispose(); hv_Column1.Dispose();
                    HOperatorSet.AreaCenter(ho_RegionDifference, out hv_Area1, out hv_Row1, out hv_Column1);
                    //-----若有相異區域，表示線段沒有整個在檢測區域內-----*
                    if ((int)(new HTuple(hv_Area1.TupleEqual(0))) != 0)
                    {
                        //-----表示整個線段都在檢測區域內-----*
                        {
                            HObject ExpTmpOutVar_0;
                            HOperatorSet.ConcatObj(ho_FitLineRegions, ho_ObjectSelected2, out ExpTmpOutVar_0
                                );
                            ho_FitLineRegions.Dispose();
                            ho_FitLineRegions = ExpTmpOutVar_0;
                        }
                    }
                }
                //-----將其合併為同個區域-----*
                ho_RegionUnion.Dispose();
                HOperatorSet.Union1(ho_FitLineRegions, out ho_RegionUnion);
                //-----將區域轉換成XLD-----*
                ho_Contours1.Dispose();
                HOperatorSet.GenContoursSkeletonXld(ho_RegionUnion, out ho_Contours1, 1, "filter");
                //-----將檢測區域的所有XLD串在一起，供後面使用-----*
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.ConcatObj(ho_FitLineXld, ho_Contours1, out ExpTmpOutVar_0);
                    ho_FitLineXld.Dispose();
                    ho_FitLineXld = ExpTmpOutVar_0;
                }

            }

            ho_FitLineRegions.Dispose();
            ho_ObjectSelected1.Dispose();
            ho_RegionDilation.Dispose();
            ho_ObjectSelected2.Dispose();
            ho_RegionDifference.Dispose();
            ho_RegionUnion.Dispose();
            ho_Contours1.Dispose();

            hv_DetectNo.Dispose();
            hv_SktNo.Dispose();
            hv_iDetect.Dispose();
            hv_iSkt.Dispose();
            hv_Area1.Dispose();
            hv_Row1.Dispose();
            hv_Column1.Dispose();

            return;
        }

        /// <summary>
        /// 找出四邊檢測區域
        /// </summary>
        /// <param name="ho_Region"></param>
        /// <param name="ho_AroundRegions"></param>
        /// <param name="hv_HorizontalPitch"></param>
        /// <param name="hv_VerticalPitch"></param>
        public void FindAroundRegions(HObject ho_Region, out HObject ho_AroundRegions,
          HTuple hv_HorizontalPitch, HTuple hv_VerticalPitch)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_UBRegionErosion, ho_UBRegionDifference;
            HObject ho_UBRegions, ho_ConnectedUBRegions, ho_SortedUBRegions;
            HObject ho_LRRegionErosion, ho_LRRegionDifference, ho_LRRegions;
            HObject ho_ConnectedLRRegions, ho_SortedLRRegions, ho_UpperRegion;
            HObject ho_BottomRegion, ho_LeftRegion, ho_RightRegion;
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_AroundRegions);
            HOperatorSet.GenEmptyObj(out ho_UBRegionErosion);
            HOperatorSet.GenEmptyObj(out ho_UBRegionDifference);
            HOperatorSet.GenEmptyObj(out ho_UBRegions);
            HOperatorSet.GenEmptyObj(out ho_ConnectedUBRegions);
            HOperatorSet.GenEmptyObj(out ho_SortedUBRegions);
            HOperatorSet.GenEmptyObj(out ho_LRRegionErosion);
            HOperatorSet.GenEmptyObj(out ho_LRRegionDifference);
            HOperatorSet.GenEmptyObj(out ho_LRRegions);
            HOperatorSet.GenEmptyObj(out ho_ConnectedLRRegions);
            HOperatorSet.GenEmptyObj(out ho_SortedLRRegions);
            HOperatorSet.GenEmptyObj(out ho_UpperRegion);
            HOperatorSet.GenEmptyObj(out ho_BottomRegion);
            HOperatorSet.GenEmptyObj(out ho_LeftRegion);
            HOperatorSet.GenEmptyObj(out ho_RightRegion);
            //=====根據規格篩出邊緣往內縮的第一條線區域=====*
            //-----篩出上下區域-----*
            ho_UBRegionErosion.Dispose();
            HOperatorSet.ErosionRectangle1(ho_Region, out ho_UBRegionErosion, 1, hv_VerticalPitch);
            //erosion_rectangle1 (Region, UBRegionErosion, VerticalPitch, VerticalPitch)
            ho_UBRegionDifference.Dispose();
            HOperatorSet.Difference(ho_Region, ho_UBRegionErosion, out ho_UBRegionDifference
                );
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_UBRegions.Dispose();
                HOperatorSet.ErosionRectangle1(ho_UBRegionDifference, out ho_UBRegions, 100,
                    hv_VerticalPitch * 0.1);
            }

            //-----排序上下區域-----*
            ho_ConnectedUBRegions.Dispose();
            HOperatorSet.Connection(ho_UBRegions, out ho_ConnectedUBRegions);
            ho_SortedUBRegions.Dispose();
            HOperatorSet.SortRegion(ho_ConnectedUBRegions, out ho_SortedUBRegions, "character",
                "true", "row");

            //-----篩出左右區域-----*
            ho_LRRegionErosion.Dispose();
            HOperatorSet.ErosionRectangle1(ho_Region, out ho_LRRegionErosion, hv_HorizontalPitch,
                hv_HorizontalPitch);
            ho_LRRegionDifference.Dispose();
            HOperatorSet.Difference(ho_Region, ho_LRRegionErosion, out ho_LRRegionDifference
                );
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_LRRegions.Dispose();
                HOperatorSet.ErosionRectangle1(ho_LRRegionDifference, out ho_LRRegions, hv_HorizontalPitch * 0.1,
                    100);
            }

            //-----排序左右區域-----*
            ho_ConnectedLRRegions.Dispose();
            HOperatorSet.Connection(ho_LRRegions, out ho_ConnectedLRRegions);
            ho_SortedLRRegions.Dispose();
            HOperatorSet.SortRegion(ho_ConnectedLRRegions, out ho_SortedLRRegions, "character",
                "true", "column");

            //-----上下左右區域-----*
            ho_UpperRegion.Dispose();
            HOperatorSet.SelectObj(ho_SortedUBRegions, out ho_UpperRegion, 1);
            ho_BottomRegion.Dispose();
            HOperatorSet.SelectObj(ho_SortedUBRegions, out ho_BottomRegion, 2);
            ho_LeftRegion.Dispose();
            HOperatorSet.SelectObj(ho_SortedLRRegions, out ho_LeftRegion, 1);
            ho_RightRegion.Dispose();
            HOperatorSet.SelectObj(ho_SortedLRRegions, out ho_RightRegion, 2);

            //----將上下左右區域依序串一起，方便迴圈使用-----*
            ho_AroundRegions.Dispose();
            HOperatorSet.ConcatObj(ho_UpperRegion, ho_BottomRegion, out ho_AroundRegions);
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_AroundRegions, ho_LeftRegion, out ExpTmpOutVar_0);
                ho_AroundRegions.Dispose();
                ho_AroundRegions = ExpTmpOutVar_0;
            }
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_AroundRegions, ho_RightRegion, out ExpTmpOutVar_0);
                ho_AroundRegions.Dispose();
                ho_AroundRegions = ExpTmpOutVar_0;
            }

            ho_UBRegionErosion.Dispose();
            ho_UBRegionDifference.Dispose();
            ho_UBRegions.Dispose();
            ho_ConnectedUBRegions.Dispose();
            ho_SortedUBRegions.Dispose();
            ho_LRRegionErosion.Dispose();
            ho_LRRegionDifference.Dispose();
            ho_LRRegions.Dispose();
            ho_ConnectedLRRegions.Dispose();
            ho_SortedLRRegions.Dispose();
            ho_UpperRegion.Dispose();
            ho_BottomRegion.Dispose();
            ho_LeftRegion.Dispose();
            ho_RightRegion.Dispose();


            return;
        }

        /// <summary>
        /// 找出樣品區域
        /// </summary>
        /// <param name="ho_Image"></param>
        /// <param name="ho_RegionTrans"></param>
        public void FindForeGround(HObject ho_Image, out HObject ho_RegionTrans)
        {



            // Local iconic variables 

            HObject ho_Regions, ho_ObjectsReduced, ho_RegionUnion1;
            HObject ho_ConnectedRegions1, ho_SelectedRegions1;

            // Local control variables 

            HTuple hv_Area1 = new HTuple(), hv_Row1 = new HTuple();
            HTuple hv_Column1 = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_RegionTrans);
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_ObjectsReduced);
            HOperatorSet.GenEmptyObj(out ho_RegionUnion1);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions1);
            //----自動二值化-----*
            ho_Regions.Dispose();
            HOperatorSet.AutoThreshold(ho_Image, out ho_Regions, 3);
            //-----移除外圍黑色區域-----*
            ho_ObjectsReduced.Dispose();
            HOperatorSet.RemoveObj(ho_Regions, out ho_ObjectsReduced, 1);
            //-----將其餘區域聯通-----*
            ho_RegionUnion1.Dispose();
            HOperatorSet.Union1(ho_ObjectsReduced, out ho_RegionUnion1);
            //-----分離基板區域跟其餘雜點-----*
            ho_ConnectedRegions1.Dispose();
            HOperatorSet.Connection(ho_RegionUnion1, out ho_ConnectedRegions1);
            //-----找出所有區域面積-----*
            hv_Area1.Dispose(); hv_Row1.Dispose(); hv_Column1.Dispose();
            HOperatorSet.AreaCenter(ho_ConnectedRegions1, out hv_Area1, out hv_Row1, out hv_Column1);
            //-----保留最大區域-----*
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                ho_SelectedRegions1.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions1, out ho_SelectedRegions1, "area",
                    "and", hv_Area1.TupleMax(), (hv_Area1.TupleMax()) + 1);
            }
            //-----預防交插線區域被篩掉-----*
            ho_RegionTrans.Dispose();
            HOperatorSet.FillUp(ho_SelectedRegions1, out ho_RegionTrans);

            ho_Regions.Dispose();
            ho_ObjectsReduced.Dispose();
            ho_RegionUnion1.Dispose();
            ho_ConnectedRegions1.Dispose();
            ho_SelectedRegions1.Dispose();

            hv_Area1.Dispose();
            hv_Row1.Dispose();
            hv_Column1.Dispose();

            return;
        }
    }
}

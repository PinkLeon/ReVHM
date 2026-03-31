using HalconDotNet;
using System.Collections.Generic;
using VHM.Interface;
using VHM.Model;


namespace VHM.Implementation
{
    /// <summary>
    /// 正面動作(產品);若一樣是正面動作,但是跑模擬,prepare和perform就可以改變一下
    /// 產品層,相關資料要放這,
    /// </summary>
    public class FrontAction : Action
    {
        /// <summary>
        /// inspection是工廠方法
        /// </summary>
        IInspection inspection;

        private List<TestSpectation> tolerances = new List<TestSpectation>();

        private List<TestSpectation> Spec = new List<TestSpectation>();

        private List<bool> FrontResults = new List<bool>();

        //量測結果也要放這
        private List<MeasurementTable> measurementTables;

        private HObject hObject;

        private AOICore AOICore;

        /// <summary>
        /// Action是產品(介面)
        /// </summary>
        /// <param name="inspection"></param>
        public FrontAction(IInspection inspection, string recipe, string type)
        {
            this.inspection = inspection;
            this.Recipe = recipe;
            this.Type = type;
        }

        /// <summary>
        /// 準備不同組資源,光源控制,相機選擇,量測方法,判斷是否合規方法
        /// </summary>

        public override void prepare()
        {
            LightControl = inspection.CreateLightControl();

            PhotoTaking = inspection.GenImage();
            //算法已經有了 還需要? 流程而已,從Global給
            //MeasurementList = inspection.CreateMeasurement();
            //算法已經有了 還需要? 從算法結果去撈,標準值,公差,結果
            //Judgement = inspection.CreateJudgement();

        }

        /// <summary>
        /// 取得必須建議的物件後,執行SOP,這時候,才要傳參數近來(和製作Pizza時,bake,cut 製程一樣)
        /// 流程層(傳遞參數,到資料層處理) -->有需要在裡面做?
        /// </summary>
        public override void Perform()
        {
            //1.取得圖像
            FrontImage = PhotoTaking.Snap();

            //2.由圖像,跑各算法確認是否符合標準

            //foreach (var ele in MeasurementList)
            //{
            //    ele.Do(FrontImage, AOICore);
            //    measurementTables.Add(ele.measurement);
            //}

            //3.由資料庫,取得量測值,公差和標準值,比較並儲存結果
            //須知道,是哪個處方,規格是啥,,表格怎建? spec 檢測項目 檢測編號
            //公差表格,對應spec表項目,紀錄規格值和公差,公差又分上下限 goole-keep MySQL有紀錄
            //Spec = Judgement.GetRecipe(Recipe);


            //FrontResults = Judgement.Judge(measurementTables.Select(n => n.Value.ToString("F2")).ToList(), Spec);

            //4.通知plc 進入下一站
            //用Global做

        }
    }
}

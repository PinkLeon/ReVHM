using Core.Interface;
using Core.Model;
using HalconDotNet;
using System.Collections.Generic;


namespace Core.Implementation
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

        /// <summary>
        /// 流程和資料如何分開?
        /// 將資料依賴注入,在流程內將資料做修改?
        /// </summary>
        //量測結果也要放這
        private List<MeasurementTable> measurementTables;
        public List<Result> resulList;
        private HObject hObject;

        private AOICore AOICore;


        /// <summary>
        /// Action是產品(介面)
        /// </summary>
        /// <param name="inspection"></param>
        public FrontAction(IInspection inspection, string recipe, string type, ICoreParameter coreParameter) : base(coreParameter)
        {
            this.inspection = inspection;
            this.Recipe = recipe;
            this.Type = type;
            this.coreParameter = coreParameter;
        }

        /// <summary>
        /// 準備不同組資源,光源控制,相機選擇,量測方法,判斷是否合規方法
        /// </summary>

        public override void prepare()
        {
            //但Action需要分front和back? 不能當共用介面?
            //1.這邊應該可保留,流程一樣,但不同工廠做不一樣的操作
            //2.還是不用那麼複雜,用繼承的,都用frontAction,但prepare在正反面及模擬,做不一樣的事
            //但1,2好像類似
            LightControl = inspection.CreateLightControl();

            PhotoTaking = inspection.GenImage();
            //算法已經有了 還需要? 流程而已,從Global給
            //這個基本沒用
            //MeasurementList = inspection.CreateMeasurement();
            //算法已經有了 還需要? 從算法結果去撈,標準值,公差,結果
            //Judgement = inspection.CreateJudgement();

        }

        /// <summary>
        /// 取得必須建議的物件後,執行SOP,這時候,才要傳參數近來(和製作Pizza時,bake,cut 製程一樣)
        /// 流程層(傳遞參數,到資料層處理) -->有需要在裡面做?
        /// 還是要建立狀態機,做固定流程?
        /// </summary>
        public override void Perform()
        {
            ////1.取得圖像
            //FrontImage = PhotoTaking.Snap();

            //step 1:開燈  燈光這類,可以在Global做,隨程式開啟和關閉
            LightControl.Initialize();
            LightControl.TurnON();

            //step 2:呼叫擷取卡,做線掃描
            PhotoTaking.Acquire();
            //step 3:移動平台至拍照位
            //讓平台開始移動，讓Line Scan相機取像
            //await Global.PLCTFrontUse.WriteMR("12", true);

            //step 4:取像
            FrontImage = PhotoTaking.Snap();

        }
    }
}

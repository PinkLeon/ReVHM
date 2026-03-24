using Core.Implementation;
using Core.Interface;
using Core.Model;
using HalconDotNet;

namespace AOI.Model
{
    /// <summary>
    /// AOI資料層
    /// </summary>
    public class AOIContext
    {
        //全部測項結果,若都無錯誤,才是ok 
        public List<Result> Result { get; set; } = new List<Result>();

        private readonly ICoreParameter _coreParameter;

        //public FrontInspection frontInspection { get; set; }

        public HObject CurrentImage { get; set; }

        public HObject FrontImage { get; set; }

        public HObject BackImage { get; set; }

        public AOICore AOICore { get; set; }



        /// <summary>
        /// 將缺陷位置Overlay到原圖後的影像
        /// </summary>
        public HObject OverlayImage { get; set; }

        public List<string> ListColors = new List<string>()
                                          { "red", "green", "blue", "cyan", "magenta", "yellow", "medium slate blue", "orange red", "spring green", "cadet blue", "coral" };


        public List<Measurement> Measurements { get; set; }


        public AOIContext(HObject frontImage, HObject backImage, ICoreParameter coreParameter)
        {
            _coreParameter = coreParameter;
            FrontImage = frontImage;
            BackImage = backImage;
            Measurements = new List<Measurement>() { new MissingLine(),
                                        new BackDirty() };
        }

        public List<TestSpectation> InitializeAOIParameter(AOICore aOICore, string type)
        {

            //讀取影像縮放設定
            aOICore.ZoomFactor = double.TryParse(_coreParameter.ZoomFactor, out double zoomfactor) ? zoomfactor : 0;

            //由資料庫,取得處方的資料
            var specData = GetSpec(type).OrderBy(n => n.id).ToList();
            //下面這些要從資料庫可以撈得到
            aOICore.TestSpectations = specData.OrderBy(n => n.id).ToList();
            aOICore.GridHeight = 0.8;
            aOICore.GridWidth = 1.5;
            aOICore.PixelSize = 5;
            aOICore.Thickness = 0.39;
            AOICore = aOICore;

            return specData;
        }

        /// <summary>
        /// 由規格型號,查詢test_spec表格,得到型號各測項目的規格值
        /// </summary>
        /// <param name="type">型號</param>
        /// <returns></returns>
        public List<TestSpectation> GetSpec(string type)
        {
            List<TestSpectation> specs = new List<TestSpectation>();
            string sql = $@"select A.item_name,B.standardvalue,B.tolerance,B.unit,
                            A.id from test_item as A  
                            left join test_spec as B
                            on A.id = B.test_item_id
                            left join product as C
                            on C.id = B.product_id;";
            Console.WriteLine(sql);
            specs = _coreParameter.MH.GetALL<TestSpectation>(sql).ToList();
            return specs;
        }
    }
}

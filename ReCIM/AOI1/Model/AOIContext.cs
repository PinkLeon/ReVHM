using HalconDotNet;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VHM.Interface;
using VHM.Model;
using static VHM.App;

namespace AOI.Model
{
    public class AOIContext
    {
        //全部測項結果,若都無錯誤,才是ok 
        public List<Task<Result>> Result { get; set; } = new List<Task<Result>>();


        //public FrontInspection frontInspection { get; set; }

        public HObject CurrentImage { get; set; }

        public AOICore AOICore { get; set; }

        /// <summary>
        /// 將缺陷位置Overlay到原圖後的影像
        /// </summary>
        public HObject OverlayImage { get; set; }

        public List<string> ListColors = new List<string>()
                                          { "red", "green", "blue", "cyan", "magenta", "yellow", "medium slate blue", "orange red", "spring green", "cadet blue", "coral" };

        public void InitializeAOIParameter(AOICore aOICore, Result result, string type)
        {

            //讀取影像縮放設定
            aOICore.ZoomFactor = double.TryParse(Global.ZoomFactor, out double zoomfactor) ? zoomfactor : 0;

            //由資料庫,取得處方的資料
            var a = result.GetSpec(type);

            aOICore.Tolerance = a.First().tolerance;

            aOICore.ToleranceType = a.First().tolerance_type;

            aOICore.Interval = a.First().interval;

            AOICore = aOICore;
        }

    }
}

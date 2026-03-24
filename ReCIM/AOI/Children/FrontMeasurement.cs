using AOI.Interface;
using HalconDotNet;

namespace AOI.Model
{
    /// <summary>
    /// 正面量測算法 直線性,歪斜,髒污,水平垂直邊距,找四邊,找正面區域,
    /// 共同:(找出並標示缺陷位置,以閥值篩選)
    /// 除了方法,要利用組合class 顯示量測值和顯示類別的屬性
    /// 先把大項目,拆成小項目,IMeasurement 應該改成抽象類別,可共用屬性,方法
    /// 正面先挑漏線,髒污來試試
    /// 多個檢測項目怎辦? 利用FrontEnum和BackEnum ? 用stringlist 方便閱讀
    /// </summary>
    public class FrontMeasurement : Measurement
    {
        HObject frontDefect;

        /// <summary>
        /// 將量測分為正,背面量測
        /// 各自做各自的檢測
        /// 正:1.找四邊,2.找出不同規格的格子寬高,各佔多少pixel(寬高to um /pixelsize *ZoomFactor )
        /// </summary>
        /// <param name="hObject"></param>

        public FrontMeasurement(HObject hObject) : base(hObject)
        {

        }

        public override void Do(HObject image)
        {

        }
    }
}

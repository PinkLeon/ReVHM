using HalconDotNet;
using System.Collections.Generic;
using VHM.Interface;
using static VHM.App;

namespace VHM.Implementation
{
    /// <summary>
    /// 正面檢測工廠
    /// </summary>
    public class FrontInspection : IInspection
    {
        static HObject hObject = new HObject();
        /// <summary>
        /// 理論上這裡不應該有參數吧,應該在資料層 ,這裡是?會改變配料,輸入才是由這裡給
        /// </summary>
        /// "MissingLine", "FrontDirty", "HorizontalPitch", "BackDirty", "CrackCorner"
        //List<Measurement> _measurements = new List<Measurement>()
        //{ new FrontDirty(), new MissingLine() };

        public ILightControl CreateLightControl()
        {
            return new FLControl();
        }

        public IAcquire GenImage()
        {
            if (Global.Simulation)
            {
                return new FrontGenImage();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 可new 一組FrontMeasurement list,使用時,讓此組list Do算法
        /// </summary>
        /// <returns></returns>
        public List<Measurement> CreateMeasurement()
        {
            return Global.InspectionList;
        }

        public Result CreateJudgement()
        {
            return new FrontResult();
        }

    }
}

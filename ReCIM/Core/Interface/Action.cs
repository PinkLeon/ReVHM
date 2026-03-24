using HalconDotNet;
using System.Collections.Generic;

namespace Core.Interface
{
    /// <summary>
    /// 抽象產品(動作),建立多項動作,實作後可呼叫各動作的方法(取像動作,控燈,量測)
    /// 產品層,產品狀態相關資料要放這,正面圖像,背面圖像
    /// </summary>
    public abstract class Action
    {
        public ILightControl LightControl { get; set; }

        public IAcquire PhotoTaking { get; set; }

        public List<Measurement> MeasurementList { get; set; }

        public Result Judgement { get; set; }



        public abstract void prepare();

        public HObject FrontImage { get; set; }

        public HObject FrontOverlayImage { get; set; }

        public HObject BackImage { get; set; }

        public HObject BackOverlayImage { get; set; }

        public string Recipe;

        public string Type;

        protected ICoreParameter coreParameter;

        public Action(ICoreParameter coreParameter)
        {
            this.coreParameter = coreParameter;
        }

        public virtual void Perform() { }
    }
}

using Core.Interface;
using HalconDotNet;

namespace Core.Implementation
{
    public class FAction : IAction
    {
        private ICoreParameter _coreParameter;
        public FAction(ICoreParameter coreParameter) : base(coreParameter)
        {
            _coreParameter = coreParameter;
        }

        public override void Step1()
        {
            _coreParameter.fCamera.Initialize();
        }

        public override void Step2()
        {

        }

        public override HObject GetImage()
        {
            return new HObject();
        }
    }
}

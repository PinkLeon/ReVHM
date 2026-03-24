using Core.Interface;
using HalconDotNet;

namespace Core.Implementation
{
    public class BSimAction : IAction
    {
        private ICoreParameter _coreParameter;

        public BSimAction(ICoreParameter coreParameter) : base(coreParameter)
        {
            _coreParameter = coreParameter;
        }
        public override void Step1()
        {
            _coreParameter.bLControl.Initialize();
        }

        public override void Step2()
        {
            _coreParameter.bCamera.Acquire();
            //做線掃描
            //FrontImage = fCamera.Snap();
        }

        public override HObject GetImage()
        {
            var _Image = _coreParameter.bCamera.Snap();
            return _Image;
        }

        public override void Step4()
        {

        }
    }
}

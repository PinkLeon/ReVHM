using Core.Interface;
using HalconDotNet;

namespace Core.Implementation
{
    public class FSimAction : IAction
    {
        private ICoreParameter _coreParameter;

        public FSimAction(ICoreParameter coreParameter) : base(coreParameter)
        {
            _coreParameter = coreParameter;
        }

        public override void Step1()
        {
            _coreParameter.fLControl.Initialize();
        }

        public override void Step2()
        {
            _coreParameter.fCamera.Acquire();
            //做線掃描
            //FrontImage = fCamera.Snap();
        }

        public override HObject GetImage()
        {
            var _Image = _coreParameter.fCamera.Snap();
            return _Image;
        }
    }
}

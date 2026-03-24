using Core.Interface;
using HalconDotNet;

namespace Core.Implementation
{
    public class BAction : IAction
    {
        private ICoreParameter _coreParameter;

        public BAction(ICoreParameter coreParameter) : base(coreParameter)
        {
            _coreParameter = coreParameter;
        }
        public override void Step1()
        {

        }

        public override void Step2()
        {

        }

        public override HObject GetImage()
        {
            return new HObject();
        }

        public override void Step4()
        {

        }
    }
}

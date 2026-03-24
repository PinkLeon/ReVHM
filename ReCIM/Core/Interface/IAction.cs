using HalconDotNet;

namespace Core.Interface
{
    public abstract class IAction
    {
        private ICoreParameter _coreParameter;

        //public virtual FLControl fLControl { get; set; }

        //public virtual BLControl bLControl { get; set; }

        public IAction(ICoreParameter coreParameter)
        {
            _coreParameter = coreParameter;
        }

        public abstract void Step1();
        public abstract void Step2();

        public abstract HObject GetImage();

        public virtual void Step4()
        {

        }

    }
}

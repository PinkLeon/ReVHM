using Core.Implementation;

namespace Core.Interface
{
    /// <summary>
    /// 正背面要實作的抽象類別
    /// </summary>
    public class FlowProcess
    {
        //不同情境下,做不同的事情
        private ICoreParameter _coreParameter;
        public IAction fAction;
        public IAction bAction;

        public FlowProcess(ICoreParameter coreParameter)
        {
            _coreParameter = coreParameter;
            //初始化燈源
            _coreParameter.fLControl.Initialize();
            _coreParameter.fCamera.Initialize();
        }

        public void Create()
        {
            if (!_coreParameter.Simulation)
            {
                fAction = new FAction(_coreParameter);
                bAction = new BAction(_coreParameter);
            }
            else
            {
                fAction = new FSimAction(_coreParameter);
                bAction = new BSimAction(_coreParameter);
            }
        }


        public void FrontProcess()
        {
            fAction.Step1();
            fAction.Step2();
        }
        public void BackProcess()
        {
            bAction.Step1();
            bAction.Step2();
        }

    }
}

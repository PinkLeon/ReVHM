using Core.Interface;
using Core.Model;
using System.Threading.Tasks;

namespace Core.Implementation
{

    public class FrontDirty : FrontMeasurement
    {
        //private ICoreParameter _coreParameter;
        //public AOICore _aOICore;

        //public FrontDirty(ICoreParameter coreParameter, AOICore aOICore) : base(coreParameter, aOICore)

        //{
        //    _coreParameter = coreParameter;
        //    _aOICore = aOICore;
        //}

        public async override Task<Result> Do(ICoreParameter coreParameter, AOICore aOICore)
        {
            await Task.Delay(1);
            return new Result();
        }
    }
}

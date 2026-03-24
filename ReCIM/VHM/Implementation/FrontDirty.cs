using HalconDotNet;
using System.Threading.Tasks;
using VHM.Interface;
using VHM.Model;

namespace VHM.Implementation
{
    public class FrontDirty : FrontMeasurement
    {
        public FrontDirty()
        {

        }

        public async override Task<Result> Do(HObject hObject, AOICore aOICore)
        {
            await Task.Delay(1);
            return new FrontResult();
        }
    }
}

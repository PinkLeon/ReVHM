using Core.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Core.Implementation
{
    /// <summary>
    /// 正面光源控制
    /// </summary>
    public class BLControl : ILightControl
    {
        List<string> LightChanel;
        public void Initialize()
        {
            //光源設定
            LightChanel = new List<string>() { "604", "605" };
        }

        public async void TurnON()
        {
            //開啟光源
            foreach (var LC in LightChanel)
            {
                //await Global.PLCTFrontUse.WriteMR(LC, true);
                //等待光源穩定時間
                await Task.Delay(20);
            }
        }

        public async void TurnOFF()
        {
            //關閉光源
            foreach (var LC in LightChanel)
            {
                //await Global.PLCTFrontUse.WriteMR(LC, false);
                //等待PLC傳輸時間
                await Task.Delay(20);
            }
        }
    }
}

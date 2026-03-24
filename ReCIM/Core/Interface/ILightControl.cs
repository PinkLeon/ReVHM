

namespace Core.Interface
{
    /// <summary>
    ///燈光控制的介面-需實作:初始化,開燈,關燈
    /// </summary>
    public interface ILightControl
    {
        //初始化
        void Initialize();
        //開燈
        void TurnON();
        //關燈
        void TurnOFF();


    }
}

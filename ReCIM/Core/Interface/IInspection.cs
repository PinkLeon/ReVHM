namespace Core.Interface
{
    /// <summary>
    ///抽象工廠-每個實作的類別都必須做檢測相關作業,每個檢測作業又有各自的方法
    ///資料層
    /// </summary>
    public interface IInspection
    {

        //開關燈
        ILightControl CreateLightControl();
        //取像
        IAcquire GenImage();
        ////量測
        //List<Measurement> CreateMeasurement();
        ////判定規格
        Result CreateJudgement();
        ////和PLC交握後,移動至下一動
        ///


    }
}

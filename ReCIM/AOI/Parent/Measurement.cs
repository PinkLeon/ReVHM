using HalconDotNet;

namespace AOI.Interface
{
    /// <summary>
    /// 量測class為抽象類別,可共用屬性,方法
    /// 1.找到正面還是背面,看圖的尺寸
    /// 2.分離前景和背景
    /// 3.找出四邊
    /// 4.量測
    /// 5.找出瑕疵區域
    /// 6.要有各量測值?或正背面分開,應該要一起
    /// </summary>
    public abstract class Measurement
    {

        public HObject HObject;



        public Measurement(HObject image)
        {
            this.HObject = image;
        }

        public abstract void Do(HObject image);


        //1.找到正面還是背面,看圖的尺寸
        //public void GetHObject(HObject hObject)
        //{
        //    HObject = hObject;
        //}
        //2.分離前景和背景

        //3.找出四邊

        //4.量測

        //5.找出瑕疵區域


    }
}

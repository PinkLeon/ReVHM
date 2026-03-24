using Core.Interface;
using HalconDotNet;

namespace Core.Implementation
{
    public class FrontGenImage : IAcquire
    {
        public HTuple ExposureTime { get; set; }

        public string CameraID { get; set; }

        public HObject hObject { get; set; }



        public void SetExposureTime(HTuple time)
        {
            ExposureTime = time;
        }

        public void Initialize()
        {

        }

        public void Close()
        {
        }

        public HObject Snap()
        {
            //HObject Hoimage = new HObject();
            //string imgPath = @"D:\新增資料夾\驗收報告\2024-11-22-001\000002\FrontImage.png";
            string imgPath = @"D:\project\VHM\soft\Ceramic Inspection Machine(v114)\CIM\Image\0603漏線";
            HOperatorSet.ReadImage(out HObject Hoimage, imgPath);
            HOperatorSet.CopyObj(Hoimage, out HObject HoimageCopy, 1, -1);
            return HoimageCopy;
        }

        public void Acquire()
        {

        }

    }
}

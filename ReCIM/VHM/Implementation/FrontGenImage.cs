using HalconDotNet;
using VHM.Interface;

namespace VHM.Implementation
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
            string imgPath = @"D:\project\VHM\soft\Ceramic Inspection Machine(v114)\CIM\Image\0603漏線";
            HOperatorSet.ReadImage(out HObject Hoimage, imgPath);
            return Hoimage;
        }

        public void Acquire()
        {

        }

    }
}

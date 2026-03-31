using HalconDotNet;

namespace VHM.Interface
{
    /// <summary>
    /// 取像相關功能
    /// </summary>
    public interface IAcquire
    {

        HTuple ExposureTime { get; set; }

        string CameraID { get; set; }

        HObject Snap();

        void Acquire();

        void Initialize();

        void Close();

        void SetExposureTime(HTuple time);
    }
}

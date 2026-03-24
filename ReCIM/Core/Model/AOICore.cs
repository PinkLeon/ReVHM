using HalconDotNet;
using System.Collections.Generic;

namespace Core.Model
{
    /// <summary>
    /// AOI 會用的共同屬性
    /// </summary>
    public class AOICore
    {
        public double ZoomFactor { get; set; } = 0;

        /// <summary>
        /// 基板內小格子的寬度
        /// </summary>
        public double GridWidth { get; set; }

        /// <summary>
        /// 基板內小格子的高度
        /// </summary>
        public double GridHeight { get; set; }

        /// <summary>
        /// 每一個像素（pixel）在現實世界中代表多大的實際距離。
        /// </summary>
        public double PixelSize { get; set; }

        /// <summary>
        /// 基板厚度(可換算為不同型號基板)
        /// </summary>
        public double Thickness { get; set; }


        public List<TestSpectation> TestSpectations { get; set; }

        public HObject FrontImage { get; set; }

        public HObject BackImage { get; set; }

    }
}

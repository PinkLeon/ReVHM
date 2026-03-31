namespace VHM.Model
{
    public class AOICore
    {
        public double ZoomFactor { get; set; } = 0;

        /// <summary>
        /// 基板內小格子的寬度
        /// </summary>
        public int GridWidth { get; set; }

        /// <summary>
        /// 基板內小格子的高度
        /// </summary>
        public int GridHeight { get; set; }

        /// <summary>
        /// 每一個像素（pixel）在現實世界中代表多大的實際距離。
        /// </summary>
        public int PixelSize { get; set; }

        /// <summary>
        /// 基板厚度(可換算為不同型號基板)
        /// </summary>
        public int Thickness { get; set; }

        public int ToleranceType { get; set; }

        public double Tolerance { get; set; }

        public int Interval { get; set; }



    }
}

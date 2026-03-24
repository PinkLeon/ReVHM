namespace VHM.Model
{
    public class MeasurementTable
    {
        /// <summary>
        /// 顯示項目
        /// </summary>
        public string Item { get; set; }
        /// <summary>
        /// 量測值
        /// </summary>
        public double Value { get; set; }
        /// <summary>
        /// 量測編號
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 標準值
        /// </summary>
        public double Spec { get; set; }

        public double Tolerance { get; set; }
        /// <summary>
        /// OK或NG
        /// </summary>
        public string Result { get; set; }
    }
}

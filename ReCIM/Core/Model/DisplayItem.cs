namespace Core.Model
{
    public class DisplayItem
    {
        public string ItemName { get; set; }

        public string Side { get; set; }

        public string MeasuredValue { get; set; }

        public string Tolerance { get; set; }

        public string ToleranceType { get; set; }

        public string Spec { get; set; }

        public string Judgement { get; set; }

        public string Unit { get; set; }

        /// <summary>
        /// 第No片基板
        /// </summary>
        public int No { get; set; }

        public double Thickness { get; set; }

    }
}

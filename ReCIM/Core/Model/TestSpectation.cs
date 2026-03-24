namespace Core.Model
{
    public class TestSpectation
    {
        public int id { get; set; }
        /// <summary>
        /// 測試項目名稱
        /// </summary>
        public string item_name { get; set; }

        public double standardvalue { get; set; }

        public double tolerance { get; set; }

        public string unit { get; set; }
        /// <summary>
        /// 產品型號
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 公差類型
        /// </summary>
        public int tolerance_type { get; set; }

        public int interval { get; set; }


    }
}

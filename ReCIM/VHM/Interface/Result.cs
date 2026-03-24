using Core;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using VHM.Model;


namespace VHM.Interface
{
    /// <summary>
    /// 由規格和公差,決定結果
    /// 此介面包含取得公差,儲存結果-->應該可以變class
    /// </summary>
    public abstract class Result
    {
        /// <summary>
        /// 全部項目檢測ok,才算PASS
        /// </summary>
        public bool FrontResult { get; set; }

        public bool BackResult { get; set; }

        public HObject DefectRegions { get; set; }

        public HTuple DefectValues { get; set; }

        public HTuple ClassNumber { get; set; }



        /// <summary>
        /// 各檢測項目量測值
        /// </summary>
        public List<HTuple> AOIinspections { get; set; }

        public List<string> Items = new List<string>() { "MissingLine", "FrontDirty", "HorizontalPitch", "BackDirty", "CrackCorner" };

        public List<string> PixelToUm(List<string> measuredValues)
        {
            List<string> covertValues = new List<string>();
            //對每個項目進行,量測值,公差,標準值比對

            //正面換算pixel 多少um 1 pixel = 5 um
            // 16384x13000 (像素) pixel 
            //利用Calibration:  換算係數:   實際尺寸/像素長度  (背面 5328x4608) 14.49  =  7200  um / 496.89  pixel

            double FrontPixelSize = 5; // um
            //還要進行單位轉換,將pixel轉um,mm也轉um比對
            foreach (var ele in measuredValues)
            {
                covertValues.Add((double.Parse(ele) * FrontPixelSize).ToString());
            }

            return covertValues;
        }

        public List<bool> Judge(List<string> measuredValues, List<TestSpectation> specfications)
        {
            List<bool> results = new List<bool>();
            //對每個項目進行,量測值,公差,標準值比對
            List<string> standardValue = new List<string>();

            List<string> specValue = new List<string>();
            //1.找出項目裡,有對應spec內standardvalue規格的,且找到是否有unit要求,其餘有測試項目但沒規格的,都為預設值
            int i = 0;
            foreach (var ele in specfications)
            {
                //如果包含unit和standardvalue將其取出,並轉換為um
                if (ele.standardvalue != 0)
                {
                    standardValue.Add(ele.standardvalue.ToString("F2"));
                }
                //如果未包含就使用預設0,單位um
                else
                {
                    standardValue.Add("0");
                }

                //找到一致的項目,做比較
                if (Items.Contains(ele.item_name) && ele.unit == "um")
                {
                    specValue.Add(standardValue[i]);
                }
                //找到一致的項目,做比較
                else if (Items.Contains(ele.item_name) && ele.unit == "mm")
                {
                    specValue.Add((double.Parse(standardValue[i]) * 1000).ToString());
                }
                //找到一致的項目,做比較
                else if (Items.Contains(ele.item_name) && (ele.unit == "" || ele.unit == null))
                {
                    specValue.Add(standardValue[i]);
                }


                //找到不同類型公差
                switch (ele.tolerance_type)
                {
                    // 值>公差
                    case 0:
                        results.Add(double.Parse(measuredValues[i]) > (double.Parse(specValue[i]) + ele.tolerance));
                        break;
                    // 值 <公差
                    case 1:
                    default:
                        results.Add(double.Parse(measuredValues[i]) < (double.Parse(specValue[i]) + ele.tolerance));
                        break;
                    //   公差1 > 值 > 公差2
                    case 2:
                        results.Add(double.Parse(measuredValues[i]) < (double.Parse(specValue[i]) + ele.tolerance)
                                    && double.Parse(measuredValues[i]) > (double.Parse(specValue[i]) - ele.tolerance));
                        break;


                }


                i++;

            }

            //還要進行單位轉換,將pixel轉um,mm也轉um比對


            return results;
        }

        /// <summary>
        /// 由規格型號,查詢test_spec表格,得到型號各測項目的規格值
        /// </summary>
        /// <param name="type">型號</param>
        /// <returns></returns>
        public List<TestSpectation> GetSpec(string type)
        {
            List<TestSpectation> specs = new List<TestSpectation>();
            string sql = $@"select * from test_item as A  
                            left join test_spec as B
                            on A.id = B.test_item_id
                            left join product as C
                            on C.id = B.product_id
                            where C.`type` = '{type}';";
            Console.WriteLine(sql);
            specs = Global.MH.GetALL<TestSpectation>(sql).ToList();
            return specs;
        }

        /// <summary>
        /// 由處方取得測試項目公差及規格
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
        public List<TestSpectation> GetRecipe(string recipe)
        {
            List<TestSpectation> specs = new List<TestSpectation>();
            //List<string> specs = new List<string>();
            string sql = $@"select * from recipe as A  
                            left join test_item as B
                            on A.test_item_id = B.id
                            left join test_spec as C
                            on A.test_item_id = C.test_item_id
                            where A.`Name` = '{recipe}' and A.status = '1';";
            specs = Global.MH.GetALL<TestSpectation>(sql).ToList();
            return specs;
        }
    }
}

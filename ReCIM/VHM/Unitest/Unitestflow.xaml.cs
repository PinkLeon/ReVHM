using Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VHM.Inplementation;
using CheckBox = System.Windows.Controls.CheckBox;

namespace VHM.UniTest
{
    /// <summary>
    /// Architecture.xaml 的互動邏輯
    /// </summary>
    public partial class UniTestflow : System.Windows.Window
    {

        private bool Simaulation = Global.ini.ReadIni("Simulation", "DB") == "1" ? true : false;

        string MessageReceiveData = "";

        // 添加訊息處理的相關欄位
        private readonly Queue<string> _messageQueue = new Queue<string>();
        private readonly object _messageLock = new object();
        private const int MAX_MESSAGE_COUNT = 1000; // 限制最大訊息數量
        private bool _isUpdatingUI = false;
        private List<string> originalData;

        public UniTestflow()
        {
            InitializeComponent();

        }


        private void CasualTestRun(object sender, RoutedEventArgs e)
        {
            var test = new FrontResult();


            var spec = test.GetRecipe("1206");

            List<string> measureddata = new List<string>() { "15", "30", "1020", "25", "20" };

            List<string> tolerances = new List<string>();

            test.Judge(measureddata, spec.OrderBy(n => n.test_item_id).ToList());
        }




        private void NeedleAllcationTestRun(object sender, RoutedEventArgs e)
        {

        }


        private void NeedleRunTestRun(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveNeedleAllcationTestRun(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveNeedleRunTestRun(object sender, RoutedEventArgs e)
        {

        }

        private void InfoClick(object sender, RoutedEventArgs e)
        {

        }

        private void DisplayRemoveNeedle(object sender, RoutedEventArgs e)
        {

        }

        private void Filter(object sender, RoutedEventArgs e)
        {


        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HandleAllSelection((CheckBox)sender);
            FilterData();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            HandleAllSelection((CheckBox)sender);
            FilterData();
        }

        private void HandleAllSelection(CheckBox checkBox)
        {

        }

        private void FilterData()
        {

        }

        private void IntoWarehouseTesting(object sender, RoutedEventArgs e)
        {

        }

        private void OutWarehouseTesting(object sender, RoutedEventArgs e)
        {

        }

        private void ConvertPositionTesting(object sender, RoutedEventArgs e)
        {


        }



        private void WarehouseDisplayedTesting(object sender, RoutedEventArgs e)
        {
        }

        private void btnNeedleDisplay_Click(object sender, RoutedEventArgs e)
        {
        }

        private void OutputConverySensorTesting(object sender, RoutedEventArgs e)
        {
        }


        private void data_for_database(object sender, RoutedEventArgs e)
        {


        }

        public async void TestBatch()
        {



        }
        private async void Batch_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateStorage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemovalTesting_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ReserveRecorded_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 開啟 XplannerLoader 測試視窗
        /// </summary>
        private void XplannerLoaderTest_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}







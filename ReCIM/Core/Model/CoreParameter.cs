using Core.API;
using Core.Implementation;
using Core.Interface;
using Core.Model;
using HalconDotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Core
{
    public class CoreParameter : ICoreParameter
    {
        //這要<再確認>下邏輯用法
        public ConcurrentQueue<Tuple<string, string, string>> LogStack = new ConcurrentQueue<Tuple<string, string, string>>();

        private bool isProcessingLogs = false;
        private readonly object lockObject = new object();
        public List<HObject> FrontMaxDefectImages { get; set; } = new List<HObject>();
        public List<HObject> FrontMinDefectImages { get; set; } = new List<HObject>();
        public List<HObject> BackMaxDefectImages { get; set; } = new List<HObject>();
        public List<HObject> BackMinDefectImages { get; set; } = new List<HObject>();
        ///// <summary>
        ///// 確認是否要開啟入料模擬
        ///// </summary>
        public bool Simulation { get; set; } = false;
        //public bool LogBusy { get; set; } = false;
        //public int ReworkNumber = 0;
        //public int PieceNumber;
        public int DefectNumber { get; set; } = 0;

        //public List<Measurement> InspectionList { get; set; }

        public FLControl fLControl { get; set; } = new FLControl();
        public FrontGenImage fCamera { get; set; } = new FrontGenImage();

        public BackGenImage bCamera { get; set; } = new BackGenImage();

        public BLControl bLControl { get; set; } = new BLControl();
        public string Lang { get; set; }

        public IniManager ini { get; set; }
        //public bool IsCameraInit;
        //public bool IsPLCInit;
        //public bool IsDBInit;
        ////public  VisionControl FrontCamera, BackCamera;
        //public bool FrontFlag;
        //public bool BackFlag;
        ////public  AIGO AIFront = new AIGO();
        ////public  AIGO AIBack = new AIGO();
        //public Thread WatchDogMission;
        ////新增一枚舉Enum,讓算法按指定的順序跑

        public AOICore aOICore { get; set; }
        public CoreParameter()
        {
            //建構式初始化必要屬性
            ini = new IniManager(
            AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini");
            Simulation = ini.ReadIni("Flow", "Simulation") == "0" ? false : true;
            //InspectionList = new List<Measurement>() { };
            fLControl = new FLControl();
            Lang = ini.ReadIni("Operation", "language");
            FrontPixelSize = ini.ReadIni("Camera", "FrontPixleSize");
            MH = new MySQLHelper(new Model.MysqlActor()
            {
                UserID = "root",
                InitialCatalog = "REVHM",
                IP = "127.0.0.1",
                Password = "CINPHOWN"
            }, this);
        }


        public enum AlgorithmOrder
        {
            HorizontalPitch,
            VerticalPitch,
            MissingLine,
            Linearity,
            SkewedLine,
            FrontDirty,
            CrackCorner,
            CrackEdge,
            BeakCorner,
            BeakEdge,
            BackDirty,
            Dimension
        }

        public List<string> StateTable { get; set; }
        public bool ConnectionStatus { get; set; } = false;

        /// <summary>
        /// 這是否需要?
        /// </summary>
        ConcurrentQueue<Tuple<string, string, string>> ICoreParameter.LogStack { get; set; }

        //為何又用顯式實作? 因為這些屬性不需要被外部直接訪問，
        //只有透過 ICoreParameter 介面才可以訪問，這樣可以更好地封裝內部實現細節。
        bool ICoreParameter.isProcessingLogs { get; set; }
        object ICoreParameter.lockObject { get; set; }
        int ICoreParameter.ReworkNumber { get; set; }
        int ICoreParameter.PieceNumber { get; set; }
        bool ICoreParameter.IsCameraInit { get; set; }
        bool ICoreParameter.IsPLCInit { get; set; }
        bool ICoreParameter.IsDBInit { get; set; }
        bool ICoreParameter.FrontFlag { get; set; }
        bool ICoreParameter.BackFlag { get; set; }
        Thread ICoreParameter.WatchDogMission { get; set; }


        // 3. 結合基底目錄和相對路徑，並解析成絕對路徑。
        // 最終結果會是: D:\Git\VHM\新增資料夾\ReCIM\UI\Setting.ini
        static string absoluteIniPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"..\..\Setting.ini")
        );


        string ICoreParameter.ZoomFactor { get; set; } = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Camera", "ZoomFactor");
        List<int> ICoreParameter.CameraID { get; set; }
        List<double> ICoreParameter.ExposureList { get; set; } = new List<double>() { 1.0 / 30.0, 1.0 / 50.0, 1.0 / 60.0, 1.0 / 100.0, 1.0 / 120.0, 1.0 / 200.0, 1.0 / 320.0 };
        //PLC初始化

        List<LanguageCore> ICoreParameter.Languagelist { get; set; }

        //public string Lang = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "language");
        public string FrontPixelSize { get; set; }
        //public  string BackPixelSize = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Camera", "BackCameraPixleSize");
        //public string ZoomFactor = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Camera", "ZoomFactor");
        //public  string SaveAllImage = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Flow", "SaveAllImage");
        //public List<Measurement> InspectionList = new List<Measurement>();

        //事件委派
        //public delegate void CIMEventEventHandle(CIMEventArg Data);
        //public  CIMEventEventHandle CIMEvent;
        //光源
        //public  LightControl LightSource = new LightControl();
        //相機ID
        //public List<int> CameraID = new List<int>();
        //使用者相關訊息
        //public  UserInfo User;
        //曝光時間
        //public List<double> ExposureList = new List<double>() { 1.0 / 30.0, 1.0 / 50.0, 1.0 / 60.0, 1.0 / 100.0, 1.0 / 120.0, 1.0 / 200.0, 1.0 / 320.0 };
        //PLC初始化
        //public  PLCOperation PLCTFrontUse = new PLCOperation(
        //                                          new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLC_IP"),
        //                                          int.Parse(new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLCPort1")));
        //public  PLCOperation PLCTBacktUse = new PLCOperation(
        //                                          new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLC_IP"),
        //                                          int.Parse(new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLCPort2")));
        //public  PLCOperation PLCTWatchDog = new PLCOperation(
        //                                          new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLC_IP"),
        //                                          int.Parse(new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLCPort3")));


        public enum RunMode
        {
            InspectionMode = 1,
            FrontMode = 2,
            BackMode = 3,
            SingleMode = 4
        }

        public MySQLHelper MH { get; set; }

        public async Task WriteLog(string type, string log, string message)
        {
            //排隊寫入log
            LogStack.Enqueue(new Tuple<string, string, string>(type, log, message));

            //啟動處理日誌的線程
            lock (lockObject)
            {
                //鎖住,在狀態為非處理時,才處理log
                if (!isProcessingLogs)
                {
                    isProcessingLogs = true;
                    Task.Run(async () => await ProcessLogs());
                }
            }
        }

        public string ConverToleranceType(int type)
        {
            switch (type)
            {
                case 0:
                    return "<";
                case 1:
                    return ">";
                case 2:
                    return "±";
                default:
                    return "?";
            }
        }
        //private  List<DisplayItem> FindOffSet()
        //{
        //    //double Offset = 0;
        //    //找到不同型號基板厚度
        //    //var thicktype = ThicknessRange(d.Thickness);
        //    //找到不同型號基板,符合測試項目的offset值
        //    string sql = String.Format(@"SELECT `value` as `mark` ,`Thickness`,`name` as `Item` FROM cim.calibration
        //                 where `mark`= 'offset'");

        //    var ItemThickness = Global.MH.GetALL(sql, typeof(DisplayItem));

        //    foreach (var item in ItemThickness)
        //    {
        //        OffSetList.Add((DisplayItem)item);
        //    }

        //    return OffSetList;

        //}

        public double ThicknessRange(double Thickness)
        {
            if (0.46 <= Thickness && Thickness <= 0.54)
            {
                return 0.5;
            }
            else if ((0.42 <= Thickness && Thickness <= 0.45))
            {
                return 0.44;

            }
            else if ((0.36 <= Thickness && Thickness <= 0.41))
            {
                return 0.39;

            }
            else if ((0.22 <= Thickness && Thickness <= 0.27))
            {
                return 0.25;
            }
            else
                return 0;

        }

        public async Task ProcessLogs()
        {
            //取出que,寫入log
            while (LogStack.TryDequeue(out var logEntry))
            {
                await ExeWriteLog(logEntry);
            }

            //當佇列為空時，將處理日誌的標誌位元設為 false
            lock (lockObject)
            {
                isProcessingLogs = false;
            }
        }

        public async Task ExeWriteLog(Tuple<string, string, string> Data)
        {

            await Task.Run(async () =>
            {
                try
                {
                    string DIRNAME = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "SystemLog");
                    string FILENAME = DIRNAME + "\\" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                    if (!Directory.Exists(DIRNAME))
                    {
                        Directory.CreateDirectory(DIRNAME);
                    }
                    FILENAME = DIRNAME + "\\" + DateTime.Now.ToString("yyyyMMdd") + ".txt";


                    if (!File.Exists(FILENAME))
                    {
                        // The File.Create method creates the file and opens a FileStream on the file. You neeed to close it.
                        File.Create(FILENAME).Close();
                    }
                    using (StreamWriter sw = File.AppendText(FILENAME))
                    {
                        if (Data != null)
                        {
                            Log(Data.Item1, Data.Item2, Data.Item3, sw);
                        }

                        sw.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }
        public void Log(string type, string log, string logMessage, TextWriter w)
        {
            w.WriteLineAsync("[" + type + "]:[" + DateTime.Now.ToString("u") + ":" + DateTime.Now.Millisecond.ToString() + "]:[" + log + logMessage + "]");
            //w.WriteLine("-------------------------------");
            //string sql = string.Format("insert into system_log (type,log,time) values('{0}','{1}','{2}')", type, log + ":" + logMessage, DateTime.Now.ToString("u") + "." + DateTime.Now.Millisecond.ToString());
            //MH.Execute(sql);
        }



        public bool PingHost(string hostUri, int portNumber)
        {
            try
            {
                using (var client = new TcpClient(hostUri, portNumber))
                    return true;
            }
            catch (SocketException ex)
            {
                //MessageBox.Show("Error pinging host:'" + hostUri + ":" + portNumber.ToString() + "'");
                return false;
            }
        }

        public IEnumerable<T> FindLogicalChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                foreach (object rawChild in LogicalTreeHelper.GetChildren(depObj))
                {
                    if (rawChild is DependencyObject)
                    {
                        DependencyObject child = (DependencyObject)rawChild;
                        if (child is T)
                        {
                            yield return (T)child;
                        }

                        foreach (T childOfChild in FindLogicalChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }

        public IEnumerable<FileInfo> GetFilesByExtensions(DirectoryInfo dir, params string[] extensions)
        {
            if (extensions == null)
                throw new ArgumentNullException("extensions");
            IEnumerable<FileInfo> files = dir.EnumerateFiles();
            return files.Where(f => extensions.Contains(f.Extension));
        }

        /// <summary>
        /// 轉換為實際基本單位
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public List<DisplayItem> ConvertRealDistance(List<DisplayItem> Data)
        {
            double Offset = 0;
            foreach (var d in Data)
            {
                ////找到不同型號基板厚度
                //var thicktype = ThicknessRange(d.Thickness);
                ////找到不同型號基板,符合測試項目的offset值
                //string sql = String.Format(@"SELECT `value` as `mark` FROM cim.calibration
                //                        where `name` = '{0}' and `thickness`= '{1}'", d.ItemName, thicktype);

                //var ItemThickness = MH.GetALL<DisplayItem>(sql).ToList();
                ////使用Cast<dynamic>,可以將object轉型成特殊dynamic類型,直接訪問IList<object)的屬性(列表中元素類型若為object,無法直接被訪問)
                //var DownloadData = ItemThickness.Cast<dynamic>().Select(n => n.mark).ToList();

                ////從MC.Tolerance找到
                //string Unit = MC.Tolerance.Where(n => n.Name == d.ItemName).Select(x => x.DisplayUnit).DefaultIfEmpty("um").First();
                //if (Unit == "mm")
                //{
                //    d.MeasuredValue = ((double.Parse(d.MeasuredValue) * MC.FrontPixelSize) / 1000).ToString("0.00");
                //}
                //else
                //{
                // 相機畫素 1pixel 換算成um
                d.MeasuredValue = ((double.Parse(d.MeasuredValue) * double.Parse(FrontPixelSize)) + Offset).ToString("0.00");
                d.Tolerance = ((double.Parse(d.Tolerance) * double.Parse(FrontPixelSize))).ToString("0.00");
                d.Spec = ((double.Parse(d.Spec) * double.Parse(FrontPixelSize)) + Offset).ToString("0.00");
                //}


            }
            return Data;
        }


    }

}

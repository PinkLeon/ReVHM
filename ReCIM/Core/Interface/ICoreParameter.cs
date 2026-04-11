using Core.API;
using Core.Implementation;
using Core.Model;
using HalconDotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Interface
{
    /// <summary>
    /// 這個介面有啥好處? 多客戶 / 多版本可切換
    /// </summary>
    public interface ICoreParameter
    {

        public IniManager ini { get; set; }
        public ConcurrentQueue<Tuple<string, string, string>> LogStack { get; set; }
        public bool isProcessingLogs { get; set; }
        public object lockObject { get; set; }
        public List<HObject> FrontMaxDefectImages { get; set; }
        public List<HObject> FrontMinDefectImages { get; set; }
        public List<HObject> BackMaxDefectImages { get; set; }
        public List<HObject> BackMinDefectImages { get; set; }

        public BLControl bLControl { get; set; }
        /// <summary>
        /// 確認是否要開啟入料模擬
        /// </summary>
        public bool Simulation { get; set; }
        //public bool LogBusy { get; set; }
        public int ReworkNumber { get; set; }
        public int PieceNumber { get; set; }
        public int DefectNumber { get; set; }

        public bool IsCameraInit { get; set; }
        public bool IsPLCInit { get; set; }
        public bool IsDBInit { get; set; }
        //public  VisionControl FrontCamera, BackCamera;
        public bool FrontFlag { get; set; }
        public bool BackFlag { get; set; }
        //public  AIGO AIFront = new AIGO();
        //public  AIGO AIBack = new AIGO();
        public Thread WatchDogMission { get; set; }
        //新增一枚舉Enum,讓算法按指定的順序跑

        public string ZoomFactor { get; set; }


        public enum AlgorithmOrder
        {

        }

        public List<string> StateTable { get; set; }
        public bool ConnectionStatus { get; set; }
        public FLControl fLControl { get; set; }

        public FrontGenImage fCamera { get; set; }

        public BackGenImage bCamera { get; set; }
        public List<LanguageCore> Languagelist { get; set; }
        public string Lang { get; set; }
        //public  string FrontPixelSize { get; set; } = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Camera", "FrontPixleSize");
        //public  string BackPixelSize = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Camera", "BackCameraPixleSize");
        //public  string SaveAllImage = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Flow", "SaveAllImage");
        //public List<Measurement> InspectionList { get; set; }

        //事件委派
        //public delegate void CIMEventEventHandle(CIMEventArg Data);
        //public  CIMEventEventHandle CIMEvent;
        //光源
        //public  LightControl LightSource = new LightControl();
        //相機ID
        public List<int> CameraID { get; set; }
        //使用者相關訊息
        //public  UserInfo User;
        //曝光時間
        public List<double> ExposureList { get; set; }
        //public  PLCOperation PLCTFrontUse = new PLCOperation(
        //                                          new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLC_IP"),
        //                                          int.Parse(new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLCPort1")));
        //public  PLCOperation PLCTBacktUse = new PLCOperation(
        //                                          new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLC_IP"),
        //                                          int.Parse(new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLCPort2")));
        //public  PLCOperation PLCTWatchDog = new PLCOperation(
        //                                          new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLC_IP"),
        //                                          int.Parse(new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "PLCPort3")));

        MySQLHelper MH { get; set; }


        public enum RunMode
        {

        }


        public Task WriteLog(string type, string log, string message);

        public string ConverToleranceType(int type);
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



        public abstract Task ProcessLogs();


        public abstract Task ExeWriteLog(Tuple<string, string, string> Data);

        public abstract List<DisplayItem> ConvertRealDistance(List<DisplayItem> Data);
    }
}

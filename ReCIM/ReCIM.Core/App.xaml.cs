using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;



namespace ReCIM.Core
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {

        public App()
        {
            // 初始化語言資源 - 必須最先執行，不相依於其他資源
            try
            {
                Global.ini = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini");
                Global.MH = new MySQLHelper(new MysqlActor() { UserID = "root", InitialCatalog = "revhm", IP = "127.0.0.1", Password = "CINPHOWN" });
                //Global.Languagelist = Global.MH.GetALL<LanguageCore>(@"SELECT `key`,`type`,`value` FROM language;").ToList();
                //Global.Alarmlist = Global.MH.GetALL<AlarmCore>("SELECT * FROM alarmlist;").ToList();
                //var dd = Global.Alarmlist;
                //Global.WriteLog("App", "應用程式啟動", "語言資源初始化完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"語言資源初始化失敗: {ex.Message}", "錯誤",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }



        }
        //連接MySQL

        //燈源控制器

        //設密碼

        //全域變數
        public static class Global
        {
            public static readonly ConcurrentQueue<Tuple<string, string, string>> LogStack = new ConcurrentQueue<Tuple<string, string, string>>();
            private static bool isProcessingLogs = false;
            private static readonly object lockObject = new object();
            public static IniManager ini = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini");
            /// <summary>
            /// MySQL資料庫宣告
            /// </summary>
            public static MySQLHelper MH;

            public static List<Measurement> InspectionList = new List<Measurement>()
            { new MissingLine(),
              new FrontDirty()
            };

            public static string ZoomFactor = new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Camera", "ZoomFactor");

            public static bool Simulation = bool.Parse(new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Flow", "Simulation"));

            public static async Task WriteLog(string type, string log, string message)
            {
                LogStack.Enqueue(new Tuple<string, string, string>(type, log, message));

                //啟動處理日誌的線程
                lock (lockObject)
                {
                    if (!isProcessingLogs)
                    {
                        isProcessingLogs = true;
                        Task.Run(async () => await ProcessLogs());
                    }
                }
            }

            private static async Task ProcessLogs()
            {
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

            private static async Task ExeWriteLog(Tuple<string, string, string> Data)
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
            public static void Log(string type, string log, string logMessage, TextWriter w)
            {
                w.WriteLineAsync("[" + type + "]:[" + DateTime.Now.ToString("u") + ":" + DateTime.Now.Millisecond.ToString() + "]:[" + log + logMessage + "]");
                //w.WriteLine("-------------------------------");
                //string sql = string.Format("insert into system_log (type,log,time) values('{0}','{1}','{2}')", type, log + ":" + logMessage, DateTime.Now.ToString("u") + "." + DateTime.Now.Millisecond.ToString());
                //MH.Execute(sql);
            }
        }


    }
}

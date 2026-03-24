using Core;
using Core.API;
using Core.Interface;
using Core.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VHM;
namespace UI
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public readonly ICoreParameter _coreParameter;

        private static List<LanguageCore> Languagelist;

        //Services.AddSingleton<ICoreParameter, CoreParameterService>();
        // 💡 這確保了：CoreParameterService 只會被創建一次！

        public IHost AppHost { get; private set; }   // ← 宣告 AppHost

        public App()
        {
            // 將實例賦值給欄位，以便 App 類別內部使用
            _coreParameter = new CoreParameter();

            // 2. 執行初始化操作，現在可以安全地使用 _coreParameter
            //LoadLanguage();

            Get_Language(new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "language"));


            this.Exit += App_Exit;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            if (_coreParameter.WatchDogMission != null)
            {
                _coreParameter.WatchDogMission.Abort();
                _coreParameter.WatchDogMission = null;
            }
        }

        // 4. 不要忘記啟動 Host 和主視窗 (在 OnStartup 中)
        protected override async void OnStartup(StartupEventArgs e)
        {
            // 3. 建立 Host
            AppHost = Host.CreateDefaultBuilder()
             .ConfigureServices((context, services) =>
             {
                 // **修正 2: 註冊已存在的實例**
                 // 這會將您在步驟 1 創建並初始化的實例註冊為 ICoreParameter 的單例
                 services.AddSingleton<ICoreParameter>(_coreParameter);
                 // 註冊 MainWindow
                 services.AddSingleton<Testification>();
                 // 註冊您的其他服務

             })
             .Build();

            await AppHost.StartAsync();

            //Get_Language(new IniManager(System.AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "language"));


            LoadLanguage();

            var testWindow = AppHost.Services.GetRequiredService<Testification>();
            testWindow.Show();

            base.OnStartup(e);
        }

        private void LoadLanguage()
        {


            ResourceDictionary langRd = null;
            try
            {
                // 假設 _coreParameter.Lang = "zh-TW"
                var langCode = _coreParameter.Lang;
                var uriString = "Lang/" + langCode + ".xaml";

                var uri = new Uri(uriString, UriKind.Relative);

                // **設定中斷點在這裡：** 確保 uri.OriginalString 是 "Lang/zh-TW.xaml"

                langRd = Application.LoadComponent(uri) as ResourceDictionary;
            }
            catch (Exception ex)
            {
                // 將錯誤詳細資訊輸出到一個地方，讓您可以看到
                string errorMessage = "載入語言資源失敗！\n\n";
                errorMessage += $"嘗試載入的程式碼 URI: Lang/{_coreParameter.Lang}.xaml\n";
                errorMessage += $"錯誤來源 Assembly: {ex.Source}\n";
                errorMessage += $"詳細錯誤訊息: {ex.Message}";

                MessageBox.Show(errorMessage);
                // 如果您有日誌系統，建議記錄 log
            }

        }

        public List<LanguageCore> Get_Language(string type)

        {
            string sql = string.Format("SELECT `key`,`type`,`value` FROM language");
            var MyLanguage = _coreParameter.MH.GetALL<LanguageCore>(sql).ToList();
            Languagelist = new List<LanguageCore>();
            foreach (var m in MyLanguage)
            {
                Languagelist.Add((LanguageCore)m);
            }
            return Languagelist;
        }
    }


}

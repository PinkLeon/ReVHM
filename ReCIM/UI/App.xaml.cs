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
        // 1. 這是你的「內部存取點」
        private ICoreParameter _coreParameter;

        // 2. 這是給「外面存取點」（就像 Global 變數）
        // 只要 Host 啟動了，App.Current.Core 就有用
        public static ICoreParameter Core => ((App)Current)._coreParameter;
        private static List<LanguageCore> Languagelist;


        public IHost AppHost { get; private set; }   // ← 宣告 AppHost

        public App()
        {


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

        /// <summary>
        /// 換客戶時只要像下面
        /// </summary>
        /// <param name="e"></param>
        //switch (customer)
        //{
        //    case "KINSUS":
        //        services.AddSingleton<ICoreParameter, CoreParameterKINSUS>();
        //        break;
        //}

        protected override async void OnStartup(StartupEventArgs e)
        {
            // 1. 建立並啟動 Host ,Host 主要負責幫你管理程式的生命週期和資源分配。
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddSingleton<ICoreParameter, CoreParameter>();
                    services.AddSingleton<Testification>();
                }).Build();

            await AppHost.StartAsync();

            // 💡 捨棄反射！直接簡單賦值
            _coreParameter = AppHost.Services.GetRequiredService<ICoreParameter>();

            // 2. 現在 _coreParameter 有值了，可以安全調用
            Get_Language(new IniManager(AppDomain.CurrentDomain.BaseDirectory + @"\Setting.ini").ReadIni("Operation", "language"));
            LoadLanguage();

            //用Testfi時,請DI賦值
            // 3. 顯示視窗
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

using CefSharp.OffScreen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CefSharp.MinimalExample.OffScreen.Notifications;
using CefSharp.MinimalExample.OffScreen.GenerationMnemonic;
using System.Linq;

namespace CefSharp.MinimalExample.OffScreen.LoadDataToURL
{
    public enum Status
    {
        Process,
        Success
    }

    public class LoadDataSolflareWithGeneration
    {
        public string urlToLoad { get; set; }
        public CreateMnemonicPhrase generatorMnemonic { get; set; }
        public Notification notifyUser { get; set; }
        public int countTestMnemonic { get; set; }

        public LoadDataSolflareWithGeneration(string urlToLoad, CreateMnemonicPhrase generatorMnemonic, Notification notifyUser, int countTestMnemonic)
        {
            this.urlToLoad = urlToLoad;
            this.generatorMnemonic = generatorMnemonic;
            this.notifyUser = notifyUser;
            this.countTestMnemonic = countTestMnemonic;
        }

        /// <summary>
        /// Тестирование мнемонической фразы
        /// </summary>
        /// <returns></returns>
        public Status TestingPhrase()
        {
            return TestingPhrase(urlToLoad, generatorMnemonic, notifyUser, countTestMnemonic);
        }
        public Status TestingPhrase(string urlToLoad, CreateMnemonicPhrase generatorMnemonic, Notification notifyUser, int countTestMnemonic)
        {
            // Статус функции
            Status status = Status.Process;

            AsyncContext.Run(async delegate
            {
                var settings = new CefSettings()
                {
                    //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                    CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),
                };

                //Perform dependency check to make sure all relevant resources are in our output directory.
                var success = await Cef.InitializeAsync(settings, performDependencyCheck: true, browserProcessHandler: null);

                if (!success)
                {
                    throw new Exception("Unable to initialize CEF, check the log file.");
                }

                // Create the CefSharp.OffScreen.ChromiumWebBrowser instance
                var browser = new ChromiumWebBrowser(urlToLoad);

                var initialLoadResponse = await browser.WaitForInitialLoadAsync();

                if (!initialLoadResponse.Success)
                {
                    throw new Exception(string.Format("Page load failed with ErrorCode:{0}, HttpStatusCode:{1}", initialLoadResponse.ErrorCode, initialLoadResponse.HttpStatusCode));
                }


                // Проверка возможности заполнения страницы
                Task<JavascriptResponse> setValue = browser.EvaluateScriptAsync($@"var el = document.getElementById('mnemonic-input-0');
                                                                el.focus();
                                                                document.execCommand('insertText', false, '');" +
                                                             @"el.dispatchEvent(new Event('change', {bubbles: true}));");
                await setValue;

                while (!setValue.Result.Success)
                {
                    setValue = browser.EvaluateScriptAsync($@"var el = document.getElementById('mnemonic-input-0');
                                                                el.focus();
                                                                document.execCommand('insertText', false, '');" +
                                                            @"el.dispatchEvent(new Event('change', {bubbles: true}));");
                    await setValue;
                }
                Console.WriteLine("Form is ready to fill.");

                // Работа с формой
                for (int countTest = 0; countTest < countTestMnemonic; countTest++)
                {
                    // Генерируем мнемонику
                    List<string> mnemonicPhrase = generatorMnemonic.GenerateMnemonicPhrase();

                    Task<bool> fillForm = FillUrlData(browser, mnemonicPhrase);
                    await fillForm;

                    // Проверка мнемонической фразы
                    bool checkMnemonicPhraseInUrl = fillForm.Result;

                    // Уведомляем пользователя, если мнемоника правильная
                    if (checkMnemonicPhraseInUrl)
                    {
                        string message = string.Concat(mnemonicPhrase.Select(x => x + " "));

                        notifyUser.ExecuteNotify(message + $@" -- Success");
                    }
                }
            });

            // Тесты мнемоник завершены
            status = Status.Success;

            return status;
        }

        /// <summary>
        /// Создание ихображения для нужд тестирования
        /// </summary>
        /// <param name="browser"></param>
        private async void CreateImageBrowser(ChromiumWebBrowser browser)
        {
            /// Создаем изображение
            // Wait for the screenshot to be taken.
            var bitmapAsByteArray = await browser.CaptureScreenshotAsync();
            // File path to save our screenshot e.g. C:\Users\{username}\Desktop\CefSharp screenshot.png
            var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

            File.WriteAllBytes(screenshotPath, bitmapAsByteArray);

            // Tell Windows to launch the saved image.
            Process.Start(new ProcessStartInfo(screenshotPath)
            {
                // UseShellExecute is false by default on .NET Core.
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Заполнение формы данными
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="mnemonicPhrase"></param>
        /// <returns></returns>
        private async Task<bool> FillUrlData(ChromiumWebBrowser browser, List<string> mnemonicPhrase)
        {
            // Проверка правильности мнемонической фразы
            bool checkMnemonicPhraseInUrl = false;

            // Заполняем поля формы
            int maxMnemonicInput = mnemonicPhrase.Count;
            for (int mnemonicInput = 0; mnemonicInput < maxMnemonicInput; mnemonicInput++)
            {
                Task<JavascriptResponse> setValue = browser.EvaluateScriptAsync($@"var el = document.getElementById('mnemonic-input-{mnemonicInput}');
                                                                el.focus();
                                                                document.execCommand('insertText', false, '{mnemonicPhrase[mnemonicInput]}');" +
                                                        @"el.dispatchEvent(new Event('change', {bubbles: true}));");
                await setValue;
            }

            // Нажимаем кнопку продолжить
            string continueButtonClassName = @"MuiButtonBase-root MuiButton-root MuiButton-contained MuiButton-containedPrimary MuiButton-sizeMedium MuiButton-containedSizeMedium  css-1hcgjm";
            browser.ExecuteScriptAsync(
            $@"var bnt = document.getElementsByClassName('{continueButtonClassName}')[0];
                        bnt.click();");


            // Проверяем наличие ошибки и возвращаемся назад, если ошибки нет
            // var errorCheck = browser.EvaluateScriptAsync("document.getElementsByClassName('MuiFormHelperText-root Mui-error css-11a179u')[0];");
            Task<JavascriptResponse> errorCheck = browser.EvaluateScriptAsync($"var el = document.getElementsByClassName('MuiFormHelperText-root Mui-error css-11a179u')[0];" +
                $"el.focus();");
            await errorCheck;
            if (!errorCheck.Result.Success)
            {
                checkMnemonicPhraseInUrl = true;

                // Возвращаемся назад
                string backButtonClassName = @"MuiButtonBase-root MuiButton-root MuiButton-text MuiButton-textPrimary MuiButton-sizeMedium MuiButton-textSizeMedium  css-1rryh4p";
                browser.ExecuteScriptAsync(
                $@"var bnt = document.getElementsByClassName('{backButtonClassName}')[0];
                        bnt.click();");
            }
            // Очищаем формы
            maxMnemonicInput = mnemonicPhrase.Count;
            for (int mnemonicInput = 0; mnemonicInput < maxMnemonicInput; mnemonicInput++)
            {
                Task<JavascriptResponse> setValue = browser.EvaluateScriptAsync($@"var el = document.getElementById('mnemonic-input-{mnemonicInput}');
                                                            el.focus();
                                                            document.execCommand('insertText', false, '');" +
                                                        @"el.dispatchEvent(new Event('change', {bubbles: true}));");
                await setValue;
            }
            Console.WriteLine($"Mnemonic phrase is incorrect -- {string.Concat(mnemonicPhrase.Select(x => x + " "))}");

            return checkMnemonicPhraseInUrl;
        }
    }
}

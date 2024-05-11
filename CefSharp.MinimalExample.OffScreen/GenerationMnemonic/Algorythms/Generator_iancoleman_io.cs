using System;
using CefSharp.OffScreen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace CefSharp.MinimalExample.OffScreen.GenerationMnemonic.Algorythms
{
    public class Generator_iancoleman_io
    {
        public string urlToLoad { get; set; }

        public Generator_iancoleman_io()
        {
            this.urlToLoad = @"https://iancoleman.io/bip39/#english";
        }

        /// <summary>
        /// Тестирование мнемонической фразы
        /// </summary>
        /// <returns></returns>
        public List<string> GetPhrase(int countWord)
        {
            List<string> mnemonicPhrase = new List<string>();

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
                await Task.Delay(500);

                // Ставим нужное количество слов
                Task<JavascriptResponse> setCountWord = browser.EvaluateScriptAsync($@"var countWord = document.getElementById('strength').value = {countWord};");
                await setCountWord;


                // Нажимаем кнопку получения мнемоники
                Task<JavascriptResponse> pressGenerate = browser.EvaluateScriptAsync($@"var buttonGenerate = document.getElementsByTagName('button')[0];
                                                                                        buttonGenerate.focus();
                                                                                        buttonGenerate.click();");
                await pressGenerate;

                await Task.Delay(100);

                // Получаем фразу
                Task<JavascriptResponse> getMnemonicPhrase = browser.EvaluateScriptAsync($@"function foo(){{var phrase = document.getElementById('phrase');
                                                                                            return phrase.value;}}
                                                                                            foo();");

                await getMnemonicPhrase;

                mnemonicPhrase = (getMnemonicPhrase.Result.Result as string).Split().ToList();
            });
            return mnemonicPhrase;    
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
    }
}

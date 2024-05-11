using CefSharp.OffScreen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CefSharp.MinimalExample.OffScreen.Notifications;
using CefSharp.MinimalExample.OffScreen.GenerationMnemonic;
using System.Linq;
using CefSharp.MinimalExample.OffScreen.GenerationMnemonic.Algorythms;
using CefSharp.Structs;

namespace CefSharp.MinimalExample.OffScreen.LoadDataToURL
{
    public class SolFlareTestMnemonic
    {
        public string urlToLoad { get; set; }
        public Generator_iancoleman_io generatorMnemonic { get; set; }
        public Notification notifyUser { get; set; }
        public int countTestMnemonic { get; set; }

        public SolFlareTestMnemonic(string urlToLoad, Generator_iancoleman_io generatorMnemonic, Notification notifyUser, int countTestMnemonic)
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
        public Status TestingPhrase(string urlToLoad, Generator_iancoleman_io generatorMnemonic, Notification notifyUser, int countTestMnemonic)
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


                string buttonHasAlreadyWallet = @"MuiButtonBase-root MuiButton-root MuiButton-contained MuiButton-containedPrimary MuiButton-sizeMedium MuiButton-containedSizeMedium  css-1hcgjm";
                // Проверка возможности заполнения страницы
                Task<JavascriptResponse> setValue = browser.EvaluateScriptAsync($@"document.getElementsByClassName('{buttonHasAlreadyWallet}')[1].click();");
                await setValue;

                // while (!setValue.Result.Success)
                // {
                //     setValue = browser.EvaluateScriptAsync($@"document.getElementsByClassName('{buttonHasAlreadyWallet}')[1].click();");
                //     await setValue;
                // }

                await Task.Delay(45000);

                await CreateImageBrowser(browser);

                for (int test = 0; test < countTestMnemonic; test++)
                {

                    // Нажимаем на кнопку "У меня уже есть кошелек"
                    setValue = browser.EvaluateScriptAsync($@"document.getElementsByClassName('{buttonHasAlreadyWallet}')[1].click();");
                    await setValue;

                    // Генерируем мнемонику
                    int countWord = 12;
                    List<string> mnemonicPhrase = generatorMnemonic.GetPhrase(countWord);

                    // Заполняем форму 
                    Task<bool> fillForm = FillUrlData(browser, mnemonicPhrase);
                    await fillForm;

                    await CreateImageBrowser(browser);

                    // Проверка мнемонической фразы
                    bool checkMnemonicPhraseInUrl = fillForm.Result;

                    // Уведомляем пользователя, если мнемоника правильная
                    string message = "";
                    if (checkMnemonicPhraseInUrl)
                    {
                        message = string.Concat(mnemonicPhrase.Select(x => x + " ")) + $@" -- Success";
                    }
                    else
                    {
                        message = string.Concat(mnemonicPhrase.Select(x => x + " ")) + $@" -- FAIL";
                    }
                    notifyUser.ExecuteNotify(message);
                }
            });

            // Тесты мнемоник завершены
            status = Status.Success;

            return status;

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
            Task<JavascriptResponse> pushContinueButton = browser.EvaluateScriptAsync(
                                                            $@"var bnt = document.getElementsByClassName('{continueButtonClassName}')[0];
                                                                        bnt.click();");
            await pushContinueButton;

            await Task.Delay(100);

            // Устанавливаем пароль
            string idNewPasswordInput = @"password";
            Task<JavascriptResponse> setPassword = browser.EvaluateScriptAsync($@"var el = document.getElementsByName('{idNewPasswordInput}')[0];
                                                                el.focus();
                                                                document.execCommand('insertText', false, '01051998');" +
                                                                @"el.dispatchEvent(new Event('change', {bubbles: true}));");
            await setPassword;

            string idNewPasswordInputRepeat = @"password2";
            Task<JavascriptResponse> setPasswordRepeat = browser.EvaluateScriptAsync($@"var el = document.getElementsByName('{idNewPasswordInputRepeat}')[0];
                                                                el.focus();
                                                                document.execCommand('insertText', false, '01051998');" +
                                                                @"el.dispatchEvent(new Event('change', {bubbles: true}));");
            await setPasswordRepeat;

            // Нажимаем кнопку продолжить
            pushContinueButton = browser.EvaluateScriptAsync(
            $@"var bnt = document.getElementsByClassName('{continueButtonClassName}')[0];
                        bnt.click();");
            await pushContinueButton;

            await Task.Delay(5000);

            // Получить информацию о количестве найденных кошельков
            string classNameInfo = @"MuiTypography-root MuiTypography-h2 MuiTypography-gutterBottom css-1tc4wys";
            Task<JavascriptResponse> getInfo = browser.EvaluateScriptAsync(@"document.getElementsByTagName('h2').item(0).innerHTML;");
            await getInfo;


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


            string searchText = "No Active Wallets Found";

            string resultCheck = getInfo.Result.Result as string;
            if (!resultCheck.Contains(searchText))
            {
                checkMnemonicPhraseInUrl = true;
            }

            // Возвращаемся назад
            browser.Back();
            await Task.Delay(500);

            return checkMnemonicPhraseInUrl;
        }


        /// <summary>
        /// Создание ихображения для нужд тестирования
        /// </summary>
        /// <param name="browser"></param>
        private async Task CreateImageBrowser(ChromiumWebBrowser browser)
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

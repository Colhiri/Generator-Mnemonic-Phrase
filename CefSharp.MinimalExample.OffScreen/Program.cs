// Copyright © 2010-2021 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using CefSharp.OffScreen;
using CefSharp.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CefSharp.MinimalExample.OffScreen
{
    public enum WordListLanguage
    {
        ChineseSimplified,
        ChineseTraditional,
        English,
        French,
        Italian,
        Japanese,
        Korean,
        Spanish
    }

    /// <summary>
    /// CefSharp.OffScreen Minimal Example
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Asynchronous demo using CefSharp.OffScreen
        /// Loads google.com, uses javascript to fill out the search box then takes a screenshot which is opened
        /// in the default image viewer.
        /// For a synchronous demo see <see cref="MainSync(string[])"/> below.
        /// </summary>
        /// <param name="args">args</param>
        /// <returns>exit code</returns>
        public static int Main(string[] args)
        {
            // Ссылка на сайт, где будут проверяться ключи
            const string testUrl = "https://solflare.com/onboard/access";

            // Получение seed-фразы
            List<string> mnemonic = new List<string>() { "1", "2", "3", "1", "2", "3", "1", "2", "3", "1", "2", "3" };

            // 
            AsyncContext.Run(async delegate
            {
                var settings = new CefSettings()
                {
                    //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                    CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
                };

                //Perform dependency check to make sure all relevant resources are in our output directory.
                var success = await Cef.InitializeAsync(settings, performDependencyCheck: true, browserProcessHandler: null);

                if (!success)
                {
                    throw new Exception("Unable to initialize CEF, check the log file.");
                }

                // Create the CefSharp.OffScreen.ChromiumWebBrowser instance
                using (var browser = new ChromiumWebBrowser(testUrl))
                {
                    var initialLoadResponse = await browser.WaitForInitialLoadAsync();

                    if (!initialLoadResponse.Success)
                    {
                        throw new Exception(string.Format("Page load failed with ErrorCode:{0}, HttpStatusCode:{1}", initialLoadResponse.ErrorCode, initialLoadResponse.HttpStatusCode));
                    }

                    var setValue = browser.EvaluateScriptAsync("document.getElementById('mnemonic-input-0').value ='hute'");

                    await setValue;

                    var sourcePage = browser.GetSourceAsync();

                    while (!setValue.Result.Success)
                    {
                        setValue = browser.EvaluateScriptAsync("document.getElementById('mnemonic-input-0').value ='hute'");

                        await setValue;

                        await Task.Delay(500);

                        if (setValue.Result.Success)
                        {
                            int maxMnemonicInput = 12;
                            for (int mnemonicInput = 0; mnemonicInput < maxMnemonicInput; mnemonicInput++)
                            {
                                setValue = browser.EvaluateScriptAsync($"document.getElementById('mnemonic-input-{mnemonicInput}').value ='{mnemonic[mnemonicInput]}'");

                                await setValue;
                            }
                        }
                    }

                    await sourcePage;

                    string result = sourcePage.Result;

                    // Нажимаем кнопку продолжить
                    var pushButton = browser.EvaluateScriptAsync("document.getElementByClassName('MuiButtonBase-root MuiButton-root MuiButton-contained MuiButton-containedPrimary MuiButton-sizeMedium MuiButton-containedSizeMedium  css-1hcgjm')[0].click();");
                    await pushButton;

                    // Проверяем наличие ошибки
                    var errorCheck = browser.EvaluateScriptAsync("document.getElementByClassName('MuiFormHelperText-root Mui-error css-11a179u')[1];");
                    await errorCheck;

                    //Give the browser a little time to render
                    await Task.Delay(500);
                    // Wait for the screenshot to be taken.
                    var bitmapAsByteArray = await browser.CaptureScreenshotAsync();

                    // File path to save our screenshot e.g. C:\Users\{username}\Desktop\CefSharp screenshot.png
                    var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

                    Console.WriteLine();
                    Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

                    File.WriteAllBytes(screenshotPath, bitmapAsByteArray);

                    Console.WriteLine("Screenshot saved. Launching your default image viewer...");

                    // Tell Windows to launch the saved image.
                    Process.Start(new ProcessStartInfo(screenshotPath)
                    {
                        // UseShellExecute is false by default on .NET Core.
                        UseShellExecute = true
                    });

                    Console.WriteLine("Image viewer launched. Press any key to exit.");
                }

                // Wait for user to press a key before exit
                Console.ReadKey();

                // Clean up Chromium objects. You need to call this in your application otherwise
                // you will get a crash when closing.
                Cef.Shutdown();
            });

            return 0;
        }
    }
}

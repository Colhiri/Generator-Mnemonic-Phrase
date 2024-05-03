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
        private static int words = 12;
        private static string[] _english = GetWordList(WordListLanguage.English);
        private static string[] lang = _english;
        private static WordListLanguage Language_list = WordListLanguage.English;


        private static string[] GetWordList(WordListLanguage wordList)
        {
            string path = $"english.txt";
            string textInFile = File.ReadAllText(path);
            string[] words = textInFile.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            return words;
        }

        private static string BytesToBinary(byte[] hash)
        {
            return string.Join("", hash.Select(h => LeftPad(Convert.ToString(h, 2), "0", 8)));
        }

        private static string LeftPad(string str, string padString, int length)
        {
            while (str.Length < length)
            {
                str = padString + str;
            }

            return str;
        }

        private static string DeriveChecksumBits(byte[] checksum)
        {
            var ent = checksum.Length * 8;
            var cs = ent / 32;

            var sha256Provider = new SHA256CryptoServiceProvider();
            var hash = sha256Provider.ComputeHash(checksum);
            var result = BytesToBinary(hash);
            return result.Substring(0, cs);
        }

        public static string EntropyToMnemonic(byte[] entropyBytes, string[] wordList, WordListLanguage wordListType)
        {
            var entropyBits = BytesToBinary(entropyBytes);
            var checksumBits = DeriveChecksumBits(entropyBytes);

            var bits = entropyBits + checksumBits;

            var chunks = Regex.Matches(bits, "(.{1,11})")
                .OfType<Match>()
                .Select(m => m.Groups[0].Value)
                .ToArray();

            var words = chunks.Select(binary =>
            {
                var index = Convert.ToInt32(binary, 2);
                return wordList[index];
            });

            var joinedText = string.Join((wordListType == WordListLanguage.Japanese ? "\u3000" : " "), words);

            return joinedText;
        }

        public static byte[] GenerateMnemonicBytes(int strength)
        {
            var rngCryptoServiceProvider = new RNGCryptoServiceProvider();

            var buffer = new byte[strength / 8];
            rngCryptoServiceProvider.GetBytes(buffer);

            return buffer;
        }
        public static List<string> Entropy(int lenght)
        {
            // byte[] bytes = new byte[lenght];
            // RandomNumberGenerator random = RandomNumberGenerator.Create();
            // RandomNumberGenerator.Fill(bytes);
            // random.GetBytes(bytes);
            // int max = bytes.Max();
            List<string> mnemonic = new List<string>();

            for (int countWord = 0; countWord < words; countWord++)
            {
                byte[] seedBytes = GenerateMnemonicBytes(words);
                string seed = EntropyToMnemonic(seedBytes, lang, Language_list);
                mnemonic.Add(seed);
            }

            return mnemonic;
        }

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
            const string testUrl = "https://solflare.com/onboard/access";

            Console.WriteLine("This example application will load {0}, take a screenshot, and save it to your desktop.", testUrl);
            Console.WriteLine("You may see Chromium debugging output, please wait...");
            Console.WriteLine();

            List<string> mnemonic = Entropy(128);

            //Console apps don't have a SynchronizationContext, so to ensure our await calls continue on the main thread we use a super simple implementation from
            //https://devblogs.microsoft.com/pfxteam/await-synchronizationcontext-and-console-apps/
            //Continuations will happen on the main thread. Cef.Initialize/Cef.Shutdown must be called on the same Thread.
            //The Nito.AsyncEx.Context Nuget package has a more advanced implementation
            //should you wish to use a pre-build implementation.
            //https://github.com/StephenCleary/AsyncEx/blob/8a73d0467d40ca41f9f9cf827c7a35702243abb8/doc/AsyncContext.md#console-example-using-asynccontext
            //NOTE: This is only required if you use await

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

        /// <summary>
        /// Synchronous demo using CefSharp.OffScreen
        /// Loads google.com, uses javascript to fill out the search box then takes a screenshot which is opened
        /// in the default image viewer.
        /// For a asynchronous demo see <see cref="Main(string[])"/> above.
        /// To use this demo simply delete the <see cref="Main(string[])"/> method and rename this method to Main.
        /// </summary>
        /// <param name="args">args</param>
        /// <returns>exit code</returns>
        public static int MainSync(string[] args)
        {
#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif

            const string testUrl = "https://www.google.com/";

            Console.WriteLine("This example application will load {0}, take a screenshot, and save it to your desktop.", testUrl);
            Console.WriteLine("You may see Chromium debugging output, please wait...");
            Console.WriteLine();

            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };

            //Perform dependency check to make sure all relevant resources are in our output directory.
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            // Create the offscreen Chromium browser.
            var browser = new ChromiumWebBrowser(testUrl);

            EventHandler<LoadingStateChangedEventArgs> handler = null;

            handler = (s, e) =>
            {
                // Check to see if loading is complete - this event is called twice, one when loading starts
                // second time when it's finished
                if (!e.IsLoading)
                {
                    // Remove the load event handler, because we only want one snapshot of the page.
                    browser.LoadingStateChanged -= handler;

                    var scriptTask = browser.EvaluateScriptAsync("document.querySelector('[name=q]').value = 'CefSharp Was Here!'");



                    scriptTask.ContinueWith(t =>
                    {
                        if(!t.Result.Success)
                        {
                            throw new Exception("EvaluateScriptAsync failed:" + t.Result.Message);
                        }

                        //Give the browser a little time to render
                        Thread.Sleep(500);
                        // Wait for the screenshot to be taken.
                        var task = browser.CaptureScreenshotAsync();
                        task.ContinueWith(x =>
                        {
                            // File path to save our screenshot e.g. C:\Users\{username}\Desktop\CefSharp screenshot.png
                            var screenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CefSharp screenshot.png");

                            Console.WriteLine();
                            Console.WriteLine("Screenshot ready. Saving to {0}", screenshotPath);

                            var bitmapAsByteArray = x.Result;

                            // Save the Bitmap to the path.
                            File.WriteAllBytes(screenshotPath, bitmapAsByteArray);

                            Console.WriteLine("Screenshot saved.  Launching your default image viewer...");

                            // Tell Windows to launch the saved image.
                            Process.Start(new ProcessStartInfo(screenshotPath)
                            {
                                // UseShellExecute is false by default on .NET Core.
                                UseShellExecute = true
                            });

                            Console.WriteLine("Image viewer launched.  Press any key to exit.");
                        }, TaskScheduler.Default);
                    });
                }
            };

            // An event that is fired when the first page is finished loading.
            // This returns to us from another thread.
            browser.LoadingStateChanged += handler;



            // We have to wait for something, otherwise the process will exit too soon.
            Console.ReadKey();

            // Clean up Chromium objects. You need to call this in your application otherwise
            // you will get a crash when closing.
            //The ChromiumWebBrowser instance will be disposed
            Cef.Shutdown();

            return 0;
        }
    }
}

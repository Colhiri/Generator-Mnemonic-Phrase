// Copyright © 2010-2021 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Threading;
using CefSharp.MinimalExample.OffScreen.GenerationMnemonic;
using CefSharp.MinimalExample.OffScreen.GenerationMnemonic.Algorythms;
using CefSharp.MinimalExample.OffScreen.LoadDataToURL;
using CefSharp.MinimalExample.OffScreen.Notifications;

namespace CefSharp.MinimalExample.OffScreen
{
    /// <summary>
    /// CefSharp.OffScreen Minimal Example
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Asynchronous demo using CefSharp.OffScreen
        /// </summary>
        /// <param name="args">args</param>
        /// <returns>exit code</returns>
        public static void Main(string[] args)
        {
            // Автоматическое создание
            var algorythmGPRN = new PseudoRandomGenerationByte();
            WordListLanguage language = WordListLanguage.English;
            int countByteEntropy = 128;
            int countWord = 12;
            // Создаем генератор мнемонической фразы
            var generation1 = new Generator_iancoleman_io();
            var generation2 = new Generator_iancoleman_io();
            var generation3 = new Generator_iancoleman_io();
            // var generation4 = new Generator_iancoleman_io();

            // Создаем генерационную модель
            Notification notify1 = new Notification("phrases_1.txt");
            Notification notify2 = new Notification("phrases_2.txt");
            Notification notify3 = new Notification("phrases_3.txt");
            // Notification notify4 = new Notification("phrases_4.txt");

            // Создаем новую страницу solflare через Cef
            string url = @$"https://solflare.com/onboard";
            // LoadDataSolflareWithGeneration loadData = new LoadDataSolflareWithGeneration(url, generation, notify, 1000000000);
            SolFlareTestMnemonic testMnemonic1 = new SolFlareTestMnemonic(url, generation1, notify1, 1000000);
            SolFlareTestMnemonic testMnemonic2 = new SolFlareTestMnemonic(url, generation2, notify2, 1000000);
            SolFlareTestMnemonic testMnemonic3 = new SolFlareTestMnemonic(url, generation3, notify3, 1000000);
            // SolFlareTestMnemonic testMnemonic4 = new SolFlareTestMnemonic(url, generation4, notify4, 1000000);

            // Тестируем созданную мнемоническую фразу на сайте
            // testMnemonic.TestingPhrase();
            // testMnemonic2.TestingPhrase();


            Thread task1 = new Thread(new ThreadStart(testMnemonic1.TestingPhrase));
            Thread task2 = new Thread(new ThreadStart(testMnemonic2.TestingPhrase));
            Thread task3 = new Thread(new ThreadStart(testMnemonic3.TestingPhrase));
            // Thread task4 = new Thread(new ThreadStart(testMnemonic4.TestingPhrase));

            task1.Start();
            task2.Start();
            task3.Start();
            // task4.Start();


        }
    }
}

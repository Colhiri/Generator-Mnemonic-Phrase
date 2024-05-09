// Copyright © 2010-2021 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using CefSharp.MinimalExample.OffScreen.GenerationMnemonic;
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
        /// Loads google.com, uses javascript to fill out the search box then takes a screenshot which is opened
        /// in the default image viewer.
        /// For a synchronous demo see <see cref="MainSync(string[])"/> below.
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
            var generation = new CreateMnemonicPhrase(language, countByteEntropy, countWord, algorythmGPRN);
            // Создаем генерационную модель
            Notification notify = new Notification();

            /*
            // Ручное создание
            // Создаем алгоритм
            var algorythmGPRN = new PseudoRandomGenerationByte();
            // Создаем генератор мнемонической фразы
            var generation = new CreateMnemonicPhrase(GenerationMnemonic.WordListLanguage.English, 128, 12, algorythmGPRN);
            // Задаем параметры ручной генерации
            int countByteEntropy = 128;
            int countWord = 12;
            // Получение заданное количество байт энтропии
            List<int> entropyBytes = new List<int>();
            generation.GetEntropyBytes(entropyBytes, countByteEntropy);
            // Считаем контрольную сумму
            byte[] controlSum = generation.GetControlSum(entropyBytes);
            // Считаем добавочные байты для добавления в байты энтропии для разделения всех байтов на слова по 11 байтов в каждом
            string addingBytes = generation.GetAddingBytes(controlSum, entropyBytes.Count());
            // Трансформируем их из строки в массив
            List<int> transformAddingBytesToInt = addingBytes.ToCharArray().Select(x => int.Parse(x.ToString())).ToList();
            entropyBytes.AddRange(transformAddingBytesToInt);
            // Разделяем байты по 11 элементов
            List<List<int>> bytesEachWordBinary = new List<List<int>>();
            generation.GetBytesEachWordBinary(bytesEachWordBinary, entropyBytes, countWord);
            // Преобразуем байты из двоичной системы в десятичную
            List<int> bytesEachWordDecimal = new List<int>();
            generation.GetBytesEachWordDecimal(bytesEachWordDecimal, bytesEachWordBinary);
            // Получаем мнемоническую фразу
            string path = $"WordsForSeedPhrase/{generation.languageMnemonicPhrase}.txt";
            List<string> wordsBIP39 = generation.GetBIP39(path);
            List<string> mnemonicPhrase = generation.GetMnemonicPhrase(bytesEachWordDecimal, wordsBIP39);
            */

            // Создаем новую страницу solflare через Cef
            string url = @$"https://solflare.com/onboard/access";
            LoadData loadData = new LoadData(url, generation, notify, 100000);

            // Тестируем созданную мнемоническую фразу на сайте
            Status checkRightMnemonicPhrase = loadData.TestingPhrase();

            Console.Write(checkRightMnemonicPhrase);

            Console.ReadKey();

        }
    }
}

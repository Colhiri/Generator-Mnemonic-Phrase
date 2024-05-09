using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.IO;

namespace CefSharp.MinimalExample.OffScreen.GenerationMnemonic
{
    /// <summary>
    /// Базовый класс генерации мнемонической фразы
    /// </summary>
    public class CreateMnemonicPhrase
    {
        // Язык мнемонической фразы
        public WordListLanguage languageMnemonicPhrase { get; set; }
        // Количество бит энтропии, если число не кратно 32 (битам) то оно неверно задано
        private int _countEntropyByte;
        public int countEntropyBytes 
        {
            get { return _countEntropyByte; }
            set 
            {
                if (!(value % 32 == 0)) 
                {
                    throw new ArgumentException("Количество бит энтропии не кратно 32. Необходимо изменить значение.");
                }
                _countEntropyByte = value;
            }
        }
        private string _addingCountByteInTail;
        public string addingCountByteInTail
        {
            get { return _addingCountByteInTail; }
            set { _addingCountByteInTail = value; }
        }
        // Количество слов в мнемонической фразе
        private int _countWords;
        public int countWords 
        { 
            get { return _countWords; }
            set { _countWords = value; } 
        }
        // Начальная энтропия
        public List<int> entropyBytes { get; set; }
        // Разделение на количество слов
        public List<List<int>> bytesEachWordBinary { get; set; }
        // Перевод байтов слов из двоичной системы исчисления в десятичную
        public List<int> bytesEachWordDecimal { get; set; }
        // Слова в выбранного языке по спецификации BIP39
        public List<string> wordsBIP39 { get; set; }
        // Алгоритм 
        private IGetEntropy entropyAlgorythm { get; set; }
        // Контрольная сумма
        private byte[] _controlSum;
        public byte[] controlSum 
        {
            get { return _controlSum; }
            set { _controlSum = value; } 
        }
        // Мнемоническая фраза
        public List<string> mnemonicPhrase { get; set; }

        /// <summary>
        /// В конструкторе последовательная инициализация. Сначала проверяется количество байт, после количество слов.
        /// </summary>
        /// <param name="languageMnemonicPhrase"></param>
        /// <param name="countEntropyByte"></param>
        /// <param name="countWords"></param>
        /// <param name="entropyAlgorythm"></param>
        public CreateMnemonicPhrase(WordListLanguage languageMnemonicPhrase, int countEntropyByte, int countWords, IGetEntropy entropyAlgorythm)
        {
            this.languageMnemonicPhrase = languageMnemonicPhrase;
            this.countEntropyBytes = countEntropyByte;
            this.countWords = countWords;
            this.entropyAlgorythm = entropyAlgorythm;

            entropyBytes = new List<int>();
            bytesEachWordBinary = new List<List<int>>();
            bytesEachWordDecimal = new List<int>();
            wordsBIP39 = new List<string>();
            mnemonicPhrase = new List<string>();
        }

        /// <summary>
        /// Генеририрует мнемоническую фразу по заданным полям
        /// </summary>
        /// <returns></returns>
        public List<string> GenerateMnemonicPhrase()
        {
            entropyBytes = new List<int>();
            bytesEachWordBinary = new List<List<int>>();
            bytesEachWordDecimal = new List<int>();
            wordsBIP39 = new List<string>();
            mnemonicPhrase = new List<string>();

            GetEntropyBytes();
            GetControlSum();
            GetAddingBytes();
            GetBytesEachWordBinary();
            GetBytesEachWordDecimal();
            GetBIP39();
            GetMnemonicPhrase();
            return mnemonicPhrase;
        }


        /// <summary>
        /// Заполнить массив байтов энтропии в классе
        /// </summary>
        private void GetEntropyBytes()
        {
            GetEntropyBytes(entropyBytes, countEntropyBytes);
        }
        /// <summary>
        /// Заполнить ЛЮБОЙ массив байтов энтропии
        /// </summary>
        /// <param name="countEntropyBytes"></param>
        /// <returns></returns>
        public void GetEntropyBytes(List<int> _entropyBytes, int countEntropyBytes)
        {
            entropyAlgorythm.ExecuteGetEntropyBytes(_entropyBytes, countEntropyBytes);
        }

        /// <summary>
        /// Найти контрольную сумму исходя из массива байтов в классе
        /// </summary>
        private void GetControlSum()
        {
            controlSum = GetControlSum(entropyBytes);
        }
        /// <summary>
        /// Найти ЛЮБУЮ контрольную сумму исходя из массива байтов
        /// </summary>
        public byte[] GetControlSum(List<int> _entropyBytes)
        {
            // Преобразование байтов энтропии в нужный для функции формат
            byte[] bytesToSHA256 = _entropyBytes.Select(x => (byte)x).ToArray();

            var sha256Provider = new SHA256CryptoServiceProvider();
            byte[] hash = sha256Provider.ComputeHash(bytesToSHA256);
            return hash;
        }

        /// <summary>
        /// Получить N байтов из контрольной суммы, исходя из ее количества
        /// </summary>
        private void GetAddingBytes()
        {
            addingCountByteInTail = GetAddingBytes(controlSum, entropyBytes.Count());
        }
        public string GetAddingBytes(byte[] controlSum, int countBytesEntropy)
        {
            int lenghtControlSum = controlSum.Length;

            // Получаем количество байтов, которые нужно добавить к изначальной энтропии
            int countAddingBytes = (int)(countBytesEntropy / 32);

            // Переводим контрольную сумму в последовательность байт
            List<string> bytes = controlSum.Select(x => Convert.ToString(x, 2)).ToList();
            string controlSumBinary = string.Concat(bytes);

            return controlSumBinary[0..countAddingBytes];
        }

        /// <summary>
        /// Заполнить массив слов из байтов в классе
        /// </summary>
        /// <returns></returns>
        private void GetBytesEachWordBinary()
        {
            GetBytesEachWordBinary(bytesEachWordBinary, entropyBytes, countWords);
        }
        /// <summary>
        /// Заполнить ЛЮБОЙ массив слов из байтов
        /// </summary>
        /// <returns></returns>
        public void GetBytesEachWordBinary(List<List<int>> bytesEachWordBinary, List<int> entropyBytes, int countWords)
        {
            int countByteInOneWord = (int)Math.Round((double)entropyBytes.Count / (double)countWords, 0, MidpointRounding.ToPositiveInfinity);
            for (int wordIndex = 0; wordIndex < countWords; wordIndex++)
            {
                int startIndex = wordIndex * countByteInOneWord + 1;
                List<int> sliceBytes;
                try
                {
                    sliceBytes = entropyBytes.GetRange(startIndex, countByteInOneWord);
                }
                catch (ArgumentException ex)
                {
                    int endIndex = entropyBytes.Count();
                    startIndex -= 1;
                    sliceBytes = entropyBytes.GetRange(startIndex, endIndex - startIndex);
                }
                bytesEachWordBinary.Add(sliceBytes);
            }
        }

        /// <summary>
        /// Преобразование байтов слов в десятичную систему
        /// </summary>
        /// <returns></returns>
        private void GetBytesEachWordDecimal()
        {
            GetBytesEachWordDecimal(bytesEachWordDecimal, bytesEachWordBinary);
        }
        /// <summary>
        /// Преобразование ЛЮБЫХ байтов слов в десятичную систему
        /// </summary>
        /// <param name="bytesEachWordDecimal"></param>
        /// <param name="bytesEachWordBinary"></param>
        public void GetBytesEachWordDecimal(List<int> bytesEachWordDecimal, List<List<int>> bytesEachWordBinary)
        {
            foreach (List<int> bytesWord in bytesEachWordBinary)
            {
                int decimalWord = Convert.ToInt32(string.Concat(bytesWord), 2);
                bytesEachWordDecimal.Add(decimalWord);
            }
        }

        /// <summary>
        /// Загрузка списка слов, исходя из выбранного языка
        /// </summary>
        private void GetBIP39()
        {
            string path = $"WordsForSeedPhrase/{languageMnemonicPhrase}.txt";
            wordsBIP39 = GetBIP39(path);
        }
        public List<string> GetBIP39(string path)
        {
            if (!File.Exists(path))
            {
                throw new System.ArgumentException("Invalid language select!");
            }
            return File.ReadAllLines(path).ToList();
        }

        private void GetMnemonicPhrase()
        {
            mnemonicPhrase = GetMnemonicPhrase(bytesEachWordDecimal, wordsBIP39);
        }
        public List<string> GetMnemonicPhrase(List<int> bytesEachWordDecimal, List<string> wordsBIP39)
        {
            List<string> mnemonicPhrase = bytesEachWordDecimal.Select(x => wordsBIP39[x]).ToList();
            return mnemonicPhrase;
        }

    }
}

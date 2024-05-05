using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using CefSharp.DevTools.Preload;
using System.Reflection;

namespace CefSharp.MinimalExample.OffScreen.GenerationMnemonic
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
    /// Базовый класс генерации мнемонической фразы
    /// </summary>
    public class MnemonicPhrase
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
            }
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
        // Поиск слов по их позициям на основе массива байтов в десятичной системе
        public List<string> wordsBIP39 { get; set; }
        // Алгоритм 
        private IGetEntropy entropyAlgorythm { get; set; }
        // Контрольная сумма
        private string _controlSum;
        public string controlSum 
        {
            get { return _controlSum; }
            set { _controlSum = value; } 
        }
        // Первые два символа контрольной суммы в двоичной системе (первая часть последнего слова)
        public string firstPartLastWord { get; set; }
        // Хвост после разделения массива байтов по словам (вторая часть последнего слова)
        public string secondPartLastWord { get; set; }


        /// <summary>
        /// В конструкторе последовательная инициализация. Сначала проверяется количество байт, после количество слов.
        /// </summary>
        /// <param name="languageMnemonicPhrase"></param>
        /// <param name="countEntropyByte"></param>
        /// <param name="countWords"></param>
        /// <param name="entropyAlgorythm"></param>
        public MnemonicPhrase(WordListLanguage languageMnemonicPhrase, int countEntropyByte, int countWords, IGetEntropy entropyAlgorythm)
        {
            this.languageMnemonicPhrase = languageMnemonicPhrase;
            this.countEntropyBytes = countEntropyByte;
            this.countWords = countWords;
            this.entropyAlgorythm = entropyAlgorythm;

            entropyBytes = new List<int>();
            bytesEachWordBinary = new List<List<int>>();
            bytesEachWordDecimal = new List<int>();
            wordsBIP39 = new List<string>();
        }

        /// <summary>
        /// Заполнить массив байтов энтропии в классе
        /// </summary>
        public void GetEntropyBytes()
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
        public void GetControlSum()
        {
            controlSum = GetControlSum(entropyBytes);
        }
        /// <summary>
        /// Найти ЛЮБУЮ контрольную сумму исходя из массива байтов
        /// </summary>
        public string GetControlSum(List<int> _entropyBytes)
        {
            // Преобразование байтов энтропии в нужный для функции формат
            byte[] bytesToSHA256 = _entropyBytes.Select(x => (byte)x).ToArray();

            var sha256Provider = new SHA256CryptoServiceProvider();
            byte[] hash = sha256Provider.ComputeHash(bytesToSHA256);
            string[] controlSumBinary = hash.Select(x => Convert.ToString(x, 16)).ToArray();
            return string.Concat(controlSumBinary);
        }

        /// <summary>
        /// Перевести первые два символа контрольной суммы из шестнадцатеричной системы в двоичную
        /// </summary>
        public void GetLastWordFirstPartToBinary()
        {
            firstPartLastWord = GetLastWordFirstPartToBinary(controlSum);
        }
        public string GetLastWordFirstPartToBinary(string controlSum)
        {
            string twoCharStartInControlSum = controlSum[0..2];
            int transformToInt = Convert.ToInt32(twoCharStartInControlSum, 16);
            string transformToBytes = Convert.ToString(transformToInt, 2);
            return transformToBytes;
        }

        /// <summary>
        /// Заполнить массив слов из байтов в классе
        /// </summary>
        /// <returns></returns>
        public void GetBytesEachWordBinary()
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
        public void GetBytesEachWordDecimal()
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
        /// Получение слов из словаря
        /// </summary>
        /// <returns></returns>
        public List<string> GetWordsBIP39()
        {
            throw new System.Exception("Not realize function!");
            return new List<string>() { "" };
        }



    }
}

using System;
using System.Collections.Generic;
using System.Text;

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
    public abstract class GenerationMnemonic
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
        public List<byte> entropyBytes { get; set; }
        // Разделение на количество слов
        public List<byte[]> bytesEachWordBinary { get; set; }
        // Перевод байтов слов из двоичной системы исчисления в десятичную
        public List<int> bytesEachWordDecimal { get; set; }
        // Поиск слов по их позициям на основе массива байтов в десятичной системе
        public List<string> wordsBIP39 { get; set; }
        // Алгоритм 
        private IGetEntropy entropyAlgorythm { get; set; }

        /// <summary>
        /// В конструкторе последовательная инициализация. Сначала проверяется количество байт, после количество слов.
        /// </summary>
        /// <param name="languageMnemonicPhrase"></param>
        /// <param name="countEntropyByte"></param>
        /// <param name="countWords"></param>
        /// <param name="entropyAlgorythm"></param>
        public GenerationMnemonic(WordListLanguage languageMnemonicPhrase, int countEntropyByte, int countWords, IGetEntropy entropyAlgorythm)
        {
            this.languageMnemonicPhrase = languageMnemonicPhrase;
            this.countEntropyBytes = countEntropyByte;
            this.countWords = countWords;
            this.entropyAlgorythm = entropyAlgorythm;

            entropyBytes = new List<byte>();
            bytesEachWordBinary = new List<byte[]>();
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
        public void GetEntropyBytes(List<byte> _entropyBytes, int countEntropyBytes)
        {
            _entropyBytes = entropyAlgorythm.ExecuteGetEntropyBytes(countEntropyBytes);

        }

        /// <summary>
        /// Заполнить массив слов из байтов в класе
        /// </summary>
        /// <returns></returns>
        public void GetBytesEachWordBinary()
        {
            GetBytesEachWordBinary(bytesEachWordBinary, entropyBytes, countWords);
        }
        /// <summary>
        /// Заполнить ЛЮБОЙ массив слов из байтов в класе
        /// </summary>
        /// <returns></returns>
        public void GetBytesEachWordBinary(List<byte[]> bytesEachWordBinary, List<byte> entropyBytes, int countWords)
        {
            for (int wordIndex = 0; wordIndex < countWords; wordIndex++)
            {

            }
        }
            

        /// <summary>
        /// Преобразование байтов слов в десятичную систему
        /// </summary>
        /// <returns></returns>
        public List<byte> GetBytesEachWordDecimal()
        {

        }

        /// <summary>
        /// Получение слов из словаря
        /// </summary>
        /// <returns></returns>
        public List<string> GetWordsBIP39()
        {

        }



    }
}

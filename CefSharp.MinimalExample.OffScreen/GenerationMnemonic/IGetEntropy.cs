using System.Collections.Generic;

namespace CefSharp.MinimalExample.OffScreen.GenerationMnemonic
{
    public interface IGetEntropy
    {   
        /// <summary>
        /// Получение энтропии в байтах
        /// </summary>
        /// <returns></returns>
        public List<int> ExecuteGetEntropyBytes(int countEntropyBytes);
    }
}

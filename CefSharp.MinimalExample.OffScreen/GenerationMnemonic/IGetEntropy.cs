using System.Collections.Generic;

namespace CefSharp.MinimalExample.OffScreen.GenerationMnemonic
{
    public interface IGetEntropy
    {   
        /// <summary>
        /// Получение энтропии в байтах
        /// </summary>
        /// <returns></returns>
        public void ExecuteGetEntropyBytes(List<int> _entropyBytes, int countEntropyBytes);
    }
}

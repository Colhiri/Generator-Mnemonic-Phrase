using System;
using System.Collections.Generic;

namespace CefSharp.MinimalExample.OffScreen.GenerationMnemonic
{
    public class PseudoRandomGenerationByte : IGetEntropy
    {
        public void ExecuteGetEntropyBytes(List<int> _entropyBytes, int countBytes)
        {
            Random randomByte = new Random();
            for (int numByte = 0; numByte < countBytes; numByte++)
            {
                _entropyBytes.Add(randomByte.Next(0, 2));
            }
        }
    }
}

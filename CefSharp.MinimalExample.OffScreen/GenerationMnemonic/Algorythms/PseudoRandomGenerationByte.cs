using System;
using System.Collections.Generic;

namespace CefSharp.MinimalExample.OffScreen.GenerationMnemonic.Algorythms
{
    public class PseudoRandomGenerationByte : IGetEntropy
    {
        public List<int> ExecuteGetEntropyBytes(int countBytes)
        {
            List<int> entropyBytes = new List<int>();

            Random randomByte = new Random();
            for (int numByte = 0; numByte < countBytes; numByte++)
            {
                entropyBytes.Add(randomByte.Next(0, 2));
            }

            return entropyBytes;
        }
    }
}

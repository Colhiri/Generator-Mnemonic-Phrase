using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace CefSharp.MinimalExample.OffScreen.GenerationMnemonic.Algorythms
{
    public class PingGenerator : IGetEntropy
    {
        private long Test()
        {
            Ping myPing = new Ping();
            String host = "google.com";
            byte[] buffer = new byte[32];
            int timeout = 10;
            PingOptions pingOptions = new PingOptions();
            PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
            return reply.RoundtripTime;
        }

        public List<int> ExecuteGetEntropyBytes(int countEntropyBytes)
        {
            PingGenerator pingGenerator = new PingGenerator();

            List<long> pings = new List<long>();

            int condition = 1;
            for (int countByte = 0; countByte < countEntropyBytes * 100; countByte++)
            {
                // Первое число остается каким было
                // Второе число складывается с предыдущим
                // Третье число реверсируется
                long pingValue = pingGenerator.Test();
                pings.Add(pingValue);
            }

            long allValues = pings.Sum();

            // Преобразование пинга в байты
            // List<string> pingsToByte = pings.Select(x => Convert.ToString(x, 2)).ToList();
            string bytes = Convert.ToString(allValues, 2);

            // Объединение всех полученных байтов
            // string concaPingBytes = string.Concat(pingsToByte);

            // Берем нужное количество байтов
            // string needEntropy = concaPingBytes[0..countEntropyBytes];
            string needEntropy = bytes[0..countEntropyBytes];

            List<int> bytesEntropy = needEntropy.ToCharArray().Select(x => int.Parse(x.ToString())).ToList();

            return bytesEntropy;
        }
    }
}

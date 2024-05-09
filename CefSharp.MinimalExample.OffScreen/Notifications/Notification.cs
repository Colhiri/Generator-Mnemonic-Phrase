using System;
using System.IO;

namespace CefSharp.MinimalExample.OffScreen.Notifications
{
    public class Notification
    {
        public string pathToLogFile { get; set; } = "MnemonicPhrase.txt";

        public Notification()
        {

        }

        /// <summary>
        /// Уведомить пользователя о том, что фраза рабочая
        /// </summary>
        /// <param name="message"></param>
        public void ExecuteNotify(string message)
        {
            if (!File.Exists(pathToLogFile)) File.Create(pathToLogFile);

            using (StreamWriter strWriter = new StreamWriter(pathToLogFile))
            {
                strWriter.Write(message);
            }

            Console.WriteLine(message);
        }
    }
}

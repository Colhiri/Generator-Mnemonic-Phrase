using System;
using System.Collections.Generic;
using System.Text;

namespace CefSharp.MinimalExample.OffScreen.Notifications
{
    public class Notification
    {
        /// <summary>
        /// Уведомить пользователя о том, что фраза рабочая
        /// </summary>
        /// <param name="message"></param>
        public void ExecuteNotify(string message)
        {
            Console.WriteLine(message);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public static class ServerLogger
    {
        private static readonly object _lock = new object();
        private static readonly string CustomLogDirectory = @"C:\Logs";
        public static void Log(string message, string fileName = "paypal_webhook_logs.txt")
        {
            try
            {


                //string folderPath = Path.Combine(AppContext.BaseDirectory, "Logs");

                string folderPath = Directory.Exists(CustomLogDirectory)
                                    ? CustomLogDirectory          //for local system
                                    : Path.Combine(AppContext.BaseDirectory, "Logs");    //for live

                if (!Directory.Exists(folderPath))
                {

                    Directory.CreateDirectory(folderPath);
                }

                string logFilePath = Path.Combine(folderPath, fileName);

                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";

                lock (_lock)
                {
                    File.AppendAllText(logFilePath, logMessage);
                }
            }
            catch(Exception  e) {
            
                Console.WriteLine(e.ToString());
            }
        }
    }
}

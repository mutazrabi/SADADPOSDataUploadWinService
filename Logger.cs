using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SADADPOSDataUploadWinService
{
    public static class Logger
    {
        public static void WriteLog(string exceptionMessage, string exceptionTrace, string methodName)
        {
            string dir = System.Configuration.ConfigurationManager.AppSettings["Logfile"].ToString();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string filePath = System.Configuration.ConfigurationManager.AppSettings["Logfile"] + DateTime.Today.ToString("dd-MM-yy") + ".txt";
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }

            FileStream fs = File.Open(filePath, FileMode.Append, FileAccess.Write);
            using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("##################################################################################################");
                sw.WriteLine("Date Time : " + DateTime.Now.ToString());
                sw.WriteLine("Method Name : " + methodName);
                sw.WriteLine("Error Message : " + exceptionMessage);
                sw.WriteLine("Error Trace : " + exceptionTrace);
                sw.Close();
            }
        }
        public static void TrackLogs(string informationMessage, string methodName)
        {
            string dir = System.Configuration.ConfigurationManager.AppSettings["Trackingfile"].ToString();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string filePath = System.Configuration.ConfigurationManager.AppSettings["Trackingfile"] + DateTime.Today.ToString("dd-MM-yy") + ".txt";
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }

            FileStream fs = File.Open(filePath, FileMode.Append, FileAccess.Write);
            using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("##################################################################################################");
                sw.WriteLine("Date Time : " + DateTime.Now.ToString());
                sw.WriteLine("Method Name : " + methodName);
                sw.WriteLine("Message : " + informationMessage);
                sw.Close();
            }
        }
        public static void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }


    }
}

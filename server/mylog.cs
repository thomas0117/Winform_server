using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Data;
namespace server
{
    public static class myLog
    {
        public static string FilePath { get; set; }
        public static void Write(string format, params object[] arg)
        {
            Write(string.Format(format, arg));
        }

        public static void Write(string message)
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                FilePath = Directory.GetCurrentDirectory();
            }
            string filename = FilePath +
                string.Format("\\{0:yyyy}\\{0:MM}\\{0:yyyy-MM-dd}.txt", DateTime.Now);
            FileInfo finfo = new FileInfo(filename);
            if (finfo.Directory.Exists == false)
            {
                finfo.Directory.Create();
            }
            string writeString = string.Format("{0:yyyy/MM/dd HH:mm:ss} {1}",
                DateTime.Now, message) + Environment.NewLine;
            try
            {
                File.AppendAllText(filename, writeString, Encoding.Unicode);
            }
            catch
            {
                
            }
        }

    }

    public class ReadWriteIni
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section,
        string key, string val, string filePath);
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section,
        string key, string def, StringBuilder retVal,
        int size, string filePath);
        public void IniWriteValue(string Section, string Key, string Value, string inipath)
        {
            try
            {
                WritePrivateProfileString(Section, Key, Value, Directory.GetCurrentDirectory() + "\\" + inipath);
            }
            catch (Exception e)
            {
                myLog.Write("寫INI失敗" + e.ToString());
            }
        }
        public string IniReadValue(string Section, string Key, string inipath)
        {
            StringBuilder temp = new StringBuilder(255);
            try
            {
                int i = GetPrivateProfileString(Section, Key, "", temp, 255, Directory.GetCurrentDirectory() + "\\" + inipath);
                if (i == 0)
                {
                    WritePrivateProfileString(Section, Key, "", Directory.GetCurrentDirectory() + "\\" + inipath);
                }
                return temp.ToString();
            }
            catch (Exception e)
            {

                myLog.Write("讀INI失敗" + e.ToString());
            }
            return "error";

        }

    }

}

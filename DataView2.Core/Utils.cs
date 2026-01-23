using System.IO;
using System.Text.RegularExpressions;

namespace DataView2.Core
{
    public static class Utils
    {
        public static void RegError(string err)
        {
            string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MyAppLogs");
            Directory.CreateDirectory(logDirectory);  // Make sure the existence of the directory.

            string logFile = Path.Combine(logDirectory, "applog.txt");

            File.AppendAllText(logFile, $"[{DateTime.Now}] Error: {err}\n");
        }
    }
}

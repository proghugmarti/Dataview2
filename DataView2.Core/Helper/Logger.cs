using System.Globalization;

namespace DataView2.Core.Helper
{
    public class Logger
    {
        public enum TypeError
        {
            ERROR = 1,
            INFO = 2,
            WARNING = 3
        }
        public static void WriteLog(string path, string message, TypeError errorType)
        {
            try
            {
                string datePart = DateTime.UtcNow.ToLocalTime().ToString("yyyyMMdd");

                if (string.IsNullOrEmpty(path))
                {
                    string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

                    if (!Directory.Exists(logsDirectory))
                    {
                        Directory.CreateDirectory(logsDirectory);
                    }

                    path = Path.Combine(logsDirectory, $"SrvcProcessing-{datePart}.log");
                }
                else
                {
                    string directory = Path.GetDirectoryName(path);
                    string filenameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                    string extension = Path.GetExtension(path);

                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    path = Path.Combine(directory, $"{filenameWithoutExtension}{datePart}{extension}");
                }

                string timestamp = DateTime.UtcNow.AddHours(13)
                    .ToString("yyyy-MM-dd HH:mm:ss.fff +13:00", CultureInfo.InvariantCulture);

                string errorTypeText = errorType switch
                {
                    TypeError.ERROR => "ERR",
                    TypeError.INFO => "INF",
                    TypeError.WARNING => "WRN",
                    _ => "UNK"
                };

                string logEntry = $"{timestamp} [{errorTypeText}] {message}";

                File.AppendAllText(path, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing log: {ex.Message}");
            }
        }
    }

}

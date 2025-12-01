using System;
using System.IO;
using System.Text;

namespace SmurfAccountManager.Services
{
    public static class LoggerService
    {
        private static readonly object _lock = new object();
        private static string _logDirectory;
        private static string _currentLogFile;

        static LoggerService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _logDirectory = Path.Combine(appData, "SmurfAccountManager", "logs");
            Directory.CreateDirectory(_logDirectory);
            
            // Create today's log file
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            _currentLogFile = Path.Combine(_logDirectory, $"app-{today}.log");
            
            // Clean up old log files (keep last 7 days)
            CleanupOldLogs();
        }

        public static string GetLogDirectory()
        {
            return _logDirectory;
        }

        public static string GetCurrentLogFile()
        {
            return _currentLogFile;
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Warning(string message)
        {
            WriteLog("WARN", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            var fullMessage = message;
            if (ex != null)
            {
                fullMessage += $"\nException: {ex.GetType().Name}: {ex.Message}\nStack Trace: {ex.StackTrace}";
            }
            WriteLog("ERROR", fullMessage);
        }

        public static void Debug(string message)
        {
            WriteLog("DEBUG", message);
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] {level,-5} {message}";
                    
                    // Write to file
                    File.AppendAllText(_currentLogFile, logEntry + Environment.NewLine, Encoding.UTF8);
                    
                    // Also write to debug output
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
            }
            catch
            {
                // Logging failed - don't crash the app
            }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-7);
                var logFiles = Directory.GetFiles(_logDirectory, "app-*.log");
                
                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Cleanup failed - not critical
            }
        }

        public static string GetRecentLogs(int lineCount = 100)
        {
            try
            {
                if (!File.Exists(_currentLogFile))
                    return "No logs available.";

                var lines = File.ReadAllLines(_currentLogFile);
                var startIndex = Math.Max(0, lines.Length - lineCount);
                var recentLines = new string[lines.Length - startIndex];
                Array.Copy(lines, startIndex, recentLines, 0, recentLines.Length);
                
                return string.Join(Environment.NewLine, recentLines);
            }
            catch (Exception ex)
            {
                return $"Error reading logs: {ex.Message}";
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SDS_Monitor
{
    public static class ElmLogger
    {
        private static string _logFilePath = string.Empty;
        private static bool _isEnabled;
        private static bool _isFullCommunicationEnabled;

        public static void Configure(bool isEnabled, bool isFullCommunicationEnabled)
        {
            _isEnabled = isEnabled;
            _isFullCommunicationEnabled = isFullCommunicationEnabled;

            if (!_isEnabled)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_logFilePath))
            {
                return;
            }

            string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _logFilePath = Path.Combine(executableDirectory, $"sds-monitor-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            AppendLine("Session", $"Log file created at {_logFilePath}");
        }

        public static string GetCurrentLogPath()
        {
            return _logFilePath;
        }

        public static void WriteConnection(string message)
        {
            AppendLine("Connection", message);
        }

        public static void WriteCommunication(string message)
        {
            if (!_isFullCommunicationEnabled)
            {
                return;
            }

            AppendLine("Communication", message);
        }

        private static void AppendLine(string section, string message)
        {
            if (!_isEnabled)
            {
                return;
            }

            Configure(_isEnabled, _isFullCommunicationEnabled);

            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{section}] {message}";
            File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            Debug.WriteLine(line);
        }
    }
}

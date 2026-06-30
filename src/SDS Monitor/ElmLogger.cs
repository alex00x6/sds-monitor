using System;
using System.Collections.Generic;
using System.Text;

namespace SDS_Monitor
{
    public static class ElmLogger
    {
        private static readonly List<string> _lines = new List<string>();

        public static void ClearLogs()
        {
            _lines.Clear();
        }
        
        public static void Write(string message)
        {
            _lines.Add($"{DateTime.Now:HH:mm:ss:fff} {message}");
            System.Diagnostics.Debug.WriteLine(message);
        }

        public static string GetLog()
        {
            if (_lines.Count == 0)
            {
                return "Log is empty";
            }
            else
            {
                return string.Join(Environment.NewLine, _lines.ToArray());
            }
        }
    }
}

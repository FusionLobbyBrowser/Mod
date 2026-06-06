using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace Bridge
{
    public class Logger
    {
        private const string DEFAULT_FILE = "log.txt";

        public LogLevel Level { get; set; }

        public string Prefix { get; set; }

        public string FileName { get; private set; } = DEFAULT_FILE;

        public Logger()
        {
            this.Level = LogLevel.Info;
            this.Prefix = string.Empty;
            Clear();
        }

        public Logger(LogLevel level, string fileName = DEFAULT_FILE)
        {
            this.Level = level;
            this.FileName = fileName;
            this.Prefix = string.Empty;
            Clear();
        }

        public Logger(string prefix, string fileName = DEFAULT_FILE)
        {
            this.Prefix = prefix;
            this.FileName = fileName;
            this.Level = LogLevel.Info;
            Clear();
        }

        public Logger(LogLevel level, string prefix, string fileName = DEFAULT_FILE)
        {
            this.Level = level;
            this.FileName = fileName;
            this.Prefix = prefix;
            Clear();
        }

        private void Clear()
        {
            using FileStream fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName), FileMode.Open);
            fileStream.SetLength(0);
            fileStream.Close();
        }

        public string Error(string message, params object[] args)
        {
            if (Level > LogLevel.Error)
                return null;

            var msg = string.Format(message, args);
            return Msg(msg, "ERROR", Color.Red, true);
        }

        public string Info(string message, params object[] args)
        {
            if (Level > LogLevel.Info)
                return null;

            var msg = string.Format(message, args);
            return Msg(msg, "INFO", Color.Cyan, false);
        }

        public string Trace(string message, params object[] args)
        {
            if (Level > LogLevel.Trace)
                return null;

            var msg = string.Format(message, args);
            return Msg(msg, "TRACE", Color.Blue, false);
        }

        public string Warning(string message, params object[] args)
        {
            if (Level > LogLevel.Warning)
                return null;

            var msg = string.Format(message, args);
            return Msg(msg, "WARN", Color.Orange, false);
        }

        private string Msg(string message, string type, Color? color = null, bool colorWholeText = true)
        {
            string msg = FormatPrefix() + message;
            string _type = $"[{type.ToUpper()}] ";
            var str = (color != null ? _type.Pastel(color.Value) : _type) + (color != null && colorWholeText ? msg.Pastel(color.Value) : msg);
            Console.WriteLine(str);
            var clean = (_type + msg).RemoveANSI();
            using (StreamWriter w = File.AppendText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName)))
                w.WriteLine(clean);
            return clean;
        }

        private string FormatPrefix() => $"{(!string.IsNullOrWhiteSpace(Prefix) ? $"[[{Prefix}]] " : string.Empty)}";
    }

    public enum LogLevel
    {
        Trace,
        Info,
        Warning,
        Error
    }
}
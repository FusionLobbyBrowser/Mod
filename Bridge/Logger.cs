using System;
using System.Drawing;

namespace Bridge
{
    public class Logger
    {
        public LogLevel Level { get; set; }

        public string Prefix { get; set; }

        public Logger()
        {
            this.Level = LogLevel.Info;
            this.Prefix = string.Empty;
        }

        public Logger(LogLevel level)
        {
            this.Level = level;
            this.Prefix = string.Empty;
        }

        public Logger(string prefix)
        {
            this.Prefix = prefix;
            this.Level = LogLevel.Info;
        }

        public Logger(LogLevel level, string prefix)
        {
            this.Level = level;
            this.Prefix = prefix;
        }

        public string Error(string message, params object[] args)
        {
            if (Level > LogLevel.Error)
                return null;

            var msg = string.Format(message, args);
            var str = ("[ERORR] " + FormatPrefix() + msg).Pastel(Color.Red);
            Console.WriteLine(str);
            return str;
        }

        public string Info(string message, params object[] args)
        {
            if (Level > LogLevel.Info)
                return null;

            var msg = string.Format(message, args);
            var str = "[INFO] ".Pastel(Color.Cyan) + FormatPrefix() + msg;
            Console.WriteLine(str);
            return str;
        }

        public string Trace(string message, params object[] args)
        {
            if (Level > LogLevel.Trace)
                return null;

            var msg = string.Format(message, args);
            var str = "[TRACE] ".Pastel(Color.Blue) + FormatPrefix() + msg;
            Console.WriteLine(str);
            return str;
        }

        public string Warning(string message, params object[] args)
        {
            if (Level > LogLevel.Warning)
                return null;

            var msg = string.Format(message, args);
            var str = "[WARN] ".Pastel(Color.Orange) + FormatPrefix() + msg;
            Console.WriteLine(str);
            return str;
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
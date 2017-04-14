using System;
namespace ETGMod {
    public class Logger {
        public enum LogLevel {
            Error = 0,
            Warn = 1,
            Info = 2,
            Debug = 3
        }

        public LogLevel MaxLogLevel =
#if DEBUG
            LogLevel.Debug;
#else
            LogLevel.Warn;
#endif

        public string ID;

        public Logger(string id) {
            ID = id;
        }

        public bool LogLevelEnabled(LogLevel level) {
            return MaxLogLevel >= level;
        }

        public void Debug(object o) {
            if (LogLevelEnabled(LogLevel.Debug)) Console.WriteLine($"[{ID} DEBUG] {o}");
        }

        public void Info(object o) {
            if (LogLevelEnabled(LogLevel.Info)) Console.WriteLine($"[{ID} INFO] {o}");
        }

        public void Warn(object o) {
            if (LogLevelEnabled(LogLevel.Warn)) Console.WriteLine($"[{ID} WARNING] {o}");
        }

        public void Error(object o, bool @throw = false) {
            if (!LogLevelEnabled(LogLevel.Error)) return;
            Console.WriteLine($"[{ID} ERROR] {o}");
            if (@throw) {
                throw new Exception(o.ToString());
            }
        }

        public void Log(LogLevel level, object o) {
            switch(level) {
            case LogLevel.Info: Info(o); break;
            case LogLevel.Debug: Debug(o); break;
            case LogLevel.Error: Error(o); break;
            case LogLevel.Warn: Warn(o); break;
            default: throw new Exception($"Wrong log level: {level}");
            }
        }
    }
}

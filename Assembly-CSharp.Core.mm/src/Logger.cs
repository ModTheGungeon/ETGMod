using System;
namespace ETGMod {
    public static class DefaultLogger {
        public static Logger Logger = new Logger("ETGMod");

        public static void Debug(object o) { Logger.Debug(o); }
        public static void Info(object o) { Logger.Info(o); }
        public static void Warn(object o) { Logger.Warn(o); }
        public static void Error(object o) { Logger.Error(o); }

    }

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
        LogLevel.Info;
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
            Console.WriteLine($"[ETGMod {ID}] {o}");
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

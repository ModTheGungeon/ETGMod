using System;
namespace UnityEngine {
    /// <summary>
    /// Patches the UnityEngine logger to use ETGMod's logger
    /// </summary>
    public class patch_Logger {
        public static ETGMod.Logger GungeonLogger = new ETGMod.Logger("Gungeon");

        /// <summary>
        /// Converts a UnityEngine.LogType to an ETGMod.Logger.LogLevel
        /// </summary>
        /// <returns>The ETGMod LogLevel</returns>
        /// <param name="type">The Unity LogType</param>
        private ETGMod.Logger.LogLevel _LogTypeToLogLevel(LogType type) {
            switch(type) {
                case LogType.Log: return ETGMod.Logger.LogLevel.Info;
                case LogType.Assert: return ETGMod.Logger.LogLevel.Error;
                case LogType.Error: return ETGMod.Logger.LogLevel.Error;
                case LogType.Exception: return ETGMod.Logger.LogLevel.Error;
                case LogType.Warning: return ETGMod.Logger.LogLevel.Warn;
                default: return ETGMod.Logger.LogLevel.Debug;
            }
        }

        private string _FormatMessage(object message, string tag = null, Object context = null) {
            if (tag != null && context != null) return $"[tag: {tag}, context: {context}] {message}";
            if (tag != null && context == null) return $"[context: {context}] {message}";
            if (tag == null && context != null) return $"[tag: {tag}] {message}";
            return message.ToString();
        }

        public bool IsLogTypeAllowed(LogType logType) {
            return GungeonLogger.LogLevelEnabled(_LogTypeToLogLevel(logType));
        }

        public void Log(string tag, object message, Object context) {
            GungeonLogger.Info(_FormatMessage(message, tag: tag, context: context));
        }

        public void Log(string tag, object message) {
            GungeonLogger.Info(_FormatMessage(message, tag: tag));
        }

        public void Log(object message) {
            GungeonLogger.Info(_FormatMessage(message));
        }

        public void Log(LogType logType, string tag, object message, Object context) {
            GungeonLogger.Log(_LogTypeToLogLevel(logType), _FormatMessage(message, tag: tag, context: context));
        }

        public void Log(LogType logType, string tag, object message) {
            GungeonLogger.Log(_LogTypeToLogLevel(logType), _FormatMessage(message, tag: tag));
        }

        public void Log(LogType logType, object message, Object context) {
            GungeonLogger.Log(_LogTypeToLogLevel(logType), _FormatMessage(message, context: context));
        }

        public void Log(LogType logType, object message) {
            GungeonLogger.Log(_LogTypeToLogLevel(logType), _FormatMessage(message));
        }

        public void LogError(string tag, object message, Object context) {
            GungeonLogger.Error(_FormatMessage(message, tag: tag, context: context));
        }

        public void LogError(string tag, object message) {
            GungeonLogger.Error(_FormatMessage(message, tag: tag));
        }

        public void LogException(Exception exception) {
            GungeonLogger.Error(_FormatMessage(exception));
        }

        public void LogException(Exception exception, Object context) {
            GungeonLogger.Error(_FormatMessage(exception, context: context));
        }

        public void LogWarning(string tag, object message) {
            GungeonLogger.Warn(_FormatMessage(message, tag: tag));
        }

        public void LogWarning(string tag, object message, Object context) {
            GungeonLogger.Warn(_FormatMessage(message, tag: tag, context: context));
        }
    }
}

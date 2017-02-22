using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MonoMod {
    // This class gets executed in the MonoMod (installer) context.
    static class MonoModRules {

        static object[] _a_object_0 = new object[0];

        static Assembly _InstallerAsm;
        public static Assembly InstallerAsm {
            get {
                if (_InstallerAsm != null)
                    return _InstallerAsm;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                    if (asm.GetName().Name == "ETGMod.Installer")
                        return _InstallerAsm = asm;
                }
                return null;
            }
        }

        static Type _t_InstallerWindow;
        public static Type t_InstallerWindow {
            get {
                if (_t_InstallerWindow != null)
                    return _t_InstallerWindow;
                return _t_InstallerWindow = InstallerAsm.GetType("ETGModInstaller.InstallerWindow");
            }
        }

        static object _InstallerWindow;
        public static object InstallerWindow {
            get {
                if (_InstallerWindow != null)
                    return _InstallerWindow;
                return _InstallerWindow = t_InstallerWindow.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            }
        }

        static MethodInfo _m_Log;
        public static MethodInfo m_Log {
            get {
                if (_m_Log != null)
                    return _m_Log;
                return _m_Log = t_InstallerWindow.GetMethod("Log");
            }
        }

        static MethodInfo _m_LogLine_Empty;
        public static MethodInfo m_LogLine_Empty {
            get {
                if (_m_LogLine_Empty != null)
                    return _m_LogLine_Empty;
                return _m_LogLine_Empty = t_InstallerWindow.GetMethod("LogLine", Type.EmptyTypes);
            }
        }

        static MethodInfo _m_LogLine;
        public static MethodInfo m_LogLine {
            get {
                if (_m_LogLine != null)
                    return _m_LogLine;
                return _m_LogLine = t_InstallerWindow.GetMethod("LogLine", new Type[] { typeof(string) });
            }
        }

        public static void Log(string s)
            => m_Log.Invoke(InstallerWindow, new object[] { s });

        public static void LogLine()
            => m_LogLine_Empty.Invoke(InstallerWindow, _a_object_0);

        public static void LogLine(string s)
            => m_LogLine.Invoke(InstallerWindow, new object[] { s });

        static MonoModRules() {
            string platform = (string) MMIL.Data.Get("Platform");
            if (platform == "Windows") {
                // Dummy rule - won't affect anything.
                MMIL.Rule.RelinkMember(
                    "System.Void SomeType::SomeMethod(System.UInt32,System.Boolean)", // "From" identifier with type.
                    "SomeOtherTypeMaybe", "System.Void SomeOtherMethod(System.UInt32,System.Boolean)" // "To" type and method identifier without type.
                );
            } else if (platform == "Linux") {
                // ...
            } else if (platform == "MacOS") {
                // ...
            }
            MMIL.Rule.RelinkMember(
                "System.Int32 SomeType::default_DoMaths(System.Int32,System.Int32)",
                "SomeType", $"System.Int32 {MMIL.Data.Get("PlatformPrefix")}DoMaths(System.Int32,System.Int32)"
            );
        }

    }
}

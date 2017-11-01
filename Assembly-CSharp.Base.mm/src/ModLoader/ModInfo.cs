using System;
using System.Collections.Generic;
using System.Reflection;
using ETGMod.Lua;
using NLua;

namespace ETGMod {
    public partial class ModLoader {
        public partial class ModInfo {
            public Logger Logger = new Logger("Unnamed Mod");
            private string _NameOverride;

            public ModInfo Parent;

            public string Name {
                get {
                    if (_NameOverride == null) return ModMetadata.Name;
                    return _NameOverride;
                }
                internal set {
                    _NameOverride = value;
                    Logger.ID = value;
                }
            }
            public List<ModInfo> EmbeddedMods {
                get;
                internal set;
            } = new List<ModInfo>();

            public LuaTable LuaEnvironment;

            public EventContainer Events;

            private Metadata _ModMetadata;
            public Metadata ModMetadata {
                get {
                    return _ModMetadata;
                }
                internal set {
                    _ModMetadata = value;
                    if (_NameOverride == null) {
                        Logger.ID = value.Name;
                    }
                }
            }

            public string RealPath {
                get;
                internal set;
            }

            public string Path {
                get;
                internal set;
            }

            public string ScriptPath {
                get;
                internal set;
            }

            public bool HasScript {
                get {
                    return ScriptPath != null;
                }
            }

            public bool HasAnyEmbeddedMods {
                get {
                    return EmbeddedMods.Count > 0;
                }
            }

            public bool IsComplete {
                get {
                    return RealPath != null && Path != null && ModMetadata != null;
                }
            }

            public object[] RunLua(LuaFunction func, string name = "[unknown]", params object[] args) {
                object[] ret = null;

                try {
                    func.Environment = LuaEnvironment;

                    ret = func.Call(args);
                } catch (Exception e) {
                    Logger.Error($"{e.GetType().Name} thrown while running {name} of mod {Name}");

                    Logger.ErrorIndent($"Lua error:");
                    Logger.ErrorIndent("  " + e.Message);
                    ETGMod.ModLoader.LuaError.Invoke(this, LuaEventMethod.Loaded, e);
                    if (e is NLua.Exceptions.LuaScriptException && ((NLua.Exceptions.LuaScriptException)e).Traceback != null) {
                        var luaex = (NLua.Exceptions.LuaScriptException)e;
                        Logger.ErrorIndent($"Lua stack trace:");
                        foreach (var l in luaex.Traceback) {
                            Logger.ErrorIndent("  " + l);
                        }
                    }

                    Logger.ErrorIndent($"C# stack trace: ");
                    foreach (var l in e.StackTrace.Split('\n')) Logger.ErrorIndent(l);

                    if (e.InnerException != null) {
                        Logger.ErrorIndent($"Inner exception: [{e.InnerException.GetType().Name}] {e.InnerException.Message}");
                        foreach (var l in e.InnerException.StackTrace.Split('\n')) Logger.ErrorIndent(l);
                    }
                }
                return ret;
            }
        }
    }
}
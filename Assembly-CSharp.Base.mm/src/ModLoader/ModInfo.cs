using System;
using System.Collections.Generic;
using System.Reflection;
using ETGMod.Lua;
using Eluant;

namespace ETGMod {
    public partial class ModLoader {
        public partial class ModInfo : IDisposable {
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
            internal LuaTable RealPackageTable;

            public EventContainer Events;
            public HookManager Hooks;

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

            public LuaVararg RunLua(LuaFunction func, string name = "[unknown]", params LuaValue[] args) {
                LuaVararg ret = null;

                try {
                    func.Environment = LuaEnvironment;

                    ret = func.Call(args);
                } catch (Exception e) {
                    ETGMod.ModLoader.LuaError.Invoke(this, LuaEventMethod.Loaded, e);
                    Logger.Error(e.Message);

                    if (e is LuaException) {
                        for (int i = 0; i < ((LuaException)e).TracebackArray.Length; i++) {
                            Logger.ErrorIndent("  " + ((LuaException)e).TracebackArray[i]);
                        }
                    } else {
                        var lines = e.StackTrace.Split('\n');
                        for (int i = 0; i < lines.Length; i++) {
                            Logger.ErrorIndent(lines[i]);
                        }
                    }
                }

                return ret;
            }

            public void Dispose() {
                LuaEnvironment?.Dispose();
                RealPackageTable?.Dispose();
                Events?.Dispose();
                Hooks?.Dispose();
            }
        }
    }
}
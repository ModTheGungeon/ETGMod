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
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ETGMod {
    public partial class ModLoader {
        public partial class ModInfo {
            internal Logger Logger = new Logger("Unnamed Mod");
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
            public List<Mod> Behaviours {
                get;
                internal set;
            } = new List<Mod>();

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
            public string AssemblyPath {
                get;
                internal set;
            }
            internal AssemblyNameMap AssemblyNameMap = new AssemblyNameMap();

            private ResolveEventHandler _AssemblyResolveHandler;
            public ResolveEventHandler AssemblyResolveHandler {
                get {
                    if (_AssemblyResolveHandler != null) return _AssemblyResolveHandler;
                    return (object sender, ResolveEventArgs args) => {
                        Assembly result = null;
                        string path;

                        Logger.Debug($"Resolving assembly: {args.Name}");
                        if (AssemblyNameMap.TryGetPath(args.Name, out path)) {
                            Logger.Debug($"Resolved with {path}");
                            result = Assembly.LoadFrom(path);
                        } else {
                            Logger.Debug($"Unresolved");
                        }

                        return result;
                    };
                }
            }

            private Assembly _Assembly;
            public Assembly Assembly {
                get {
                    if (_Assembly != null) return _Assembly;
                    if (AssemblyPath == null) {
                        throw new InvalidOperationException($"Tried to access Assembly without AssemblyPath");
                    }
                    return _Assembly = Assembly.LoadFrom(AssemblyPath);
                }
            }

            public bool HasAssembly {
                get {
                    return Assembly != null;
                }
            }

            public bool HasAnyBehaviour {
                get {
                    return Behaviours.Count > 0;
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
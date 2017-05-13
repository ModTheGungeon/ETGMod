using System;
using System.Collections.Generic;
using System.IO;

namespace ETGMod {
    public partial class ModLoader {
        public partial class ModInfo {
            public class ResourcePool {
                public static string BaseResourceDir {
                    get {
                        return "resources";
                    }
                }

                public string BaseDir {
                    get;
                    protected set;
                }

                public string Path {
                    get;
                    protected set;
                }

                private Dictionary<string, LoadedResource> _LoadedResources = new Dictionary<string, LoadedResource>();

                public ResourcePool(string base_dir) {
                    BaseDir = base_dir;
                    Path = System.IO.Path.Combine(BaseDir, BaseResourceDir);
                }

                public LoadedResource Load(string relative_path) {
                    var normalized = relative_path.NormalizePath();
                    var relative_to_resources_dir = System.IO.Path.Combine(BaseResourceDir, normalized);
                    var final_path = System.IO.Path.Combine(BaseDir, relative_to_resources_dir);

                    if (!File.Exists(final_path)) {
                        throw new FileNotFoundException($"Resource '{normalized}' doesn't exist.");
                    }

                    var res = new LoadedResource(
                        normalized,
                        final_path
                    );
                    _LoadedResources[normalized] = res;
                    return res;
                }

                public string LoadText(string relative_path) {
                    return Load(relative_path).ReadText();
                }

                public byte[] LoadBinary(string relative_path) {
                    return Load(relative_path).ReadBinary();
                }
            }
        }
    }
}

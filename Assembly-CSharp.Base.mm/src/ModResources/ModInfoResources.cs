using System;
using System.IO;

namespace ETGMod {
    public partial class ModLoader {
        public partial class ModInfo {
            private ResourcePool _Resources;
            public ResourcePool Resources {
                get {
                    if (_Resources != null) return _Resources;
                    if (RealPath == null) {
                        throw new InvalidOperationException($"Tried to access Resources without RealPath");
                    }
                    return _Resources = new ResourcePool(RealPath);
                }
            }

            public bool FileExists(string relative_path) {
                var p = System.IO.Path.Combine(ResourcePool.BaseResourceDir, relative_path);
                return File.Exists(p);
            }

            public bool DirectoryExists(string relative_path) {
                var p = System.IO.Path.Combine(ResourcePool.BaseResourceDir, relative_path);
                return Directory.Exists(p);
            }

            public T Load<T>(string relative_path) {
                try {
                    return Resources.Load(relative_path).SpecialCast<T>(this);
                } catch (FileNotFoundException e) {
                    throw new FileNotFoundException($"[{Logger.ID}] While loading resource '{relative_path.NormalizePath()}': {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}

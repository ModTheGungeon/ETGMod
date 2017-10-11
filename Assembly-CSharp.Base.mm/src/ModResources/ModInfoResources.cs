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

            public LoadedResource LoadResource(string relative_path) {
                try {
                    return Resources.Load(relative_path);
                } catch (FileNotFoundException e) {
                    throw new FileNotFoundException($"[{Logger.ID}] While loading resource '{relative_path.NormalizePath()}': {e.Message}\n{e.StackTrace}");
                }
            }

            public string LoadText(string relative_path) {
                try {
                    return Resources.Load(relative_path).ReadText();
                } catch (FileNotFoundException e) {
                    throw new FileNotFoundException($"[{Logger.ID}] While loading text resource '{relative_path.NormalizePath()}': {e.Message}\n{e.StackTrace}");
                }
            }

            public byte[] LoadBytes(string relative_path) {
                try {
                    return Resources.Load(relative_path).ReadBinary();
                } catch (FileNotFoundException e) {
                    throw new FileNotFoundException($"[{Logger.ID}] While loading bytes resource '{relative_path.NormalizePath()}': {e.Message}\n{e.StackTrace}");
                }
            }

            public UnityEngine.Texture2D LoadTexture(string relative_path) {
                try {
                    return Resources.Load(relative_path).GetTexture2D();
                } catch (FileNotFoundException e) {
                    throw new FileNotFoundException($"[{Logger.ID}] While loading resource '{relative_path.NormalizePath()}': {e.Message}\n{e.StackTrace}");
                }
            }

            public Animation LoadAnimation(string relative_path) {
                try {
                    return Resources.Load(relative_path).GetAnimation(this);
                } catch (FileNotFoundException e) {
                    throw new FileNotFoundException($"[{Logger.ID}] While loading resource '{relative_path.NormalizePath()}': {e.Message}\n{e.StackTrace}");
                }
            }

            public Animation.Collection LoadCollection(string relative_path) {
                try {
                    return Resources.Load(relative_path).GetAnimationCollection(this);
                } catch (FileNotFoundException e) {
                    throw new FileNotFoundException($"[{Logger.ID}] While loading resource '{relative_path.NormalizePath()}': {e.Message}\n{e.StackTrace}");
                }
            }

            public void Unload(string relative_path) {
                try {
                    Resources.Unload(relative_path);
                } catch (FileNotFoundException e) {
                    throw new FileNotFoundException($"[{Logger.ID}] While unloading resource '{relative_path.NormalizePath()}': {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}

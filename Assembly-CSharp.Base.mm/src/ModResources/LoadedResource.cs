using System;
using System.Text;
using System.IO;
using ETGMod;
using UnityEngine;
using System.Collections.Generic;

namespace ETGMod {
    public class LoadedResource {
        public enum StorageType {
            Stream,
            Text,
            Binary
        }

        public StorageType Type { get; protected set; }
        public string Path { get; protected set; }
        public string ResourcePath { get; protected set; }

        public Stream Stream { get; protected set; }
        public string TextContent { get; protected set; }
        public byte[] BinaryContent { get; protected set; }

        private Dictionary<Type, object> _SpecialCastCache = new Dictionary<Type, object>();

        public static Func<ModLoader.ModInfo, Type, LoadedResource, object> SpecialCastCallback = (info, type, res) => {
            if (type == typeof(string)) {
                return res.ReadText();
            } else if (type == typeof(byte[])) {
                return res.ReadBinary();
            } else if (type == typeof(Stream)) {
                return res.Stream;
            } else if (type == typeof(LoadedResource)) {
                return res;
            } else if (type == typeof(Texture2D)) {
                var tex = new Texture2D(0, 0);
                tex.name = res.Path;
                tex.LoadImage(res.ReadBinary());
                tex.filterMode = FilterMode.Point;
                return tex;
            } else if (type == typeof(Animation.Collection)) {
                return new Animation.Collection(info, res.ReadText(), System.IO.Path.GetDirectoryName(res.ResourcePath));
            }
            return null;
        };

        public LoadedResource(string resource_path, string path, StorageType type = StorageType.Stream) {
            ResourcePath = resource_path;
            Path = path;
            Type = type;

            Stream = File.OpenRead(path);
            if (type == StorageType.Stream) return;
            if (type == StorageType.Text) ReadText();
            else if (type == StorageType.Binary) ReadBinary();
        }

        public string ReadText() {
            if (TextContent != null) return TextContent;
            return TextContent = new StreamReader(Stream).ReadToEnd();
        }

        public byte[] ReadBinary() {
            if (BinaryContent != null) return BinaryContent;
            return BinaryContent = new BinaryReader(Stream).ReadAllBytes();
        }

        public T SpecialCast<T>(ModLoader.ModInfo info) {
            object result = null;
            var type = typeof(T);

            if (_SpecialCastCache.TryGetValue(type, out result)) {
                return (T)result;
            }

            result = SpecialCastCallback.Invoke(info, type, this);
            if (result != null) {
                _SpecialCastCache[type] = result;
                return (T)result;
            }

            throw new InvalidOperationException($"Unsupported special type: {typeof(T).FullName}");
        }
    }
}
using System;
using System.Text;
using System.IO;
using UnityEngine;

namespace ETGMod {
    public class LoadedResource {
        public enum StorageType {
            Stream,
            Text,
            Binary
        }

        public StorageType Type { get; protected set; }
        public string Path { get; protected set; }

        public Stream Stream { get; protected set; }
        public string TextContent { get; protected set; }
        public byte[] BinaryContent { get; protected set; }

        public static Func<Type, LoadedResource, object> SpecialCastCallback = (type, res) => {
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
            }
            return null;
        };

        public LoadedResource(string path, StorageType type = StorageType.Stream) {

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

        public T SpecialCast<T>() {
            var invlist = SpecialCastCallback.GetInvocationList();
            object result = null;
            for (int i = 0; i < invlist.Length; i++) {
                var deleg = invlist[i];

                result = deleg.DynamicInvoke(new object[] { typeof(T), this });
                if (result != null) return (T)result;
            }
            throw new InvalidOperationException($"Unsupported special type: {typeof(T).FullName}");
        }
    }
}

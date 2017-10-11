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

        private Texture2D _Texture2D;
        public Texture2D GetTexture2D() {
            if (_Texture2D != null) return _Texture2D;
            return _Texture2D = UnityUtil.LoadTexture2D(ReadBinary(), Path);
        }

        private Animation.Collection _AnimationCollection;
        public Animation.Collection GetAnimationCollection(ModLoader.ModInfo info) {
            if (_AnimationCollection != null) return _AnimationCollection;
            return _AnimationCollection = new Animation.Collection(info, ReadText(), System.IO.Path.GetDirectoryName(ResourcePath));
        }

        private Animation _Animation;
        public Animation GetAnimation(ModLoader.ModInfo info) {
            if (_Animation != null) return _Animation;
            return _Animation = new Animation(info, ReadText(), System.IO.Path.GetDirectoryName(ResourcePath));
        }

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
    }
}
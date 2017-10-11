using System;
using System.IO;
using UnityEngine;

namespace ETGMod {
    public static class UnityUtil {
        public static Texture2D LoadTexture2D(byte[] content, string name) {
            var tex = new Texture2D(0, 0);
            tex.name = name;
            tex.LoadImage(content);
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        public static Texture2D LoadTexture2D(string path) {
            using (var file = File.OpenRead(path)) {
                return LoadTexture2D(new BinaryReader(file).ReadAllBytes(), path);
            }
        }

        public static Color NewColorRGB(int r, int g, int b) {
            return NewColorRGBA(r, g, b, 255);
        }

        public static Color NewColorRGBA(int r, int g, int b, int a) {
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }
     }
}

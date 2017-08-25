using System;
using System.IO;
using UnityEngine;

namespace ETGMod {
    public static class UnityUtil {
        public static Texture2D LoadTexture2D(string path) {
            var tex = new Texture2D(0, 0);
            tex.name = path;
            using (var file = File.OpenRead(path)) {
                tex.LoadImage(new BinaryReader(file).ReadAllBytes());
            }
            tex.filterMode = FilterMode.Point;
            return tex;
        }
    }
}

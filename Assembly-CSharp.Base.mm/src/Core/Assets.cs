using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod asset management.
    /// </summary>
    public static class Assets {

        public static Dictionary<string, ETGModAssetMetadata> Map = new Dictionary<string, ETGModAssetMetadata>();

        public static string RemoveExtension(string file) {
            return file.Substring(0, file.LastIndexOf('.'));
        }

        public static void Crawl(string dir, string root = null) {
            root = root ?? dir;
            string[] files = Directory.GetFiles(dir);
            for (int i = 0; i < files.Length; i++) {
                string file = files[i];
                Map[RemoveExtension(file.Substring(root.Length + 1))] = new ETGModAssetMetadata(file);
            }
            files = Directory.GetDirectories(dir);
            for (int i = 0; i < files.Length; i++) {
                Crawl(files[i], root);
            }
        }

        public static void Hook() {
            ETGModUnityEngineHooks.Load = Load;
            // ETGModUnityEngineHooks.LoadAsync = LoadAsync;
            // ETGModUnityEngineHooks.LoadAll = LoadAll;
            // ETGModUnityEngineHooks.UnloadAsset = UnloadAsset;
        }

        private readonly static Type t_Texture = typeof(Texture);
        public static UnityEngine.Object Load(string path, Type type) {
            if (path == "PlayerCoopCultist") {
                Debug.Log("LOADHOOK Loading resource \"" + path + "\" of (requested) type " + type);

                return Resources.Load(Player.CoopReplacement ?? (path + ETGModUnityEngineHooks.SkipSuffix), type) as GameObject;
            }

            // @RIOKU / TODO crawl through an assets / resource folder and assembly-stored assets, FEZ style

            ETGModAssetMetadata metadata;
            if (!Map.TryGetValue(path, out metadata)) {
                return null;
            }

            // TODO load and parse data from metadata
            
            return null;
        }

        public static void HandleSprite(tk2dBaseSprite sprite) {
            Material[] materials = sprite.Collection.material_1;
            for (int i = 0; i < materials.Length; i++) {
                Texture2D texOrig = materials[i].mainTexture as Texture2D;
                Texture2D tex = new Texture2D(texOrig.width, texOrig.height, texOrig.format, 1 < texOrig.mipmapCount, texOrig.filterMode != FilterMode.Point);
                Color[] data = tex.GetPixels();

                for (int y = 0; y < tex.height; y++) {
                    for (int x = 0; x < tex.width; x++) {
                        int p = x + y * tex.width;
                        data[p] = new Color(x / (float) tex.width, y / (float) tex.height, 1f, 1f);
                    }
                }

                tex.SetPixels(data);
                tex.Apply(true, false);
                materials[i].mainTexture = tex;
            }
        }

    }

    public static tk2dBaseSprite Handle(this tk2dBaseSprite sprite) {
        Assets.HandleSprite(sprite);
        return sprite;
    }

}

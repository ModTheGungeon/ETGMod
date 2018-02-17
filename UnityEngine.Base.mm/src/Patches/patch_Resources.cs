#pragma warning disable 0626
#pragma warning disable 0649
#pragma warning disable 0436

using System.Diagnostics;
using MonoMod;
using UnityEngine;

namespace UnityEnginePatches {
    [MonoModPatch("UnityEngine.Resources")]
    internal sealed class ResourcesPatch {
        // hijacking resources.load and such (not actually used right now)

        [MonoModOriginal]
        public static extern Object Ooad(string path, System.Type systemTypeInstance);
        [MonoModOriginalName("Ooad")]
        public static Object Load(string path, System.Type type) {
            return Ooad(path, type);
        }

        [MonoModOriginal]
        public static extern ResourceRequest OoadAsync(string path, System.Type type);
        [MonoModOriginalName("OoadAsync")]
        public static ResourceRequest LoadAsync(string path, System.Type type) {
            return OoadAsync(path, type);
        }

        [MonoModOriginal]
        public static extern Object[] OoadAll(string path, System.Type systemTypeInstance);
        [MonoModOriginalName("OoadAll")]
        public static Object[] LoadAll(string path, System.Type type) {
            return OoadAll(path, type);
        }

        [MonoModOriginal]
        public static extern Object OetBuiltinResource(System.Type type, string path);
        [MonoModOriginalName("OetBuiltinResource")]
        public static Object GetBuiltinResource(System.Type type, string path) {
            return OetBuiltinResource(type, path);
        }

        [MonoModOriginal]
        public static extern void OnloadAsset(Object assetToUnload);
        [MonoModOriginalName("OnloadAsset")]
        public static void UnloadAsset(Object asset) {
            OnloadAsset(asset);
        }

        [MonoModOriginal]
        public static extern AsyncOperation OnloadUnusedAssets();
        [MonoModOriginalName("OnloadUnusedAssets")]
        public static AsyncOperation UnloadUnusedAssets() {
            return OnloadUnusedAssets();
        }
    }
}

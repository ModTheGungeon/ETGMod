#pragma warning disable 0626
#pragma warning disable 0649
using System.Diagnostics;
using MonoMod;

namespace UnityEngine {
    internal sealed class patch_Resources {

        [MonoModOriginal] public static extern Object Ooad(string path, System.Type systemTypeInstance);
        [MonoModOriginalName("Ooad")]
        public static Object Load(string path, System.Type type) {
            if (path.EndsWith(ETGModUnityEngineHooks.SkipSuffix)) {
                return Ooad(path.Substring(0, path.Length - ETGModUnityEngineHooks.SkipSuffix.Length), type);
            }
            return ETGModUnityEngineHooks.Load?.Invoke(path, type) ?? Ooad(path, type);
        }

        [MonoModOriginal] public static extern ResourceRequest OoadAsync(string path, System.Type type);
        [MonoModOriginalName("OoadAsync")]
        public static ResourceRequest LoadAsync(string path, System.Type type) {
            if (path.EndsWith(ETGModUnityEngineHooks.SkipSuffix)) {
                return LoadAsync(path.Substring(0, path.Length - ETGModUnityEngineHooks.SkipSuffix.Length), type);
            }
            return ETGModUnityEngineHooks.LoadAsync?.Invoke(path, type) ?? OoadAsync(path, type);
        }

        [MonoModOriginal] public static extern Object[] OoadAll(string path, System.Type systemTypeInstance);
        [MonoModOriginalName("OoadAll")]
        public static Object[] LoadAll(string path, System.Type type) {
            if (path.EndsWith(ETGModUnityEngineHooks.SkipSuffix)) {
                return OoadAll(path.Substring(0, path.Length - ETGModUnityEngineHooks.SkipSuffix.Length), type);
            }
            return ETGModUnityEngineHooks.LoadAll?.Invoke(path, type) ?? OoadAll(path, type);
        }

        [MonoModOriginal] public static extern Object OetBuiltinResource(System.Type type, string path);
        [MonoModOriginalName("OetBuiltinResource")]
        public static Object GetBuiltinResource(System.Type type, string path) {
            return OetBuiltinResource(type, path);
        }

        [MonoModOriginal] public static extern void OnloadAsset(Object assetToUnload);
        [MonoModOriginalName("OnloadAsset")]
        public static void UnloadAsset(Object asset) {
            if (ETGModUnityEngineHooks.UnloadAsset(asset)) {
                return;
            }
            OnloadAsset(asset);
        }

        [MonoModOriginal] public static extern AsyncOperation OnloadUnusedAssets();
        [MonoModOriginalName("OnloadUnusedAssets")]
        public static AsyncOperation UnloadUnusedAssets() {
            return OnloadUnusedAssets();
        }

    }
}

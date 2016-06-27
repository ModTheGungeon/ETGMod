using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngineInternal;
using MonoMod;

namespace UnityEngine {
    public sealed class patch_Resources {

        [/*WrapperlessIcall, */TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern Object Ooad(string path, System.Type systemTypeInstance);
        [MonoModOriginalName("Ooad")]
        public static Object Load(string path, System.Type type) {
            Log("HOOKED LOAD");
            Log(System.Environment.StackTrace.ToString());

            return Ooad(path, type);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern ResourceRequest OoadAsync(string path, System.Type type);
        [MonoModOriginalName("OoadAsync")]
        public static ResourceRequest LoadAsync(string path, System.Type type) {
            Log("HOOKED WHAT, LOADASYNC");
            Log(System.Environment.StackTrace.ToString());

            return OoadAsync(path, type);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern Object[] OoadAll(string path, System.Type systemTypeInstance);
        [MonoModOriginalName("OoadAll")]
        public static Object[] LoadAll(string path, System.Type type) {
            Log("HOOKED WHY, LOADALL");
            Log(System.Environment.StackTrace.ToString());

            return OoadAll(path, type);
        }

        [/*WrapperlessIcall,*/ TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern Object OetBuiltinResource(System.Type type, string path);
        [MonoModOriginalName("OetBuiltinResource")]
        public static Object GetBuiltinResource(System.Type type, string path) {
            Log("HOOKED HOW, GETBUILTINRESOURCE");
            Log(System.Environment.StackTrace.ToString());

            return OetBuiltinResource(type, path);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern void OnloadAsset(Object assetToUnload);
        [MonoModOriginalName("OnloadAsset")]
        public static void UnloadAsset(Object asset) {
            Log("HOOKED WHEN, UNLOADASSET");
            Log(System.Environment.StackTrace.ToString());

            OnloadAsset(asset);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern AsyncOperation OnloadUnusedAssets();
        [MonoModOriginalName("OnloadUnusedAssets")]
        public static AsyncOperation UnloadUnusedAssets() {
            Log("HOOKED WHATEVER, UNLOADUNUSEDASSETS");
            Log(System.Environment.StackTrace.ToString());

            return OnloadUnusedAssets();
        }

        private static void Log(string text) {
            if (ETGModAssetMetadata.LogHook != null) {
                ETGModAssetMetadata.LogHook(text);
            }
        }

    }
}

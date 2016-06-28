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

            return Ooad(path, type);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern ResourceRequest OoadAsync(string path, System.Type type);
        [MonoModOriginalName("OoadAsync")]
        public static ResourceRequest LoadAsync(string path, System.Type type) {

            return OoadAsync(path, type);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern Object[] OoadAll(string path, System.Type systemTypeInstance);
        [MonoModOriginalName("OoadAll")]
        public static Object[] LoadAll(string path, System.Type type) {

            return OoadAll(path, type);
        }

        [/*WrapperlessIcall,*/ TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern Object OetBuiltinResource(System.Type type, string path);
        [MonoModOriginalName("OetBuiltinResource")]
        public static Object GetBuiltinResource(System.Type type, string path) {

            return OetBuiltinResource(type, path);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern void OnloadAsset(Object assetToUnload);
        [MonoModOriginalName("OnloadAsset")]
        public static void UnloadAsset(Object asset) {

            OnloadAsset(asset);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [MonoModOriginal] public static extern AsyncOperation OnloadUnusedAssets();
        [MonoModOriginalName("OnloadUnusedAssets")]
        public static AsyncOperation UnloadUnusedAssets() {

            return OnloadUnusedAssets();
        }

    }
}

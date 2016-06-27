using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngineInternal;

namespace UnityEngine {
    public sealed class patch_Resources {

        [/*WrapperlessIcall, */TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern Object orig_Load(string path, System.Type systemTypeInstance);
        public static Object Load(string path, System.Type type) {
            Log("HOOKED LOAD");
            Log(System.Environment.StackTrace.ToString());

            return orig_Load(path, type);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern ResourceRequest orig_LoadAsync(string path, System.Type type);
        public static ResourceRequest LoadAsync(string path, System.Type type) {
            Log("HOOKED WHAT, LOADASYNC");
            Log(System.Environment.StackTrace.ToString());

            return orig_LoadAsync(path, type);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern Object[] orig_LoadAll(string path, System.Type systemTypeInstance);
        public static Object[] LoadAll(string path, System.Type type) {
            Log("HOOKED WHY, LOADALL");
            Log(System.Environment.StackTrace.ToString());

            return orig_LoadAll(path, type);
        }

        [/*WrapperlessIcall,*/ TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern Object orig_GetBuiltinResource(System.Type type, string path);
        public static Object GetBuiltinResource(System.Type type, string path) {
            Log("HOOKED HOW, GETBUILTINRESOURCE");
            Log(System.Environment.StackTrace.ToString());

            return orig_GetBuiltinResource(type, path);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void orig_UnloadAsset(Object assetToUnload);
        public static void UnloadAsset(Object asset) {
            Log("HOOKED WHEN, UNLOADASSET");
            Log(System.Environment.StackTrace.ToString());

            orig_UnloadAsset(asset);
        }

        //[WrapperlessIcall]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern AsyncOperation orig_UnloadUnusedAssets();
        public static AsyncOperation UnloadUnusedAssets() {
            Log("HOOKED WHATEVER, UNLOADUNUSEDASSETS");
            Log(System.Environment.StackTrace.ToString());

            return orig_UnloadUnusedAssets();
        }

        private static void Log(string text) {
            if (ETGModAssetMetadata.LogHook != null) {
                ETGModAssetMetadata.LogHook(text);
            }
        }

    }
}

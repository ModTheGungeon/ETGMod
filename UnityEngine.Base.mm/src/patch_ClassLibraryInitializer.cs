#pragma warning disable 0626
#pragma warning disable 0649

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngineInternal;
using MonoMod;

namespace UnityEngine {
    internal static class patch_ClassLibraryInitializer {

        private static extern void orig_Init();
        // Seemingly first piece of managed code running in Unity
        private static void Init() {
            orig_Init();
            MonoDebug.Force();
        }
    }
}

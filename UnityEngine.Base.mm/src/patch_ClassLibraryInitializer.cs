#pragma warning disable 0626
#pragma warning disable 0649

namespace UnityEngine {
    internal static class patch_ClassLibraryInitializer {

        private static extern void orig_Init();
        // Seemingly first piece of managed code running in Unity
        private static void Init() {
            orig_Init();
            try {
                MonoDebug.Force();
            } catch (System.Exception e) {
                Debug.Log("ClassLibraryInitializer called MonoDebug.Force and it sudoku'd.");
                Debug.Log(e);
            }
        }
    }
}

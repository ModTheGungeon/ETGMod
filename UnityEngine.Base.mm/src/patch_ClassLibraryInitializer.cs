#pragma warning disable 0626
#pragma warning disable 0649

using System;

namespace UnityEngine {
    internal static class patch_ClassLibraryInitializer {

        private delegate string[] d_mono_runtime_get_main_args(); //ret MonoArray*

        private static extern void orig_Init();
        private static void Init() {
            orig_Init();

            string[] args = PInvokeHelper.Mono.GetDelegate<d_mono_runtime_get_main_args>()();
            bool debuggerClient = false;
            for (int i = 1; i < args.Length; i++) {
                string arg = args[i];
                if (arg == "--debugger-client") {
                    debuggerClient = true;
                }
            }

            try {
                if (debuggerClient) MonoDebug.SetupDebuggerAgent();
                MonoDebug.Init();
                if (debuggerClient) MonoDebug.InitDebuggerAgent();
            } catch (Exception e) {
                Debug.Log("Called MonoDebug and it sudoku'd.");
                Debug.Log(e);
            }
        }

    }
}

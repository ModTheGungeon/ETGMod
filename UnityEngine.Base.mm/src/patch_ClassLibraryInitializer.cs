#pragma warning disable 0626
#pragma warning disable 0649

using System;
using System.Runtime.InteropServices;

namespace UnityEngine {
    internal static class patch_ClassLibraryInitializer {

        private delegate string[] d_mono_runtime_get_main_args();
		[DllImport("mono")]
		private static extern string[] mono_runtime_get_main_args(); //ret MonoArray*

        private static extern void orig_Init();
        // Seemingly first piece of managed code running in Unity
        private static void Init() {
            orig_Init();
            
			string[] args;
			if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
				args = mono_runtime_get_main_args();
			} else {
				args = MonoDebug.GetDelegate<d_mono_runtime_get_main_args>("mono_runtime_get_main_args")();
			}
			bool debuggerClient = false;
			// 0 is the binary path
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

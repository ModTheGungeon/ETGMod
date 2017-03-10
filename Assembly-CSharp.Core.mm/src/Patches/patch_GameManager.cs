#pragma warning disable 0626
#pragma warning disable 0649

/////////////////////
//// ENTRY POINT ////
/////////////////////

using System;
using ETGMod;
using UnityEngine;
using System.Reflection;

internal class patch_GameManager : GameManager {
    protected extern void orig_Awake();
    private void Awake() {
        ETGMod.DefaultLogger.Info("Mod the Gungeon entry point");

        var asm = Assembly.GetExecutingAssembly();
        var types = asm.GetTypes();
        for (int i = 0; i < types.Length; i++) {
            var type = types[i];
            if (typeof(Backend).IsAssignableFrom(type) && !type.IsAbstract) {
                var method = type.GetMethod("Init", BindingFlags.Static | BindingFlags.Public);
                if (method == null) {
                    DefaultLogger.Error($"Failed getting Init method for backend {type.Name}. Ignoring.");
                    continue;
                }

                var version = (Version)type.GetField("Version")?.GetValue(null);
                if (version == null) {
                    DefaultLogger.Error($"Failed getting Version for backend {type.Name}. Defaulting to 1.0.0.");
                    version = new Version(1, 0, 0);
                }

                Backend.AllBackends.Add(new Backend.Info {
                    Name = type.Name,
                    Version = version,
                    Type = type
                });

                DefaultLogger.Info($"Initializing backend {type.Name}");
                method.Invoke(null, null);
            }
        }

        orig_Awake();
    }
}

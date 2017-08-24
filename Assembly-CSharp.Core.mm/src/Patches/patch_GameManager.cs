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
        Loader.Logger.Info("Mod the Gungeon entry point");
        Backend.GameObject = new GameObject("Mod the Gungeon");

        var asm = Assembly.GetExecutingAssembly();
        var types = asm.GetTypes();
        for (int i = 0; i < types.Length; i++) {
            var type = types[i];
            if (type.IsSubclassOf(typeof(Backend))) {
                var backend = (Backend)Backend.GameObject.AddComponent(type);

                DontDestroyOnLoad(backend);

                Backend.AllBackends.Add(new Backend.Info {
                    Name = type.Name,
                    StringVersion = backend.StringVersion,
                    Version = backend.Version,
                    Type = type,
                    Instance = backend
                });

                backend.NoBackendsLoadedYet();
            }
        }

        for (int i = 0; i < Backend.AllBackends.Count; i++) {
            var backend = Backend.AllBackends[i];
            Loader.Logger.Info($"Initializing backend {backend.Name} {backend.StringVersion}");
            backend.Instance.Loaded();
        }

        for (int i = 0; i < Backend.AllBackends.Count; i++) {
            Backend.AllBackends[i].Instance.AllBackendsLoaded();
        }

        orig_Awake();
    }
}

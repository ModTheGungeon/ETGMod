#pragma warning disable 0626
#pragma warning disable 0649

/////////////////////
//// ENTRY POINT ////
/////////////////////

using System;
using ETGMod;
using UnityEngine;
using System.Reflection;
using MonoMod;

namespace ETGMod.CorePatches {
    [MonoModPatch("global::GameManager")]
    internal class GameManager : global::GameManager {
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

                    try {
                        backend.NoBackendsLoadedYet();
                    } catch (Exception e) {
                        Loader.Logger.Error($"Exception while pre-loading backend {type.Name}: [{e.GetType().Name}] {e.Message}");
                        foreach (var l in e.StackTrace.Split('\n')) Loader.Logger.ErrorIndent(l);
                    }
                }
            }

            for (int i = 0; i < Backend.AllBackends.Count; i++) {
                var backend = Backend.AllBackends[i];
                Loader.Logger.Info($"Initializing backend {backend.Name} {backend.StringVersion}");
                try {
                    backend.Instance.Loaded();
                } catch (Exception e) {
                    Loader.Logger.Error($"Exception while loading backend {backend.Name}: [{e.GetType().Name}] {e.Message}");
                    foreach (var l in e.StackTrace.Split('\n')) Loader.Logger.ErrorIndent(l);
                }
            }

            for (int i = 0; i < Backend.AllBackends.Count; i++) {
                try {
                    Backend.AllBackends[i].Instance.AllBackendsLoaded();
                } catch (Exception e) {
                    Loader.Logger.Error($"Exception while post-loading backend {Backend.AllBackends[i].Name}: [{e.GetType().Name}] {e.Message}");
                    foreach (var l in e.StackTrace.Split('\n')) Loader.Logger.ErrorIndent(l);
                }
            }

            orig_Awake();
        }
    }
}

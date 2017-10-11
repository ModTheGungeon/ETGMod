#pragma warning disable 0626
#pragma warning disable 0649

using System;
using MonoMod;

namespace ETGMod.BasePatches {
    [MonoModPatch("global::MainMenuFoyerController")]
    public class MainMenuFoyerController : global::MainMenuFoyerController {
        public extern void AddLine(string s);

        public void Start() {
            var word = ETGMod.ModLoader.LoadedMods.Count == 1 ? "mod" : "mods";
            AddLine($"{ETGMod.ModLoader.LoadedMods.Count} {word} loaded");
        }

    }
}
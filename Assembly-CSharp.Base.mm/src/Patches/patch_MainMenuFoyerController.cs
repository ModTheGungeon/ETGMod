#pragma warning disable 0626
#pragma warning disable 0649

using System;

public class patch_MainMenuFoyerController : MainMenuFoyerController {
    public extern void AddLine(string s);
        
    public void Start() {
        var word = ETGMod.ETGMod.ModLoader.LoadedMods.Count == 1 ? "mod" : "mods";
        AddLine($"{ETGMod.ETGMod.ModLoader.LoadedMods.Count} {word} loaded");
    }
}

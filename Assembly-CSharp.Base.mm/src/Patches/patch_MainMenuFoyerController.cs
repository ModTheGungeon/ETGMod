#pragma warning disable 0626
#pragma warning disable 0649

using System;

public class patch_MainMenuFoyerController : MainMenuFoyerController {
    private extern void orig_Awake();
    private void Awake() {
        orig_Awake();

        VersionLabel.Text = $"Gungeon {VersionLabel.Text}";
        for (int i = 0; i < ETGMod.Backend.AllBackends.Count; i++) {
            var backend = ETGMod.Backend.AllBackends[i];
            _AddLine($"{backend.Name} {backend.StringVersion}");
        }

        var word = ETGMod.ETGMod.ModLoader.LoadedMods.Count == 1 ? "mod" : "mods";
        _AddLine($"{ETGMod.ETGMod.ModLoader.LoadedMods.Count} {word} loaded");
    }

    private void _AddLine(string line) {
        if (VersionLabel.Text.Length > 0) {
            VersionLabel.Text += $"\n{line}";
        } else {
            VersionLabel.Text = line;
        }
        VersionLabel.Position = new UnityEngine.Vector3(VersionLabel.Position.x, VersionLabel.Position.y - VersionLabel.Font.LineHeight);
    }
}

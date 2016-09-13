#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

internal class patch_SteamManager : SteamManager {
    protected extern void orig_Awake();
    private void Awake() {
        if (ETGMod.Platform.DisableSteam) {
            return;
        }

        orig_Awake();
    }
}

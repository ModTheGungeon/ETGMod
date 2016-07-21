#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

internal class patch_MainMenuFoyerController : MainMenuFoyerController {

    protected extern void orig_Awake();
    protected void Awake() {
        orig_Awake();

        VersionLabel.Text += " | Mod the Gungeon " + ETGMod.BaseVersion;

    }

}

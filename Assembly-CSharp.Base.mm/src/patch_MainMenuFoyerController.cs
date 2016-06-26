#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

public class patch_MainMenuFoyerController : MainMenuFoyerController {

    public extern void orig_method_0();
    public new void method_0() {
        orig_method_0();

        dfLabel_0.Text += " | Mod the Gungeon " + ETGMod.BaseVersion;

        // TODO add mod menu button here
    }

}

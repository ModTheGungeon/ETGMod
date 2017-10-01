#pragma warning disable 0626
#pragma warning disable 0649

using System;
using SGUI;
using MonoMod;

namespace ETGMod.GUI.Patches {
    [MonoModPatch("global::dfButton")]
    public class dfButton : global::dfButton {
        protected extern void orig_OnKeyPress(dfKeyEventArgs args);
        protected override void OnKeyPress(dfKeyEventArgs args) {
            if (SGUIRoot.Main.Backend.LastKeyEventConsumed) return;

            orig_OnKeyPress(args);
        }
    }
}
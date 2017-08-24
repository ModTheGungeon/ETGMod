#pragma warning disable 0626
#pragma warning disable 0649

using System;
using SGUI;

public class patch_dfButton : dfButton {

    protected extern void orig_OnKeyPress(dfKeyEventArgs args);
    protected override void OnKeyPress(dfKeyEventArgs args) {
        if (SGUIRoot.Main.Backend.LastKeyEventConsumed) return;

        orig_OnKeyPress(args);
    }
}

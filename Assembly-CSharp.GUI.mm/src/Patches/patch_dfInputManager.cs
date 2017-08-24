#pragma warning disable 0626
#pragma warning disable 0649

using System;
using SGUI;

public class patch_dfInputManager : dfInputManager {
    bool _etgmod_sgui_patched = false;

    public extern void orig_OnEnable();
    public new void OnEnable() {
        orig_OnEnable();

        if (_etgmod_sgui_patched) return;
        _etgmod_sgui_patched = true;

        ETGMod.GUI.GUI.Logger.Debug($"Patching dfInputManager adapter with SGUIDFInput");
        Adapter = new SGUIDFInput(Adapter);
    }
}
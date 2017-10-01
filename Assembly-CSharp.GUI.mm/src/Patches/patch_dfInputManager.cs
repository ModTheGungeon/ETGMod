#pragma warning disable 0626
#pragma warning disable 0649

using System;
using SGUI;
using MonoMod;

namespace ETGMod.GUI.Patches {
    [MonoModPatch("global::dfInputManager")]
    public class dfInputManager : global::dfInputManager {
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
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class patch_FoyerCharacterSelectFlag : FoyerCharacterSelectFlag {

    public extern void orig_Update();
    public void Update() {
        if (MultiplayerManager.isPlayingMultiplayer&&!GameManager.Instance.SecondaryPlayer) {
            ToggleSelf(true);
        } else {
            orig_Update();
        }
    }

    private extern void orig_ToggleSelf(bool activate);
    private void ToggleSelf(bool activate) {
        orig_ToggleSelf(activate);
    }
}


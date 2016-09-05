#pragma warning disable 0626
#pragma warning disable 0649

using MonoMod;
using UnityEngine;
using System;

internal class patch_BossCardUIController : BossCardUIController {

    private extern void orig_ToggleCoreVisiblity(bool visible);
    private void ToggleCoreVisiblity(bool visible) {
        if (!visible) {
            for (int i = 0; i < parallaxSprites.Count; ) {
                if (parallaxSprites[i] != null) {
                    ++i;
                    continue;
                }
                // FIXES BROKEN BOSS CARD.
                parallaxSprites.RemoveAt(i);
                parallaxSpeeds.RemoveAt(i);
                parallaxStarts.RemoveAt(i);
                parallaxEnds.RemoveAt(i);
            }
        }
        orig_ToggleCoreVisiblity(visible);
    }

}

#pragma warning disable 0626
#pragma warning disable 0649

using System;

internal class patch_PlayerConsumables : PlayerConsumables {
    public extern bool orig_get_InfiniteKeys();
    public bool get_InfiniteKeys() {
        if (ETGMod.Player.InfiniteKeys != null && ETGMod.Player.InfiniteKeys.HasValue) {
            return ETGMod.Player.InfiniteKeys.Value;
        } else {
            return orig_get_InfiniteKeys();
        }
    }
}

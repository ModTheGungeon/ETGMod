#pragma warning disable 0626
#pragma warning disable 0649

internal class patch_PlayerConsumables : PlayerConsumables {
    
    public extern bool orig_get_InfiniteKeys();
    public bool get_InfiniteKeys() {
        return ETGMod.Player.InfiniteKeys ?? orig_get_InfiniteKeys();
    }

}

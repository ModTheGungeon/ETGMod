#pragma warning disable 0626
#pragma warning disable 0649

using Dungeonator;

internal class patch_Chest : Chest {

    public extern static Chest orig_Spawn(Chest chestPrefab, IntVector2 basePosition, RoomHandler room, bool ForceNoMimic = false);
    public new static Chest Spawn(Chest chestPrefab, IntVector2 basePosition, RoomHandler room, bool ForceNoMimic = false) {
        Chest returnValue = orig_Spawn(chestPrefab, basePosition, room, ForceNoMimic);
        ETGMod.Chest.OnPostSpawn?.Invoke(returnValue);
        return returnValue;
    }
    protected extern void orig_Open(PlayerController player);
    protected new void Open(PlayerController player) {
        if (ETGMod.Chest.OnPreOpen.RunHook(true, this, player)) {
            orig_Open(player);
            ETGMod.Chest.OnPostOpen?.Invoke(this, player);
        }
    }

}

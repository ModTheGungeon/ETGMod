#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

internal class patch_GameManager : GameManager {

    public static extern GameObject orig_get_CoopPlayerPrefabForNewGame();
    public static GameObject get_CoopPlayerPrefabForNewGame() {
        // Doesn't actually replace the Cultist
        if (ETGMod.Player.CoopReplacement != null) {
            return Resources.Load(ETGMod.Player.CoopReplacement) as GameObject;
        }
        return orig_get_CoopPlayerPrefabForNewGame();
    }

    protected extern void orig_Awake();
    private void Awake() {
        orig_Awake();

        if (ETGModMainBehaviour.Instance == null) {
            ETGModMainBehaviour.Instance = new GameObject("ETGMod Manager").AddComponent<ETGModMainBehaviour>();
        }
    }

}

#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

public class patch_GameManager : GameManager {

    public static extern GameObject orig_get_GameObject_1();
    public static GameObject get_GameObject_1() {
        if (ETGMod.Player.CoopReplacement != null) {
            return Resources.Load(ETGMod.Player.CoopReplacement) as GameObject;
        }
        return orig_get_GameObject_1();
    }

    protected extern void orig_Awake();
    private void Awake() {
        orig_Awake();

        if (ETGModManager.Instance == null) {
            new GameObject("ETGMod Manager").AddComponent<ETGModManager>();
        }
    }

}

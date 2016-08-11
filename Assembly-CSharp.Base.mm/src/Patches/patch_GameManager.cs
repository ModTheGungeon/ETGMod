#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

internal class patch_GameManager : GameManager {
    protected extern void orig_Awake();
    private void Awake() {
        if (ETGModMainBehaviour.Instance == null) {
            ETGModMainBehaviour.Instance = new GameObject("ETGMod Manager").AddComponent<ETGModMainBehaviour>();
        }

        orig_Awake();
    }
}

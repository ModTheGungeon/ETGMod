#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

public class patch_GameManager : GameManager {

    protected extern void orig_Awake();
    private void Awake() {
        orig_Awake();

        // Gets called more than once in the game's lifetime...
        ETGMod.Start();
    }

    protected extern void orig_Update();
    protected new void Update() {
        orig_Update();

        ETGMod.Update();
    }

}

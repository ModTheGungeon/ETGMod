#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

public class patch_GameManager : GameManager {

    protected extern void orig_Update();
    protected new void Update() {
        orig_Update();

        // TODO: Find better start injection point (ETGMod's being started in Update if not started already.)
        ETGMod.Update();
    }

}

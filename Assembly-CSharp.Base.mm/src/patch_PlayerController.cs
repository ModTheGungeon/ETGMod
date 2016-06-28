#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

public class patch_PlayerController : PlayerController {

    public extern void orig_Awake();
    public new void Awake() {
        orig_Awake();

        Tk2dBaseSprite_0.Handle();
    }

}

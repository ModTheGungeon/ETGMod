#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

public abstract class patch_tk2dBaseSprite : tk2dBaseSprite {

    public extern void orig_Awake();
    public new void Awake() {
        orig_Awake();

        /*
        Debug.Log("SPRITEHOOK: Hooked sprite " + name);
        Transform parent = transform;
        while ((parent = parent.parent) != null) {
            Debug.Log("SPRITEHOOK: Parent: " + parent.name);
        }
        */
    }

}

#pragma warning disable 0626
#pragma warning disable 0649

using System;
using UnityEngine;

public class patch_GameObject {
    extern public Component orig_AddComponent(Type componentType);
    public Component AddComponent(Type componentType) {
        var component = orig_AddComponent(componentType);
        if (component is tk2dSpriteAnimator) {
            ((patch_tk2dSpriteAnimator)component).TexModPatch();
        } else if (component is tk2dSpriteCollectionData) {
            ((patch_tk2dSpriteCollectionData)component).TexModPatch();
        }
        return component;
    }
}

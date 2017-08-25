#pragma warning disable 0626
#pragma warning disable 0649

using System;
using UnityEngine;

public abstract class patch_tk2dBaseSprite : tk2dBaseSprite {
    private void Start() {
        TexModPatch();
    }

    public void TexModPatch() {
        if (Collection != null) {
            ((patch_tk2dSpriteCollectionData)Collection).TexModPatch();
        }
        if (spriteAnimator != null) ((patch_tk2dSpriteAnimator)spriteAnimator).TexModPatch();

        UnityEngine.Object.Destroy(this, 1f);
        return;

        Build();
        UpdateMaterial();
        UpdateCollider();
        UpdateColors();
        UpdateVertices();
    }

    public void Update() {
        if (Collection.spriteCollectionName == "TitleScreenCollection") {
            Console.WriteLine("HIIIIIIIIIII");
        }
    }
}

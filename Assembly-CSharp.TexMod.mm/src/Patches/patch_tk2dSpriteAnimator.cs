#pragma warning disable 0626
#pragma warning disable 0649

using System;
using MonoMod;
using TexMod;
using System.Collections.Generic;
using ETGMod;

abstract public class patch_tk2dSpriteAnimator : tk2dSpriteAnimator {
    private bool _texmod_init = false;

    extern private void orig_Start();
    private void Start() {
        orig_Start();

        TexModPatch();
    }

    public void TexModPatch() {
        if (_texmod_init) return;
        _texmod_init = true;

        var anim_name = TexMod.TexMod.GenerateSpriteAnimationName(gameObject.GetComponent<tk2dBaseSprite>());

        Animation anim;
        if (TexMod.TexMod.AnimationMap.TryGetValue(anim_name, out anim)) {
            TexMod.TexMod.Logger.Debug($"Found patch animation '{anim_name}'");

            anim.PatchAnimator(spriteAnimator);
        }
    }
}

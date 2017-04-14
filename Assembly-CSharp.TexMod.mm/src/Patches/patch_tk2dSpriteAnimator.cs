#pragma warning disable 0626
#pragma warning disable 0649

using System;
using MonoMod;
using TexMod;
using System.Collections.Generic;
using ETGMod;

abstract public class patch_tk2dBaseSprite : tk2dBaseSprite {
    private bool _texmod_init = false;
    private bool _texmod_nonanimated = false;
    private Animation _texmod_anim;

    private void _PatchNonAnimated() {
        var def = Collection.spriteDefinitions[_spriteId];
        var clip_id = _texmod_anim.GetClipID(def.name);
        if (clip_id != null) {
            TexMod.TexMod.Logger.Debug($"Patching non-animated");
            spriteAnimator = _texmod_anim.ApplyAnimator(gameObject);
            spriteAnimator.playAutomatically = true;
            spriteAnimator.DefaultClipId = clip_id.Value;
            _spriteId = spriteAnimator.Library.clips[0].frames[0].spriteId;
        } else {
            if (spriteAnimator != null) {
                TexMod.TexMod.Logger.Debug($"Unpatching non-animated");
                Destroy(spriteAnimator.Library);
                Destroy(spriteAnimator);
            } else {
                TexMod.TexMod.Logger.Debug($"No need to unpatch non-animated");
            }
        }
        UpdateGeometry();
        UpdateVertices();
        UpdateMaterial();
        UpdateCollider();
        SpriteOutlineManager.HandleSpriteChanged(this);
        UpdateZDepth();
    }

    extern public int orig_set_spriteId(int value);
    public void patch_set_spriteId(int value) {
        orig_set_spriteId(value);
        _spriteId = value;
        if (_texmod_nonanimated) _PatchNonAnimated();
    }

    private void Start() {
        if (_texmod_init) return;
        _texmod_init = true;

        var anim_name = TexMod.TexMod.GenerateSpriteAnimationName(this);

        if (spriteAnimator == null) {
            TexMod.TexMod.Logger.Debug($"BASESPRITE {TexMod.TexMod.GenerateSpriteAnimationName(this)} {Collection.spriteDefinitions[spriteId].name}");
        } else {
            TexMod.TexMod.Logger.Debug($"BASESPRITE {TexMod.TexMod.GenerateSpriteAnimationName(this)}");
        }

        Animation anim;
        if (TexMod.TexMod.AnimationMap.TryGetValue(anim_name, out anim)) {
            TexMod.TexMod.Logger.Debug($"Found patch animation '{anim_name}'");

            if (spriteAnimator == null) { // NOT ANIMATED
                _texmod_nonanimated = true;
                _texmod_anim = anim;

                _PatchNonAnimated();
            } else { // ANIMATED
                TexMod.TexMod.Logger.Debug($"Already has animator - PATCHING!");
                anim.PatchAnimator(spriteAnimator);
            }
        }
    }
}

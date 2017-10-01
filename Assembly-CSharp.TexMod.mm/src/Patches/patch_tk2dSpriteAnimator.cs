#pragma warning disable 0626
#pragma warning disable 0649

using System;
using MonoMod;
using TexMod;
using System.Collections.Generic;
using ETGMod;

abstract public class patch_tk2dSpriteAnimator : tk2dSpriteAnimator {
    private bool _texmod_init = false;

    private tk2dSpriteAnimator _texmod_saved_animator;
    private tk2dSpriteCollectionData _texmod_saved_collection;
    private tk2dSpriteDefinition[] _texmod_saved_definitions;
    private tk2dSpriteAnimationClip[] _texmod_saved_clips;
    private tk2dSpriteAnimationFrame[][] _texmod_saved_frames;

    extern private void orig_Start();
    private void Start() {
        orig_Start();

        TexModPatch();
    }

    public void TexModUnpatch() {
        if (!_texmod_init) return;

        _texmod_init = false;
        var collection = gameObject.GetComponent<tk2dBaseSprite>().Collection;

        Animation.CopyAnimator(_texmod_saved_animator, this);
        Animation.CopyCollection(_texmod_saved_collection, collection);

        Library.clips = _texmod_saved_clips;
        for (int i = 0; i < Library.clips.Length; i++) {
            Library.clips[i].frames = _texmod_saved_frames[i];
        }

        collection.spriteDefinitions = _texmod_saved_definitions;
    }

    public void TexModPatch() {
        if (_texmod_init) return;
        _texmod_init = true;

        TexMod.TexMod.AddPatchedObject(this);

        var collection = gameObject.GetComponent<tk2dBaseSprite>().Collection;

        _texmod_saved_animator = new tk2dSpriteAnimator();
        _texmod_saved_collection = new tk2dSpriteCollectionData();
        _texmod_saved_definitions = (tk2dSpriteDefinition[])collection.spriteDefinitions.Clone();
        _texmod_saved_clips = (tk2dSpriteAnimationClip[])Library.clips.Clone();

        _texmod_saved_frames = (tk2dSpriteAnimationFrame[][])Array.CreateInstance(typeof(tk2dSpriteAnimationFrame[]), Library.clips.Length);
        for (int i = 0; i < Library.clips.Length; i++) {
            _texmod_saved_frames[i] = (tk2dSpriteAnimationFrame[])Library.clips[i].frames.Clone();
        }
        Animation.CopyAnimator(this, _texmod_saved_animator);
        Animation.CopyCollection(collection, _texmod_saved_collection);


        var anim_name = TexMod.TexMod.GenerateSpriteAnimationName(gameObject.GetComponent<tk2dBaseSprite>());

        Animation anim;
        if (TexMod.TexMod.AnimationMap.TryGetValue(anim_name, out anim)) {
            TexMod.TexMod.Logger.Debug($"Found patch animation '{anim_name}'");

            anim.PatchAnimator(spriteAnimator);
        }
    }
}

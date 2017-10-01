#pragma warning disable 0626
#pragma warning disable 0649

using System;
using ETGMod;

public class patch_tk2dSpriteCollectionData : tk2dSpriteCollectionData {
    private bool _texmod_init = false;
    private tk2dSpriteCollectionData _texmod_saved_collection;

    private void Start() {
        TexModPatch();
    }

    public void TexModUnpatch() {
        if (!_texmod_init) return;

        _texmod_init = false;
        Animation.CopyCollection(_texmod_saved_collection, this);
    }

    public void TexModPatch() {
        if (_texmod_init) return;
        _texmod_init = true;

        TexMod.TexMod.AddPatchedObject(this);

        _texmod_saved_collection = new tk2dSpriteCollectionData();
        Animation.CopyCollection(this, _texmod_saved_collection);

        Animation.Collection collection;
        if (TexMod.TexMod.CollectionMap.TryGetValue(name, out collection)) {
            TexMod.TexMod.Logger.Debug($"Found patch collection '{name}', sprite defs: {spriteDefinitions.Length}");
            collection.PatchCollection(this);
            TexMod.TexMod.Logger.Debug($"Sprite defs after patching: {spriteDefinitions.Length}");
        }
    }
}
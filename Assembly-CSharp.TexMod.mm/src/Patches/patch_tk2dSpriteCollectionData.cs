#pragma warning disable 0626
#pragma warning disable 0649

using System;
using ETGMod;

public class patch_tk2dSpriteCollectionData : tk2dSpriteCollectionData {
    private bool _texmod_init = false;

    private void Start() {
        TexModPatch();
    }

    public void TexModPatch() {
        Console.WriteLine("COLLECTION: " + name);

        if (_texmod_init) return;
        _texmod_init = true;

        Animation.Collection collection;
        if (TexMod.TexMod.CollectionMap.TryGetValue(name, out collection)) {
            TexMod.TexMod.Logger.Debug($"Found patch collection '{name}', sprite defs: {spriteDefinitions.Length}");
            collection.PatchCollection(this);
            TexMod.TexMod.Logger.Debug($"Sprite defs after patching: {spriteDefinitions.Length}");

            TexMod.TexMod.Logger.Debug($"knav '{GetSpriteDefinition("knav3_idle_001")}'");
        }
    }
}
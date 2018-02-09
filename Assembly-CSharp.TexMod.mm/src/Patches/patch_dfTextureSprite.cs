using System;
using UnityEngine;

// this patch makes it possible to provide a sort-of cross between tk2d and df
// where texmod patch collections can also patch df sprites assuming they are
// pre-identified by etgmod

// TODO offsx and offsy not supported

public class patch_dfTextureSprite : dfTextureSprite {
    private string _texmod_fake_collname;
    private string _texmod_fake_defname;

    private Texture _texmod_saved_texture;
    private float _texmod_saved_width;
    private float _texmod_saved_height;
    private Material _texmod_saved_material;
    private Rect _texmod_saved_croprect;
    private bool _texmod_saved_croptexture;

    private bool _texmod_init = false;

    public void TexModUnpatch() {
        Texture = _texmod_saved_texture;
        Material = _texmod_saved_material;
        //TexMod.TexMod.Logger.Debug($"relpos1 {RelativePosition}");
        //RelativePosition -= _texmod_saved_translation;
        Width = _texmod_saved_width;
        Height = _texmod_saved_height;
        CropTexture = _texmod_saved_croptexture;
        CropRect = _texmod_saved_croprect;

        _texmod_init = false;
    }

    public new void Start() {
        TexModPatch();
    }

    public void TexModPatch() {
        if (_texmod_init) return;
        if (_texmod_fake_collname == null || _texmod_fake_defname == null) return;
        _texmod_init = true;

        _texmod_saved_texture = Texture;
        _texmod_saved_material = Material;

        _texmod_saved_width = Width;
        _texmod_saved_height = Height;
        _texmod_saved_croprect = CropRect;
        _texmod_saved_croptexture = CropTexture;

        GUIManager.UIScale = 1f;

        TexMod.TexMod.AddPatchedObject(this);

        ETGMod.Animation.Collection collection;
        TexMod.TexMod.Logger.Debug($"Looking for DF fake TexMod sprite collection ('{_texmod_fake_collname}')");
        if (TexMod.TexMod.CollectionMap.TryGetValue(_texmod_fake_collname, out collection)) {
            TexMod.TexMod.Logger.Debug($"DF fake collection found, searching for definition '{_texmod_fake_defname}'");
            var idx = collection.GetSpriteDefinitionIndex(_texmod_fake_defname);
            if (idx != null) {
                TexMod.TexMod.Logger.Debug($"DF fake definition found! Patching.");

                var def = (ETGMod.BasePatches.tk2dSpriteDefinition)collection.CollectionData.spriteDefinitions[idx.Value];
                Texture = def.material.mainTexture;
                Material = def.material;

                Width = def.GetCropWidth(Texture) * def.GetScaleW(Texture);
                Height = def.GetCropHeight(Texture) * def.GetScaleH(Texture);

                CropRect = new Rect(
                    def.GetCropX(Texture),
                    def.GetCropY(Texture),
                    def.GetCropWidth(Texture),
                    def.GetCropHeight(Texture)
                );
                CropTexture = true;
            }
        } else {
            TexMod.TexMod.Logger.Debug("Not found.");
        }
    }
    public void TexModInitDFFake(string fake_collection_name, string fake_definition_name) {
        _texmod_fake_defname = fake_definition_name;
        _texmod_fake_collname = fake_collection_name;
    }
}

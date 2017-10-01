#pragma warning disable 0626
#pragma warning disable 0649

using System;
using MonoMod;

namespace TexMod.Patches {
    [MonoModPatch("global::MainMenuFoyerController")]
    public class MainMenuFoyerController : ETGMod.CorePatches.MainMenuFoyerController {
        private extern void orig_Awake();

        private void IdentifyDFTitleCard() {
            TexMod.Logger.Debug($"DF Pre-ident: MainMenuFoyerController");

            // pre-ident df sprites
            ((patch_dfTextureSprite)TitleCard).TexModInitDFFake(
                fake_collection_name: "TitleScreenCollection",
                fake_definition_name: "title_words_black_001"
            );

            orig_Awake();
        }

        private void Awake() {
            IdentifyDFTitleCard();
            AddModVersions();
        }
    }
}

#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

internal class patch_CharacterSelectController : CharacterSelectController {

    public static new string GetCharacterPathFromIdentity(PlayableCharacters character) {
        if (character == PlayableCharacters.Pilot) {
            return "PlayerRogue";
        }
        if (character == PlayableCharacters.Soldier) {
            return "PlayerMarine";
        }
        return "Player" + character;
    }

    public static extern string orig_GetCharacterPathFromQuickStart();
    public static new string GetCharacterPathFromQuickStart() {
        if (ETGMod.Player.QuickstartReplacement != null) {
            return ETGMod.Player.QuickstartReplacement;
        }

        if (GameManager.Options.PreferredQuickstartCharacter == GameOptions.QuickstartCharacter.LAST_USED) {
            return GetCharacterPathFromIdentity(GameManager.Options.LastPlayedCharacter);
        }

        // The GameOptions.QuickstartCharacter enum names are uppercase - let's just fall back to the original hardcoded values.
        return orig_GetCharacterPathFromQuickStart();
    }

}

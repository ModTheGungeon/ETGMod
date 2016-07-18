#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;

internal class patch_CharacterSelectController : CharacterSelectController {

    /// <summary>
    /// Stringify the character so it can be used "elsewhere".
    /// </summary>
    /// <param name="character">Character to stringify.</param>
    /// <returns>"Player" + character.ToString()</returns>
    public static new string smethod_0(PlayableCharacters character) {
        if (character == PlayableCharacters.Pilot) {
            return "PlayerRogue";
        }
        return "Player" + character.ToString();
    }

    public static extern string orig_smethod_1();
    /// <summary>
    /// Get quick start character.
    /// </summary>
    /// <returns>Preferred quick start character (last used or configured).</returns>
    public static new string smethod_1() {
        if (ETGMod.Player.QuickstartReplacement != null) {
            return ETGMod.Player.QuickstartReplacement;
        }

        if (GameManager.GameOptions_0.PreferredQuickstartCharacter == GameOptions.QuickstartCharacter.LAST_USED) {
            if (GameManager.GameOptions_0.LastPlayedCharacter == PlayableCharacters.Pilot) {
                return "PlayerRogue";
            }
            return smethod_0(GameManager.GameOptions_0.LastPlayedCharacter);
        }

        // The GameOptions.QuickstartCharacter enum names are uppercase - let's just fall back to the original hardcoded values.
        return orig_smethod_1();
    }

}

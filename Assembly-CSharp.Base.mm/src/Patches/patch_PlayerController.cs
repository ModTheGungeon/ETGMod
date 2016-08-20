#pragma warning disable 0108
#pragma warning disable 0626

using UnityEngine;

internal class patch_PlayerController : PlayerController {

    // @Zandra UNUSED. Why was this even using MonoModIgnore? --0x0ade
    /*
    [MonoMod.MonoModLinkTo(typeof(PlayerController), "m_activeActions")]
    public GungeonActions m_activeActions;
    */

    public extern Vector2 orig_HandlePlayerInput();
    public new Vector2 HandlePlayerInput() {
        Vector2 orig = orig_HandlePlayerInput();

        if (GameManager.Instance.SecondaryPlayer == this && MultiplayerManager.IsPlayingMultiplayer) {
            Vector2 v = new Vector2(
                NetworkInput.directions[0] > 0f && NetworkInput.directions[1] > 0f ? 0f : NetworkInput.directions[0] > 0f ? NetworkInput.directions[0] : -NetworkInput.directions[1],
                NetworkInput.directions[2] > 0f && NetworkInput.directions[3] > 0f ? 0f : NetworkInput.directions[2] > 0f ? NetworkInput.directions[2] : -NetworkInput.directions[3]
            );
            return Vector2.ClampMagnitude(v, 1f);
        }

        return orig;
    }

}


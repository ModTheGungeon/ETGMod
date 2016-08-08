#pragma warning disable 0626
#pragma warning disable 0649

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

internal class patch_Foyer : Foyer {

    public static bool IsNetSelect = false;

    private extern IEnumerator orig_HandleCharacterSelect();
    public IEnumerator HandleCharacterSelect() {
        return orig_HandleCharacterSelect();
    }

    private extern IEnumerator orig_OnSelectedCharacter(float delayTime, FoyerCharacterSelectFlag flag);
    public IEnumerator OnSelectedCharacter(float delayTime, FoyerCharacterSelectFlag flag) {
        return orig_OnSelectedCharacter(delayTime, flag);

        /*if (!IsNetSelect) {
            ETGMultiplayer.PacketHelper.SendRPCToSelf("SelectPlayer", true, flag.CharacterPrefabPath.Split('/').Last());

            return orig_OnSelectedCharacter(delayTime, flag);
        } else {
            IsNetSelect=false;
            return orig_OnSelectedCharacter(delayTime, flag);
        }*/
    }

}


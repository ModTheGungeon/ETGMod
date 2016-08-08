using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class patch_PlayerController : PlayerController {

    [MonoMod.MonoModIgnore]
    public GungeonActions m_activeActions;

    public extern Vector2 orig_HandlePlayerInput();
    public Vector2 HandlePlayerInput() {
        if (GameManager.Instance.SecondaryPlayer==this) {
            Vector2 orig = orig_HandlePlayerInput();
            Vector2 v = new Vector2(
                ( ( NetworkInput.directions[0]>0&&NetworkInput.directions[1]>0 ) ? 0 : ( NetworkInput.directions[0]>0 ? NetworkInput.directions[0] : -NetworkInput.directions[1] ) ),
                ( ( NetworkInput.directions[2]>0&&NetworkInput.directions[3]>0 ) ? 0 : ( NetworkInput.directions[2]>0 ? NetworkInput.directions[2] : -NetworkInput.directions[3] ) )
                );
            return Vector2.ClampMagnitude(v,1);
        }
        return orig_HandlePlayerInput();
    }

}


#pragma warning disable 0626
#pragma warning disable 0649

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class patch_Foyer : Foyer{
    private extern IEnumerator orig_method_8(float float_0, FoyerCharacterSelectFlag foyerCharacterSelectFlag_1);
    public IEnumerator method_8(float float_0, FoyerCharacterSelectFlag flag) {
        return orig_method_8(float_0,flag);
    }
}


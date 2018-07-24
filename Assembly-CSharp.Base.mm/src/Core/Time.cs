using System;
using UnityEngine;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod Time configuration.
    /// </summary>
    public static class Time {
        
        public static void SetTimeScaleModifierIsPost(bool isPost, GameObject source) {
            patch_BraveTime.SetTimeScaleModifierIsPost(isPost, source);
        }

    }

}

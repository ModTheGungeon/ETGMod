#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using MonoMod;

/// <summary>
/// Character / enemy cache?
/// </summary>
internal static class patch_GClass235 {
    public static extern Object orig_smethod_0(string name);
    public static Object smethod_0(string name) {
        if (ETGMod.Player.CoopReplacement != null && name == "PlayerCoopCultist") {
            return orig_smethod_0(ETGMod.Player.CoopReplacement);
        }
        return orig_smethod_0(name);
    }

}

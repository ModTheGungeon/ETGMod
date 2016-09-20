#pragma warning disable 0626
#pragma warning disable 0649

using MonoMod;
using UnityEngine;

internal static class patch_StringTableManager {

    public static extern void orig_SetNewLanguage(StringTableManager.GungeonSupportedLanguages language, bool force = false);
    public static void SetNewLanguage(StringTableManager.GungeonSupportedLanguages language, bool force = false) {
        orig_SetNewLanguage(language, force);
        ETGMod.Databases.Strings.LanguageChanged();
    }


}

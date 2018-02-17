using System;
using MonoMod;
using System.Collections.Generic;
using Language = StringTableManager.GungeonSupportedLanguages;
using WeightedString = WeightedItem<string>;
using OverwriteMap = System.Collections.Generic.Dictionary<StringTableManager.GungeonSupportedLanguages, System.Collections.Generic.Dictionary<string, WeightedItem<string>>>;

namespace ETGMod.BasePatches {
    [MonoModPatch("global::StringTableManager")]
    public class StringTableManager {
        private static Dictionary<string, global::StringTableManager.StringCollection> m_itemsTable;
        public static Action<Language> LanguageChanged = null;
        public extern static void orig_SetNewLanguage(Language language, bool force = false);

        public static void SetNewLanguage(Language language, bool force = false) {
            orig_SetNewLanguage(language, force);
            LanguageChanged?.Invoke(language);
        }

        public extern static string orig_GetItemsString(string key, int index = -1);

        public static string GetItemsString(string key, int index = -1) {
            API.I18N._Logger.Debug($"Get: {key} => {m_itemsTable.ContainsKey(key)}");
            return orig_GetItemsString(key, index);
        }
    }
}

namespace ETGMod.API {
    public class I18N {
        static I18N() {
            BasePatches.StringTableManager.LanguageChanged += (lang) => ForceUpdate();
        }

        internal static Logger _Logger = new Logger("I18N");

        public static readonly Dictionary<Language, string> LanguageToIDMap = new Dictionary<Language, string> {
            {Language.ENGLISH, "english"},
            {Language.FRENCH, "french"},
            {Language.SPANISH, "spanish"},
            {Language.ITALIAN, "italian"},
            {Language.GERMAN, "german"},
            {Language.BRAZILIANPORTUGUESE, "brazilian"},
            {Language.JAPANESE, "japanese"},
            {Language.KOREAN, "korean"},
            {Language.RUSSIAN, "russian"},
            {Language.POLISH, "polish"},
            {Language.CHINESE, "chinese"},
        };

        public static readonly Dictionary<string, Language> IDToLanguageMap = new Dictionary<string, Language> {
            {"english", Language.ENGLISH},
            {"french", Language.FRENCH},
            {"spanish", Language.SPANISH},
            {"italian", Language.ITALIAN},
            {"german", Language.GERMAN},
            {"brazilian", Language.BRAZILIANPORTUGUESE},
            {"japanese", Language.JAPANESE},
            {"korean", Language.KOREAN},
            {"russian", Language.RUSSIAN},
            {"polish", Language.POLISH},
            {"chinese", Language.CHINESE},
        };

        public static readonly string ENGLISH = "english";
        public static readonly string FRENCH = "french";
        public static readonly string SPANISH = "spanish";
        public static readonly string ITALIAN = "italian";
        public static readonly string GERMAN = "german";
        public static readonly string BRAZILIAN = "brazilian";
        public static readonly string JAPANESE = "japanese";
        public static readonly string KOREAN = "korean";
        public static readonly string RUSSIAN = "russian";
        public static readonly string POLISH = "polish";
        public static readonly string CHINESE = "chinese";

        public static OverwriteMap ItemOverwrites = new OverwriteMap();

        public static string GetLanguageID(Language lang) {
            var id = "english";
            LanguageToIDMap.TryGetValue(lang, out id);
            return id;
        }

        public static Language GetLanguageEnum(string id) {
            var lang = Language.ENGLISH;
            IDToLanguageMap.TryGetValue(id, out lang);
            return lang;
        }

        public static void AddItemString(string id, string key, string value, float weight = 1f) {
            key = $"#{key}";

            var lang = GetLanguageEnum(id);

            if (StringTableManager.CurrentLanguage == lang) {
                _Logger.Debug($"Current language matches, immediately adding new string {key}");
                StringTableManager.StringCollection coll;
                if (!StringTableManager.ItemTable.TryGetValue(key, out coll)) {
                    StringTableManager.ItemTable[key] = coll = new StringTableManager.ComplexStringCollection();
                }
                coll.AddString(value, weight);
            }
            AddOverwrite(ItemOverwrites, lang, key, value, weight);

            _Logger.Debug($"Added item string {key} => {value} (weight: {weight})");
        }

        public static void AddOverwrite(OverwriteMap map, Language lang, string key, string value, float weight = 1f) {
            Dictionary<string, WeightedString> overwrites;
            if (!map.TryGetValue(lang, out overwrites)) overwrites = map[lang] = new Dictionary<string, WeightedString>();

            overwrites[key] = new WeightedString(value, weight);
        }

        public static void ForceUpdate(OverwriteMap overwrite_map, Dictionary<string, StringTableManager.StringCollection> table) {
            Dictionary<string, WeightedString> overwrites;
            if (overwrite_map.TryGetValue(StringTableManager.CurrentLanguage, out overwrites)) {
                foreach (var pair in overwrites) {
                    table[pair.Key].AddString(pair.Value.value, pair.Value.weight);
                }
            }
        }

        public static void ForceUpdate() {
            _Logger.Debug($"I18N update!");

            ForceUpdate(ItemOverwrites, StringTableManager.ItemTable);
        }
    }
}

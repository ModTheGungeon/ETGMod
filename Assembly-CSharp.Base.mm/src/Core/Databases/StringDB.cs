using System;
using System.Collections.Generic;

public sealed class StringDB {

    internal StringDB() { }

    public StringTableManager.GungeonSupportedLanguages CurrentLanguage {
        get {
            return GameManager.Options.CurrentLanguage;
        }
        set {
            StringTableManager.SetNewLanguage(value, true);
        }
    }

    public readonly StringDBTable Core      = new StringDBTable(() => StringTableManager.CoreTable);
    public readonly StringDBTable Items     = new StringDBTable(() => StringTableManager.ItemTable);
    public readonly StringDBTable Enemies   = new StringDBTable(() => StringTableManager.EnemyTable);
    public readonly StringDBTable Intro     = new StringDBTable(() => StringTableManager.IntroTable);

    public Action<StringTableManager.GungeonSupportedLanguages> OnLanguageChanged;

    public void LanguageChanged() {
        Core    .LanguageChanged();
        Items   .LanguageChanged();
        Enemies .LanguageChanged();
        Intro   .LanguageChanged();
        OnLanguageChanged?.Invoke(CurrentLanguage);
    }

}

public sealed class StringDBTable {

    internal StringDBTable(Func<Dictionary<string, StringTableManager.StringCollection>> _getTable) {
        _GetTable = _getTable;
    }

    private readonly Func<Dictionary<string, StringTableManager.StringCollection>> _GetTable;
    private Dictionary<string, StringTableManager.StringCollection> _CachedTable;
    public Dictionary<string, StringTableManager.StringCollection> Table {
        get {
            return _CachedTable ?? (_CachedTable = _GetTable());
        }
    }

    private readonly List<string> _ChangeKeys = new List<string>();
    private readonly List<StringTableManager.StringCollection> _ChangeValues = new List<StringTableManager.StringCollection>();

    public StringTableManager.StringCollection this[string key] {
        get {
            return Table[key];
        }
        set {
            Table[key] = value;

            int i = _ChangeKeys.IndexOf(key);
            if (i > 0) {
                _ChangeValues[i] = value;
            } else {
                _ChangeKeys.Add(key);
                _ChangeValues.Add(value);
            }

            JournalEntry.ReloadDataSemaphore++;
        }
    }

    public bool ContainsKey(string key) {
        return Table.ContainsKey(key);
    }

    public void Set(string key, string value) {
        StringTableManager.StringCollection value_ = new StringTableManager.SimpleStringCollection();
        value_.AddString(value, 1f);
        this[key] = value_;
    }

    public string Get(string key) {
        return StringTableManager.GetString(key);
    }

    public void LanguageChanged() {
        _CachedTable = null;
        Dictionary<string, StringTableManager.StringCollection> table = Table;
        for (int i = 0; i < _ChangeKeys.Count; i++) {
            table[_ChangeKeys[i]] = _ChangeValues[i];
        }
    }

}

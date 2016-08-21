using System;

public class AutocompletionSettings {
    public Func<int, string, string[]> Match;
    public AutocompletionSettings (Func<string, string[]> match) {
        Match = (Func<int, string, string[]>) delegate(int index, string key) {
            if (index == 0) {
                return match(key);
            }
            return null;
        };
    }

    public AutocompletionSettings(Func<int, string, string[]> match) {
        Match = match;
    }

    public static bool MatchContains = true;
}

public static class StringAutocompletionExtensions {
    public static bool AutocompletionMatch(this string text, string match) {
        if (AutocompletionSettings.MatchContains) {
            return text.Contains (match);
        } else {
            return text.StartsWith (match);
        }
    }
}
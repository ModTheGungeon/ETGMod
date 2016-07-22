using System;

public class AutocompletionSettings {
    public Func<string, string[]> Match;
    public AutocompletionSettings (Func<string, string[]> match) {
        Match = match;
    }
}
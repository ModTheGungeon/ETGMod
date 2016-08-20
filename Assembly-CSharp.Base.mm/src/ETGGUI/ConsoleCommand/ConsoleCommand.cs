using System;

public class ConsoleCommand : ConsoleCommandUnit {

    private string[] _EmptyStringArray = {};

    public ConsoleCommand(Action<string[]> cmdref, AutocompletionSettings autocompletion) {
        CommandReference = cmdref;
        Autocompletion = autocompletion;
    }

    public ConsoleCommand(Action<string[]> cmdref) {
        CommandReference = cmdref;
        Autocompletion = new AutocompletionSettings ((string input) => _EmptyStringArray);
    }

}


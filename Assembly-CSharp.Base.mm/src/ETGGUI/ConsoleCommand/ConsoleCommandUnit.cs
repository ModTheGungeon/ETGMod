using System;

public class ConsoleCommandUnit {
    public string Name;

    public System.Action<string[]> CommandReference;

    public void RunCommand(string[] args) {
        CommandReference(args);
    }

    public AutocompletionSettings Autocompletion;

    public ConsoleCommandUnit () {
    }
}

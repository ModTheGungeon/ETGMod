using System;

public class ConsoleCommandUnit {
    public string Name;

    public Action<string[]> CommandReference;

    public void RunCommand(string[] args) {
        CommandReference(args);
    }

    public AutocompletionSettings Autocompletion;

    public ConsoleCommandUnit () {
    }
}

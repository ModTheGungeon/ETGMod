using System;

public class ConsoleCommandUnit {
    /// <summary>
    /// The command's name in the console, what you use to call it.
    /// </summary>
    public string Name;

    /// <summary>
    /// The autocompletion settings used when autocompletion was requested
    /// </summary>
    public AutocompletionSettings Autocompletion;

    public ConsoleCommandUnit () {
    }
}
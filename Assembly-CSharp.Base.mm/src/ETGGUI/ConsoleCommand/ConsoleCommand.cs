using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class ConsoleCommand : ConsoleCommandUnit {

    private string[] _EmptyStringArray = new string[] {};

    public ConsoleCommand(System.Action<string[]> cmdref, AutocompletionSettings autocompletion) {
        CommandReference = cmdref;
        Autocompletion = autocompletion;
    }

    public ConsoleCommand(System.Action<string[]> cmdref) {
        CommandReference = cmdref;
        Autocompletion = new AutocompletionSettings (delegate (string input) {
            return _EmptyStringArray;
        });
    }

}


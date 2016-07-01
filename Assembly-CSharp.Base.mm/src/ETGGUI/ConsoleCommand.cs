using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


/// <summary>
/// ETG Mod console command class
/// </summary>
public class ConsoleCommand {


    /// <summary>
    /// The command's name in the console, what you use to call it.
    /// </summary>
    public string commandName;

    /// <summary>
    /// The function run when
    /// </summary>
    public System.Action<string[]> commandReference;

    /// <summary>
    /// A 2D array of all accepted arguments.
    /// The first coordinate is the argument number, returned array is a list of accepted arguments.
    /// </summary>
    public string[][] acceptedArguments;

    public ConsoleCommand(string _cmdName, System.Action<string[]> _cmdRef, params string[][] _acceptedArgs) {
        commandName=_cmdName;
        commandReference=_cmdRef;
        if (_acceptedArgs!=null) {
            acceptedArguments=_acceptedArgs;
        }
    }

    public void RunCommand(string[] args) {
        if (acceptedArguments!=null) {
            for (int i = 0; i<args.Length; i++) {

                //Out of range, don't bother anymore
                if (acceptedArguments.Length<=i) {
                    break;
                }

                //There's no acceptable arguments, so skip this.
                if (acceptedArguments[i]==null) {
                    continue;
                }

                //Did we find a matching command in the acceptable arguments?
                bool foundMatch=false;

                foreach (string s in acceptedArguments[i]) {
                    if (s==args[i]) {
                        foundMatch=true;
                        break;
                    }
                }

                if (foundMatch) {
                    continue;
                } else {
                    Debug.Log("Unnaceptable argument " +'"'+args[i]+'"' +" in command " + commandName);
                    return;
                }
            }
        }

        commandReference(args);
    }

    internal bool IsCommandCorrect(string[] splitCommand) {

        if (acceptedArguments==null)
            return true;

        for(int i = 1; i < splitCommand.Length; i++) {

            if (acceptedArguments.Length<=i-1)
                return true;

            if (acceptedArguments[i-1]==null)
                continue;

            bool isContained = false;
            for(int j = 0; j < acceptedArguments[i-1].Length && !isContained; j++) {
                if (acceptedArguments[i-1][j]==splitCommand[i])
                    isContained=true;
            }
            if (!isContained)
                return false;
        }

        return true;
    }
}


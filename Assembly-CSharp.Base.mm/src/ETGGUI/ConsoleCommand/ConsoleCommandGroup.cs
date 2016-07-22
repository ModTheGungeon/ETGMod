using System;
using System.Collections.Generic;

public class ConsoleCommandGroup : ConsoleCommandUnit {
    private Dictionary<string, ConsoleCommand> _Commands = new Dictionary<string, ConsoleCommand>();
    private Dictionary<string, ConsoleCommandGroup> _Groups = new Dictionary<string, ConsoleCommandGroup>();

    public ConsoleCommandGroup () {
        Autocompletion = new AutocompletionSettings (delegate(string input) {
            string[] list = GetAllUnitNames();
            List<String> ret = new List<String>();
            for (int i = 0; i < list.Length; i++) {
                string name = list[i];
                if (name.StartsWith(input)) {
                    ret.Add(name);
                }
            }
            return ret.ToArray();
        });
    }

    public ConsoleCommandGroup AddUnit(string name, ConsoleCommand command) {
        command.Name = name;
        _Commands [name] = command;
        return this;
    }

    public ConsoleCommandGroup AddUnit(string name, Action<string[]> action) {
        ConsoleCommand command = new ConsoleCommand (action);
        command.Name = name;
        _Commands [name] = command;
        return this;
    }

    public ConsoleCommandGroup AddUnit(string name, Action<string[]> action, AutocompletionSettings autocompletion) {
        ConsoleCommand command = new ConsoleCommand (action, autocompletion);
        command.Name = name;
        _Commands [name] = command;
        return this;
    }

    public ConsoleCommandGroup AddUnit(string name, ConsoleCommandGroup group) {
        group.Name = name;
        _Groups [name] = group;
        return this;
    }

    public ConsoleCommandUnit GetUnit(string[] unit) {
        ConsoleCommandGroup currentgroup = this;
        if (unit.Length == 0) return currentgroup;
        for (int i = 0; i < unit.Length; i++) {
            if (currentgroup.GetGroup(unit[i]) != null) {
                currentgroup = _Groups [unit [i]];
            } else if (currentgroup.GetCommand (unit [i]) != null) {
                return currentgroup.GetCommand (unit [i]);
            }
        }
        return currentgroup;
    }

    public List<List<String>> ConstructPaths() {
        List<List<String>> ret = new List<List<String>>();
        foreach (string key in _Commands.Keys) {
            List<String> tmp = new List<String> ();;
            tmp.Add (key);
            ret.Add (tmp);
        }

        foreach (string key in _Groups.Keys) {
            List<List<String>> tmp = _Groups [key].ConstructPaths ();
            for (int i = 0; i < tmp.Count; i++) {
                List<String> prefixtmp = new List<String> ();
                prefixtmp.Add (key);
                for (int j = 0; j < tmp [i].Count; j++) {
                    prefixtmp.Add (tmp[i][j]);
                }
                ret.Add (prefixtmp);
            }
        }

        return ret;
    }

    public string[] GetAllUnitNames() {
        List<String> ret = new List<String>();

        foreach (string key in _Groups.Keys) {
            ret.Add(key);
        }
        foreach (string key in _Commands.Keys) {
            ret.Add(key);
        }

        return ret.ToArray();
    }

    public ConsoleCommandGroup GetGroup(params string[] unit) {
        ConsoleCommandGroup currentgroup = this;
        if (unit.Length == 0) return currentgroup;
        for (int i = 0; i < unit.Length; i++) {
            if (currentgroup.GetGroup(unit[i]) != null) {
                currentgroup = _Groups [unit [i]];
            }
        }
        return currentgroup;
    }

    public ConsoleCommandGroup GetGroup(string unit) {
        if (!_Groups.ContainsKey (unit)) return null;
        return _Groups [unit];
    }

    public ConsoleCommand GetCommand(params string[] unit) {
        ConsoleCommandGroup currentgroup = this;
        if (unit.Length == 0) return null;
        for (int i = 0; i < unit.Length; i++) {
            if (currentgroup.GetGroup(unit[i]) != null) {
                currentgroup = _Groups [unit [i]];
            } else if (currentgroup.GetCommand (unit [i]) != null) {
                return currentgroup.GetCommand (unit [i]);
            }
        }
        return null;
    }

    public ConsoleCommand GetCommand(string name) {
        if (_Commands.ContainsKey(name)) {
            return _Commands [name];
        } else {
            return null;
        }
    }

    public int GetFirstNonUnitIndexInPath(string[] path) {
        ConsoleCommandGroup currentgroup = this;
        int storedindex = 0;
        for (int i = 0; i < path.Length; i++) {
            if (currentgroup.GetGroup(path[i]) != null) {
                currentgroup = _Groups [path [i]];
                storedindex++;
            } else if (currentgroup.GetCommand (path [i]) != null) {
                return i + 1;
            }
        }
        return storedindex;
    }
}
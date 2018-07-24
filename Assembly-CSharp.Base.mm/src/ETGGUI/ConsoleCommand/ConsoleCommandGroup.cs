﻿using System;
using System.Collections.Generic;
using System.Linq;

public class ConsoleCommandGroup : ConsoleCommandUnit {
    private Dictionary<string, ConsoleCommand> _Commands = new Dictionary<string, ConsoleCommand>();
    private Dictionary<string, ConsoleCommandGroup> _Groups = new Dictionary<string, ConsoleCommandGroup>();

    private AutocompletionSettings _CreateDefaultAutocompletion() {
        return new AutocompletionSettings (delegate(string input) {
            string[] list = GetAllUnitNames ();
            List<string> ret = new List<string> ();
            for (int i = 0; i < list.Length; i++) {
                string name = list [i];
                if (name.StartsWith (input)) {
                    ret.Add (name);
                }
            }
            return ret.ToArray ();
        });
    }

    public ConsoleCommandGroup (Action<string[]> cmdref) {
        Autocompletion = _CreateDefaultAutocompletion();
        CommandReference = cmdref;
    }

    public ConsoleCommandGroup(Action<string[]> cmdref, AutocompletionSettings additionalautocompletion) {
        Autocompletion = new AutocompletionSettings (delegate(int index, string keyword) {
            string[] additionalresults = additionalautocompletion.Match(index, keyword);
            string[] membersresults = _CreateDefaultAutocompletion().Match(index, keyword);
            if (additionalresults != null) {
                return membersresults.Concat(additionalresults).ToArray();
            } else {
                return membersresults;
            }
        });
        CommandReference = cmdref;
    }

    public ConsoleCommandGroup ()
        : this((string[] args) => ETGModConsole.Log("Command group does not have an assigned action")) {
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

    public ConsoleCommandGroup AddGroup(string name) {
        AddUnit(name, new ConsoleCommandGroup());
        return this;
    }

    public ConsoleCommandGroup AddGroup(string name, Action<string[]> action) {
        AddUnit(name, new ConsoleCommandGroup(action));
        return this;
    }

    public ConsoleCommandGroup AddGroup(string name, Action<string[]> action, AutocompletionSettings autocompletion) {
        AddUnit (name, new ConsoleCommandGroup (action, autocompletion));
        return this;
    }

    public UnitSearchResult SearchUnit(string[] path) {
        ConsoleCommandGroup currentgroup = this;
        UnitSearchResult result = new UnitSearchResult();
        for (int i = 0; i < path.Length; i++) {
            ConsoleCommandGroup group = currentgroup.GetGroup(path[i]);
            ConsoleCommand command = currentgroup.GetCommand(path[i]);
            if (group != null) {
                currentgroup = group;
                result.index++;
            } else if (command != null) {
                result.index++;
                result.unit = command;
                return result;
            }
        }
        result.unit = currentgroup;
        return result;
    }

    public List<List<string>> ConstructPaths() {
        List<List<string>> ret = new List<List<string>>();
        foreach (string key in _Commands.Keys) {
            List<string> tmp = new List<string> ();
            tmp.Add (key);
            ret.Add (tmp);
        }

        foreach (string key in _Groups.Keys) {
            List<string> groupkeytmp = new List<string> ();
            groupkeytmp.Add (key);
            ret.Add (groupkeytmp);

            List<List<string>> tmp = _Groups [key].ConstructPaths ();
            for (int i = 0; i < tmp.Count; i++) {
                List<string> prefixtmp = new List<string> ();
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
        List<string> ret = new List<string>();

        foreach (string key in _Groups.Keys) {
            ret.Add(key);
        }
        foreach (string key in _Commands.Keys) {
            ret.Add(key);
        }

        return ret.ToArray();
    }

    public ConsoleCommandUnit GetUnit(string[] unit) {
      return SearchUnit(unit).unit;
    }

    public ConsoleCommandGroup GetGroup(string[] unit) {
        return SearchUnit(unit).unit as ConsoleCommandGroup;
    }

    public ConsoleCommandGroup GetGroup(string unit) {
        if (!_Groups.ContainsKey (unit)) return null;
        return _Groups [unit];
    }

    public ConsoleCommand GetCommand(string[] unit) {
        return SearchUnit(unit).unit as ConsoleCommand;
    }

    public ConsoleCommand GetCommand(string name) {
        if (!_Commands.ContainsKey(name)) return null;
        return _Commands[name];
    }

    public int GetFirstNonUnitIndexInPath(string[] path) {
        return SearchUnit(path).index + 1;
    }

    public class UnitSearchResult {
        public int index = -1;
        public ConsoleCommandUnit unit;
        public UnitSearchResult(int index, ConsoleCommandUnit unit) {
          this.index = index;
          this.unit = unit;
        }
        public UnitSearchResult() {}
    }
}

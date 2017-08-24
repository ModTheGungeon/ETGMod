using System;
using System.Collections.Generic;

namespace ETGMod.Console {
    public class CommandHistory {
        public List<string> Entries = new List<string> { "" };
        public int CurrentIndex = 0;

        private Parser.Executor _Executor;
        private Parser.Parser _Parser;

        public CommandHistory(Parser.Executor exec, Parser.Parser parser) {
            _Executor = exec;
            _Parser = parser;
        }

        public string Execute(int index) {
            if (index < 0 || index >= Entries.Count) throw new Exception("Tried to execute command from history at nonexistant index.");

            return _Executor.ExecuteCommand(_Parser.Parse(Entries[index]), index);
        }

        public string Entry {
            get {
                return Entries[CurrentIndex];
            }
            set {
                Entries[CurrentIndex] = value;
            }
        }

        public string LastEntry {
            get {
                return Entries[Entries.Count - 1];
            }
            set {
                Entries[Entries.Count - 1] = value;
            }
        }

        public bool IsLastIndex {
            get {
                return CurrentIndex == Entries.Count - 1;
            }
        }

        public int LastIndex {
            get {
                return Entries.Count - 1;
            }
        }

        public void MoveUp(int n = 1) {
            CurrentIndex -= n;
            if (CurrentIndex < 0) CurrentIndex = 0;
        }

        public void MoveDown(int n = 1) {
            CurrentIndex += n;
            if (CurrentIndex > Entries.Count - 1) CurrentIndex = Entries.Count - 1;
        }

        public void Push() {
            if (!IsLastIndex) LastEntry = Entry;
            Entries.Add("");
            CurrentIndex = LastIndex;
        }
    }
}

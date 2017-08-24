using System;
using System.Collections.Generic;

namespace ETGMod.Console {
    public class Command {
        public string Name { get; private set; }
        public Dictionary<string, Command> SubCommands = new Dictionary<string, Command>();
        public Func<List<string>, int?, string> Callback { get; private set; }
        public AutoCompletor AutoCompletor { get; private set; }

        public string HelpText = "";

        public Command(string name, Func<List<string>, int?, string> callback) {
            Name = name;
            Callback = callback;
        }

        public Command(string name, Func<List<string>, string> callback) {
            Name = name;
            Callback = (args, _) => callback(args);
        }

        public Command WithHelpText(string text) {
            HelpText = text;
            return this;
        }

        public Command WithSubCommand(Command sub) {
            SubCommands[sub.Name] = sub;
            return this;
        }

        public Command WithAutoCompletor(AutoCompletor completor) {
            AutoCompletor = completor;
            return this;
        }

        public string Run(List<string> args, int? history_index = null) {
            return Callback.Invoke(args, history_index);
        }
    }
}

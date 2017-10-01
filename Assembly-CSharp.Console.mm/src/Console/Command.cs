using System;
using System.Collections.Generic;
using System.Text;

namespace ETGMod.Console {
    public class Command {
        public string Name { get; protected set; }
        public Dictionary<string, Command> SubCommands = new Dictionary<string, Command>();
        public Func<List<string>, int?, Command, string> Callback { get; private set; }
        public AutoCompletor AutoCompletor { get; private set; }

        public string HelpText = "";

        public Command(string name, Func<List<string>, int?, Command, string> callback) {
            Name = name;
            Callback = callback;
        }

        public Command(string name, Func<List<string>, int?, string> callback)
            : this(name, (args, histindex, _) => callback(args, histindex)) {}

        public Command(string name, Func<List<string>, string> callback)
            : this(name, (args, _, _2) => callback(args)) {}

        public Command WithHelpText(string text) {
            HelpText = text;
            return this;
        }

        public Command WithSubCommand(Command sub) {
            SubCommands[sub.Name] = sub;
            return this;
        }

        public Command WithSubCommand(string name, Func<List<string>, int?, Command, string> callback) {
            SubCommands[name] = new Command(name, callback);
            return this;
        }

        public Command WithSubCommand(string name, Func<List<string>, int?, string> callback) {
            SubCommands[name] = new Command(name, callback);
            return this;
        }

        public Command WithSubCommand(string name, Func<List<string>, string> callback) {
            SubCommands[name] = new Command(name, callback);
            return this;
        }

        public Command WithSubGroup(Group group) {
            SubCommands[group.Name] = group;
            return this;
        }

        public Command WithSubGroup(string name) {
            SubCommands[name] = new Group(name);
            return this;
        }

        public Command WithAutoCompletor(AutoCompletor completor) {
            AutoCompletor = completor;
            return this;
        }

        public virtual string Run(List<string> args, int? history_index = null) {
            return Callback.Invoke(args, history_index, this);
        }
    }

    public class Group : Command {
        public Group(string name) : base(name, (Func<List<string>, int?, Command, string>)null) {}

        public new Group WithHelpText(string text) {
            return (Group)base.WithHelpText(text);
        }

        public new Group WithSubCommand(Command sub) {
            return (Group)base.WithSubCommand(sub);
        }

        public new Group WithSubCommand(string name, Func<List<string>, int?, Command, string> callback) {
            return WithSubCommand(new Command(name, callback));
        }

        public new Group WithSubCommand(string name, Func<List<string>, int?, string> callback) {
            return WithSubCommand(new Command(name, callback));
        }

        public new Group WithSubCommand(string name, Func<List<string>, string> callback) {
            return WithSubCommand(new Command(name, callback));
        }

        public new Group WithSubGroup(Group group) {
            return (Group)base.WithSubGroup(group);
        }

        public new Group WithSubGroup(string name) {
            return WithSubGroup(name);
        }

        public new Group WithAutoCompletor(AutoCompletor completor) {
            return (Group)base.WithAutoCompletor(completor);
        }

        public override string Run(List<string> args, int? history_index = null) {
            var b = new StringBuilder();
            b.AppendLine($"Can't execute command group '{Name}'. Did you mean:");
            foreach (var c in SubCommands) {
                b.AppendLine($"- {Name}{Console.COMMAND_PATH_SEPARATOR}{c.Key}");
            }
            throw new Exception(b.ToString());
        }
    }
}

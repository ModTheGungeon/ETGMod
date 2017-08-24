using System;
using System.Text;
using System.Collections.Generic;

namespace ETGMod.Console.Parser {
    public class Executor {
        public delegate string ExecFunc(string name, List<string> args, int? history_index);

        public ExecFunc Exec;

        public Executor(ExecFunc exec) {
            Exec = exec;
        }

        public string EvaluateArgument(Argument arg) {
            var builder = new StringBuilder();
            for (int i = 0; i < arg.Content.Count; i++) {
                var node = arg.Content[i];
                if (node is Literal) {
                    builder.Append(((Literal)node).Content);
                } else if (node is Command) {
                    builder.Append(ExecuteCommand((Command)node) ?? "");
                }
            }
            return builder.ToString();
        }

        public string ExecuteCommand(Command cmd, int? history_index = null) {
            var name = EvaluateArgument(cmd.Name);
            var args = new List<string>();
            for (int i = 0; i < cmd.Args.Count; i++) {
                args.Add(EvaluateArgument(cmd.Args[i]));
            }


            return Exec.Invoke(name, args, history_index);
        }
    }
}

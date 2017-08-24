using System;
using System.Collections.Generic;
using System.Text;

namespace ETGMod.Console.Parser {
    public struct Position {
        public int FirstLine;
        public int LastLine;
        public int FirstChar;
        public int LastChar;

        public Position(int first_line, int first_char, int last_line, int last_char) {
            FirstLine = first_line;
            FirstChar = first_char;
            LastLine = last_line;
            LastChar = last_char;
        }
    }

    public interface ASTNode {
        Position Position { get; }
    }

    public struct Argument : ASTNode {
        public Position Position { get; }
        public List<ASTNode> Content;

        public Argument(List<ASTNode> content, Position position) {
            Position = position;
            Content = content;
        }

        public override string ToString() {
            var builder = new StringBuilder("[");
            for (int i = 0; i < Content.Count; i++) {
                builder.Append(Content[i].ToString());
                if (i != Content.Count - 1) builder.Append(", ");
            }
            builder.Append("]");

            return builder.ToString();
        }
    }

    public struct Literal : ASTNode {
        public Position Position { get; }
        public string Content;

        public Literal(string content, Position position) {
            Position = position;
            Content = content;
        }

        public override string ToString() {
            return $"\"{Content}\"";
        }
    }

    public struct Command : ASTNode {
        public Position Position { get; }
        public Argument Name;
        public List<Argument> Args;

        public Command(Argument name, List<Argument> args, Position position) {
            Position = position;
            Name = name;
            Args = args;
        }

        public override string ToString() {
            var builder = new StringBuilder(Name.ToString());
            builder.Append('(');
            for (int i = 0; i < Args.Count; i++) {
                builder.Append(Args[i].ToString());
                if (i != Args.Count - 1) builder.Append(", ");
            }
            builder.Append(')');
            return builder.ToString();
        }
    }
}

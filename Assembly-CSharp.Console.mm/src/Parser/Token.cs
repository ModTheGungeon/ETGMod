using System;
using System.Collections.Generic;
using System.Text;

namespace ETGMod.Console.Parser {
    public enum TokenType {
        CommandStart,
        CommandEnd,
        Separator,
        Literal
    }

    public enum QuoteType {
        Normal,
        Verbatim
    }

    public class LiteralInfo {
        public QuoteType QuoteType;
        public bool IsTerminated = true; 

        public char QuoteChar {
            get {
                switch(QuoteType) {
                    case QuoteType.Normal: return '"';
                    case QuoteType.Verbatim: return '{';
                }
                return '?';
            }
        }
    }

    public struct Token {
        public TokenType Type;
        public string Content;
        public LiteralInfo LiteralInfo;
        public bool IsFinalToken;

        public int FirstLine;
        public int FirstCharacter;
        public int LastLine;
        public int LastCharacter;

        public string PositionInfo {
            get {
                if (FirstLine == LastLine && FirstCharacter == LastCharacter) {
                    return $"{FirstLine}:{FirstCharacter}";
                } else {
                    return $"{FirstLine}:{FirstCharacter}-{LastLine}:{LastCharacter}";
                }
            }
        }

        public Token(TokenType type, string content, int first_line, int first_character, int last_line, int last_character) {
            IsFinalToken = false;
            LiteralInfo = null;
            FirstLine = first_line;
            FirstCharacter = first_character;
            LastLine = last_line;
            LastCharacter = last_character;
            Type = type;
            Content = content;
        }

        public static string EscapeNormal(string s) {
            var builder = new StringBuilder();
            for (int i = 0; i < s.Length; i++) {
                var c = s[i];
                switch(c) {
                    case '\n': builder.Append("\\n"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    default: builder.Append(c); break;
                }
            }
            return builder.ToString();
        }

        public static string EscapeVerbatim(string s) {
            var builder = new StringBuilder();
            for (int i = 0; i < s.Length; i++) {
                var c = s[i];
                switch (c) {
                    case '}': builder.Append("\\}"); break;
                    case '\\': builder.Append("\\\\"); break;
                    default: builder.Append(c); break;
                }
            }
            return builder.ToString();
        }

        public static string EscapeUnquoted(string s) {
            var builder = new StringBuilder();
            for (int i = 0; i < s.Length; i++) {
                var c = s[i];
                if (Array.IndexOf(Lexer.SPECIAL_UNQUOTED_LITERAL_CHARS, c) != -1) {
                    builder.Append("\\");
                }
                builder.Append(c);
            }
            return builder.ToString();
        }

        public override string ToString() {
            if (Type == TokenType.Literal) {
                if (LiteralInfo == null) return EscapeUnquoted(Content);
                switch (LiteralInfo.QuoteType) {
                    //FIXME unterminated strings
                    case QuoteType.Normal: return $"\"{EscapeNormal(Content)}\"";
                    case QuoteType.Verbatim: return $"\"{EscapeVerbatim(Content)}\"";
                }                    
            }
            return Content;
        }
    }
}

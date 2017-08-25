using System;
using System.Collections.Generic;
using System.Text;

namespace ETGMod.Console.Parser {
    public class Lexer {
        internal static char[] SPECIAL_UNQUOTED_LITERAL_CHARS = { '[', ']', '"', ' ', '\t', '\n', '\0' };

        private string _Str;

        public int Pos;
        public bool IsFinished {
            get { return CurrentChar == '\0'; }
        }
        public int Line;
        public int Character;

        public Lexer() {}

        public Lexer(string content) {
            Reset(content);
        }

        public void Reset(string content) {
            _Str = content;
            Line = 1;
            Character = 0;
            Pos = -1;
        }

        public char CurrentChar {
            get {
                if (Pos < _Str.Length && Pos >= 0) return _Str[Pos];
                return '\0';
            }
        }

        public char PeekChar {
            get {
                if (Pos + 1 < _Str.Length && Pos + 1 >= 0) return _Str[Pos + 1];
                return '\0';
            }
        }

        public char UnescapedChar {
            get {
                if (CurrentChar != '\\') return CurrentChar;
                MoveNext();
                var escape_c = CurrentChar;
                switch (escape_c) {
                case 'n': return '\n';
                case 't': return '\t';
                case '\\': return '\\';
                default: return escape_c;
                }
            }
        }

        public string SkipUntilLastWhitespace() {
            var builder = new StringBuilder();
            builder.Append(CurrentChar);
            while(PeekChar != '\0' && (PeekChar == ' ' || PeekChar == '\t' || PeekChar == '\n')) {
                MoveNext();
                builder.Append(CurrentChar);
            }
            return builder.ToString();
        }

        public void MoveNext() {
            Pos += 1;
            Character += 1;
            if (CurrentChar == '\n') {
                Line += 1;
                Character = 0;
            }
        }

        public List<Token> Lex() {
            var list = new List<Token>();
            Token? token = null;
            while (token?.IsFinalToken != true) {
                token = NextToken();
                list.Add(token.Value);
            }
            return list;
        }

        public static string Unlex(List<Token> strs) {
            var builder = new StringBuilder();
            foreach (var s in strs) {
                builder.Append(s);
            }
            return builder.ToString();
        }

        private Token _Token(TokenType type, int first_line, int first_character, string content) {
            return new Token(type, content, first_line, first_character, Line, Character);
        }

        public Token NextToken() {
            if (_Str == null) throw new InvalidOperationException("Can't lex with no input");

            Token token;

            if (Pos == -1) {
                token = _Token(TokenType.CommandStart, 1, 1, "");
            } else {
                switch (CurrentChar) {
                case '[': token = _Token(TokenType.CommandStart, Line, Character, "["); break;
                case ']': token = _Token(TokenType.CommandEnd, Line, Character, "]"); break;
                case '\0':
                    token = _Token(TokenType.CommandEnd, Line, Character, "");
                    token.IsFinalToken = true;
                    break;
                case '{':
                    var opening_line = Line;
                    var opening_char = Character;
                    var builder = new StringBuilder();
                    var level = 1;

                    while (true) {
                        MoveNext();
                        if (CurrentChar == '{') level += 1;
                        else if (CurrentChar == '}') {
                            level -= 1;
                            if (level <= 0) break;
                        }
                        if (CurrentChar == '\0') break;
                        builder.Append(CurrentChar);
                    }

                    token = _Token(TokenType.Literal, opening_line, opening_char, builder.ToString());
                    token.LiteralInfo = new LiteralInfo {
                        QuoteType = QuoteType.Verbatim
                    };

                    token.LiteralInfo.IsTerminated = level == 0;

                    break;
                case ' ':
                case '\t':
                case '\n':
                    opening_line = Line;
                    opening_char = Character;

                    var content = SkipUntilLastWhitespace();
                    token = _Token(TokenType.Separator, opening_line, opening_char, content);
                    break;
                case '"':
                    opening_line = Line;
                    opening_char = Character;

                    builder = new StringBuilder();

                    while (PeekChar != '"' && PeekChar != '\0') {
                        MoveNext();
                        builder.Append(UnescapedChar);
                    }

                    MoveNext();

                    token = _Token(TokenType.Literal, opening_line, opening_char, builder.ToString());
                    token.LiteralInfo = new LiteralInfo {
                        QuoteType = QuoteType.Normal
                    };

                    if (CurrentChar == '\0') token.LiteralInfo.IsTerminated = false;
                    break;
                default:
                    opening_line = Line;
                    opening_char = Character;

                    builder = new StringBuilder();
                    builder.Append(UnescapedChar);

                    while (Array.IndexOf(SPECIAL_UNQUOTED_LITERAL_CHARS, PeekChar) == -1) {
                        MoveNext();
                        builder.Append(UnescapedChar);
                    }

                    token = _Token(TokenType.Literal, opening_line, opening_char, builder.ToString());
                    break;
                }
            }

            MoveNext();
            return token;
        }
    }
}

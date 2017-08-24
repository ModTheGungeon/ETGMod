using System;
using System.Collections.Generic;
using System.Text;

namespace ETGMod.Console.Parser {
    public class Parser {
        public class ParserException : Exception {
            public Token? Token { set; get; } = null;

            public ParserException(Token token, string msg) : base($"[{token.PositionInfo} {token.Type}] {msg}") {
                Token = token;
            }

            public ParserException(string msg) : base(msg) {}
        }

        private Lexer _Lexer = new Lexer();
        private Token _Token;
        private bool _Lenient = false;

        public Command Parse(string cmd, bool lenient = false) {
            _Lenient = lenient;
            _Lexer.Reset(cmd);
            _NextToken();
            return _ReadCommand();
        }

        private void _NextToken() {
            _Token = _Lexer.NextToken();
        }

        private ParserException _Error(string msg) {
            return new ParserException(_Token, msg);
        }

        private Command _ReadCommand() {
            var pos = new Position();

            pos.FirstChar = _Token.FirstCharacter;
            pos.FirstLine = _Token.FirstLine;
            _NextToken();

            var args = new List<Argument>();
            Argument name = default(Argument);

            var no_name_yet = true;
            while (_Token.Type != TokenType.CommandEnd) {
                if (_Token.Type == TokenType.Separator) _NextToken();
                if (no_name_yet) {
                    name = _ReadArgument();
                    no_name_yet = false;
                } else args.Add(_ReadArgument());
            }

            if (no_name_yet) throw new ParserException("No command provided");

            pos.LastChar = _Token.LastCharacter;
            pos.LastLine = _Token.LastLine;
            _NextToken();

            return new Command(name, args, pos);
        }

        private Literal _ReadLiteral() {
            if (!_Lenient && _Token.LiteralInfo?.IsTerminated == false) {
                throw new ParserException(_Token, $"Unterminated quote ('{_Token.LiteralInfo.QuoteChar}')");
            }
            var content = _Token.Content;
            var pos = new Position(_Token.FirstLine, _Token.FirstCharacter, _Token.LastLine, _Token.LastCharacter);
            _NextToken();
            return new Literal(content, pos);
        }

        private Argument _ReadArgument() {
            var content = new List<ASTNode>();

            while (_Token.Type != TokenType.Separator && _Token.Type != TokenType.CommandEnd) {
                if (_Token.Type == TokenType.CommandStart) {
                    content.Add(_ReadCommand());
                } else if (_Token.Type == TokenType.Literal) {
                    content.Add(_ReadLiteral());
                }
            }

            var first_line = content.Count == 0 ? 0 : content[0].Position.FirstLine;
            var first_char = content.Count == 0 ? 0 : content[0].Position.FirstChar;
            var last_line = content.Count == 0 ? 0 : content[content.Count - 1].Position.LastLine;
            var last_char = content.Count == 0 ? 0 : content[content.Count - 1].Position.LastChar;

            return new Argument(content, new Position(first_line, first_char, last_line, last_char));
        }
    }
}

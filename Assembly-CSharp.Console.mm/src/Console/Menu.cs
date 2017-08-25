using System;
using System.Collections.Generic;
using SGUI;
using UnityEngine;
using ETGMod.GUI;
using System.Text;

namespace ETGMod.Console {
    public class ConsoleMenu : Menu {
        public static ConsoleMenu Instance = null;
        public const char COMMAND_PATH_SEPARATOR = '/';
        private static Logger _Logger = new Logger("ConsoleMenu");
        public Dictionary<string, Command> Commands = new Dictionary<string, Command>();

        public string CurrentCommandText {
            set {
                if (!History.IsLastIndex) {
                    History.LastEntry = History.Entry;
                    History.CurrentIndex = History.LastIndex;
                }

                History.Entry = value;
                _GetChild<SGUI.STextField>(WindowChild.InputBox).Text = value;
            }

            get {
                return _GetChild<SGUI.STextField>(WindowChild.InputBox).Text;
            }
        }

        public CommandHistory History;

        public bool PrintUsedCommand = true;

        public class CommandResolutionException : Exception { public CommandResolutionException(string msg) : base(msg) {} }

        private Parser.Lexer _Lexer = new Parser.Lexer(); // a lexer for autocomplete purposes
        private Parser.Parser _Parser = new Parser.Parser();
        private Parser.Executor _Executor;

        public Command ResolveCommand(string path) {
            Command cmd;
            string cmd_str;

            if (TryResolveCommand(path, out cmd, out cmd_str)) return cmd;
            if (cmd_str == path) {
                throw new CommandResolutionException($"Command '{path}' doesn't exist.");
            } else {
                throw new CommandResolutionException($"Command '{path}' doesn't exist (because '{cmd_str}' doesn't exist).");
            }
        }

        public bool TryResolveCommand(string path, out Command cmd, out string result_command) {
            Command foundcmd = null;
            var dict = Commands;

            var split = path.Split(COMMAND_PATH_SEPARATOR);

            for (int i = 0; i < split.Length; i++) {
                var el = split[i];

                if (!dict.TryGetValue(el, out foundcmd)) {
                    var builder = new StringBuilder();
                    for (int j = 0; j <= i; j++) {
                        builder.Append(split[j]);
                        if (j != i) builder.Append(COMMAND_PATH_SEPARATOR);
                    }

                    cmd = foundcmd;
                    result_command = builder.ToString();
                    return false;
                }

                dict = foundcmd.SubCommands;
            }

            cmd = foundcmd;
            result_command = path;
            return true;
        }

        private enum WindowChild {
            OutputBox,
            AutocompleteBox,
            InputBox
        }

        private bool _AutocompleteVisible {
            get { return _GetChild<SGroup>(WindowChild.AutocompleteBox).Visible; }
            set { _GetChild<SGroup>(WindowChild.AutocompleteBox).Visible = value; }
        }
        private int _AutocompleteIndex = 0;
        private string _AutocompletePreContent = null;

        private T _GetChild<T>(WindowChild child) where T : SElement {
            return (T)Window[(int)child];
        }

        public string Text {
            get { return _GetChild<STextField>(WindowChild.InputBox).Text; }
            set { _GetChild<STextField>(WindowChild.InputBox).Text = value; }
        }

        private void _DiscardAutocomplete(bool dont_reset_text = false) {
            if (!dont_reset_text) {
                if (_AutocompletePreContent == null) throw new InvalidOperationException("Tried to discard autocomplete with no content");

                Text = _AutocompletePreContent;
            }
            _AutocompleteVisible = false;
            _AutocompleteIndex = 0;
        }

        private bool _CharFitsInLiteralOrSeparatorToken(Parser.Token token, int pos) {
            var fits = false;
            switch(token.Type) {
            case Parser.TokenType.Literal:
                fits = token.FirstLine == 1 && token.LastLine == 1 && (token.FirstCharacter - 1) <= pos && (token.LastCharacter + 1) >= pos;
                break;
            case Parser.TokenType.Separator:
                fits = token.FirstLine == 1 && token.LastLine == 1 && token.FirstCharacter <= pos && token.LastCharacter >= pos;
                break;
            }

            return fits;
        }

        private Parser.Token? _FindCommandEnd(int command_start_idx, List<Parser.Token> tokens) {
            var command_start = tokens[command_start_idx];
            if (command_start.Type != Parser.TokenType.CommandStart) throw new ArgumentException("command_start_idx must point to a CommandStart token");

            Parser.Token? command_end = null;

            int depth = 1;
            for (int i = 0; i < tokens.Count; i++) {
                var token = tokens[i];
                if (token.Type == Parser.TokenType.CommandStart) depth += 1;
                else if (token.Type == Parser.TokenType.CommandEnd) {
                    depth -= 1;
                    if (depth <= 0) {
                        command_end = token;
                        break;
                    }
                }
            }

            return command_end;
        }

        private bool _CharFitsInASTNode(int char_pos, Parser.ASTNode node, bool tight = false) {
            return node.Position.FirstLine == 1 &&
                       node.Position.LastLine == 1 &&
                       char_pos >= (node.Position.FirstChar - (
                           (node is Parser.Argument) ? 1 : 0
                       )) &&
                       char_pos <= (node.Position.LastChar - (
                           tight ? 1 : 0
                       ));
        }

        private bool _CharFitsBetweenASTNodes(int char_pos, Parser.ASTNode left, Parser.ASTNode right) {
            return left.Position.FirstLine == 1 &&
                       left.Position.LastLine == 1 &&
                       right.Position.FirstLine == 1 &&
                       right.Position.LastLine == 1 &&
                       char_pos >= left.Position.LastChar &&
                       char_pos <= right.Position.FirstChar;
        }

        private struct AutocompleteInfo {
            public int ArgIndex; // 0-indexed, 0 is command, 1+ is args
            public Parser.Command CommandNode;
            public Parser.ASTNode ASTNode;
            public int LastCharIndex;
            public int FirstCharIndex;

            public AutocompleteInfo(int arg_index, Parser.Command command_node, Parser.ASTNode ast_node) {
                ArgIndex = arg_index;
                CommandNode = command_node;
                ASTNode = ast_node;
                if (ASTNode != null) {
                    LastCharIndex = ASTNode.Position.LastChar;
                    FirstCharIndex = ASTNode.Position.FirstChar;
                } else {
                    LastCharIndex = CommandNode.Position.LastChar;
                    FirstCharIndex = CommandNode.Position.FirstChar;
                }
            }

            public override string ToString() {
                return $"[AutocompleteInfo] ArgIndex={ArgIndex} CommandNode={CommandNode} ASTNode={ASTNode} LastCharIndex={LastCharIndex}";
            }
        }

        private AutocompleteInfo _GetAutocompleteInfo(Parser.Command cmd) {
            var inbox = _GetChild<STextField>(WindowChild.InputBox);

            var cursor_pos = inbox.CursorIndex; // CursorIndex is 0 based

            // TODO
            // tabbing at: e|[[echo e]cho cho] a b
            // weirdly jumps

            for (int i = 0; i < cmd.Name.Content.Count; i++) {
                var node = cmd.Name.Content[i];

                if (node is Parser.Command && _CharFitsInASTNode(cursor_pos, node, tight: true)) {
                    // forget that shit, just autocomplete this command thank you very much
                    return _GetAutocompleteInfo((Parser.Command)node);
                }
            }

            if (_CharFitsInASTNode(cursor_pos, cmd.Name)) {
                return new AutocompleteInfo(0, cmd, cmd.Name);
            }

            if (cmd.Args.Count >= 1) {
                if (_CharFitsBetweenASTNodes(cursor_pos, cmd.Name, cmd.Args[0])) {
                    return new AutocompleteInfo(1, cmd, cmd.Args[0]);
                } else {
                    int arg_index = 1;
                    for (int i = 0; i < cmd.Args.Count; i++) {
                        for (int j = 0; j < cmd.Args[i].Content.Count; j++) {
                            var node = cmd.Args[i].Content[j];

                            if (node is Parser.Command && _CharFitsInASTNode(cursor_pos, node, tight: true)) {
                                return _GetAutocompleteInfo((Parser.Command)node);
                            }
                        }

                        var arg = cmd.Args[i];
                        if (_CharFitsInASTNode(cursor_pos, arg)) {
                            return new AutocompleteInfo(arg_index, cmd, arg);
                        }

                        if ((i + 1) < cmd.Args.Count && _CharFitsBetweenASTNodes(cursor_pos, arg, cmd.Args[i + 1])) {
                            return new AutocompleteInfo(arg_index, cmd, cmd.Args[i + 1]);
                        }

                        arg_index += 1;
                    }
                }
            }

            return new AutocompleteInfo(cmd.Args.Count, cmd, null);
        }

        private void _DoAutoComplete() {
            var input_box = _GetChild<STextField>(WindowChild.InputBox);
            var info = _GetAutocompleteInfo(_Parser.Parse(CurrentCommandText, lenient: true));
            input_box.MoveCursor(info.LastCharIndex, info.FirstCharIndex - 1); // CursorIndex is 0-indexed
        }

        public void Paragraph() {
            _GetChild<SGroup>(WindowChild.OutputBox).Children.Add(new SLabel());
        }

        public void PrintLine(string txt, int? color = null) {
            var outputbox = _GetChild<SGroup>(WindowChild.OutputBox);
            //if (outputbox.Children.Count == 0) {
            outputbox.Children.Add(new SLabel(txt) {Color = color});
            //  return;
            //} else {
            //    var elem = outputbox.Children[outputbox.Children.Count - 1];

            //    if (elem is SLabel) ((SLabel)elem).Text += txt;
            //    else outputbox.Children.Add(new SLabel(txt));
            //}

            var outbox = _GetChild<SGroup>(WindowChild.OutputBox);
            outbox.ScrollPosition = new Vector2(0, outbox.ContentSize.y);

            outbox.UpdateStyle(); // SGUI BUG! without this scroll breaks with labels with a lot of newlines
        }

        public void PrintError(string txt) {
            if (txt.IndexOf('\n') == -1) {
                PrintLine(txt, 0xFF6E6E);
            } else {
                foreach (var line in txt.Split('\n')) {
                    PrintError(line);
                }
            }
        }

        public string ExecuteCommand(string cmd) {
            return _Executor.ExecuteCommand(_Parser.Parse(cmd));
        }

        public void ExecuteCommandAndPrintResult(string cmd) {
            if (PrintUsedCommand) PrintLine("> " + cmd, color: 0x575757);
            try {
                PrintLine(ExecuteCommand(cmd));
            } catch (Exception e) {
                PrintError("Exception while executing command:");
                PrintError(e.Message);
                PrintError("More detailed info in the log.");

                _Logger.Error($"Exception while running command '{cmd}':");
                _Logger.Error(e.Message);
                var stlines = e.StackTrace.Split('\n');

                for (int i = 0; i < stlines.Length; i++) {
                    _Logger.ErrorIndent(stlines[i]);
                }
            }
        }

        public Command AddGroup(string name) {
            return AddCommand(new Command(name, (args, histindex, self) => {
                PrintError($"Can't execute command group '{name}'. Did you mean:");
                foreach (var cmd in self.SubCommands) {
                    PrintError($"- {name}{COMMAND_PATH_SEPARATOR}{cmd.Key}");
                }
                return null;
            }));
        }

        public Command AddCommand(Command cmd) {
            Commands[cmd.Name] = cmd;
            return cmd;
        }

        public Command AddCommand(string name, Func<List<string>, int?, string> callback) {
            return Commands[name] = new Command(name, callback);
        }

        public Command AddCommand(string name, Func<List<string>, string> callback) {
            return Commands[name] = new Command(name, callback);
        }

        // for the debug/mods command
        private void _GetModInfo(StringBuilder builder, ModLoader.ModInfo info, string indent = "") {
            builder.AppendLine($"{indent}- {info.Name}: {info.Resources.ResourceCount} resources");
            foreach (var mod in info.EmbeddedMods) {
                if (mod.Parent == info) {
                    _GetModInfo(builder, mod, indent + "  ");
                }
            }
        }

        private Logger.Subscriber _LoggerSubscriber;
        private bool _Subscribed = false;
        private static Dictionary<Logger.LogLevel, int> _LoggerColors = new Dictionary<Logger.LogLevel, int> {
            {Logger.LogLevel.Debug, 0x0ADE00},
            {Logger.LogLevel.Info, 0x00ADEE},
            {Logger.LogLevel.Warn, 0xEDA000},
            {Logger.LogLevel.Error, 0xFF1F1F}
        };
        private Logger.LogLevel _LogLevel = Logger.LogLevel.Debug;

        public override SElement CreateWindow() {
            Instance = this;
            var etgmod = Backend.SearchForBackend("ETGMod");

            _LoggerSubscriber = (logger, loglevel, indent, str) => {
                PrintLine(logger.String(loglevel, str, indent: indent), color: _LoggerColors[loglevel]);
            };

            _Executor = new Parser.Executor((name, args, history_index) => {
                Command cmd = ResolveCommand(name);
                if (history_index == null && History.LastIndex > 0) {
                    history_index = History.LastIndex - 1;
                }
                return cmd.Run(args, history_index);
            });

            History = new CommandHistory(_Executor, _Parser);

            AddCommand(new Command("!!", (args, histindex) => {
                if (histindex - 1 < 0) throw new Exception("Can't run previous command (history is empty).");
                return History.Execute(histindex.Value - 1);
            }));

            AddCommand(new Command("!'", (args, histindex) => {
                if (histindex - 1 < 0) throw new Exception("Can't run previous command (history is empty).");
                return History.Entries[histindex.Value - 1];
            }));

            AddCommand(new Command("echo", (args) => {
                return string.Join(" ", args.ToArray());
            })).WithSubCommand(new Command("hello", (args) => {
                return "Hello, world!\nHello, world!\nHello, world!\nHello, world!\nHello, world!\nHello, world!";
            }));

            {
                var g = AddGroup("debug");

                g.WithSubCommand(new Command("parser-bounds-test", (args) => {
                    var text = "echo Hello! \"Hello world!\" This\\ is\\ great \"It\"works\"with\"\\ wacky\" stuff\" \\[\\] \"\\[\\]\" [e[echo c][echo h][echo [echo \"o\"]] \"hel\"[echo lo][echo !]]";
                    CurrentCommandText = text;
                    return null;
                }));

                g.WithSubCommand(new Command("mods", (args) => {
                    var s = new StringBuilder();

                    s.AppendLine("Loaded mods:");
                    foreach (var mod in ETGMod.ModLoader.LoadedMods) {
                        _GetModInfo(s, mod);
                    }
                    return s.ToString();
                }));
            }

            {
                var g = AddGroup("log");
               
                g.WithSubCommand(new Command("sub", (args) => {
                    if (_Subscribed) return "Already subscribed.";
                    Logger.Subscribe(_LoggerSubscriber);
                    _Subscribed = true;
                    return "Done.";
                }));

                g.WithSubCommand(new Command("unsub", (args) => {
                    if (!_Subscribed) return "Not subscribed yet.";
                    Logger.Unsubscribe(_LoggerSubscriber);
                    _Subscribed = false;
                    return "Done.";
                }));

                g.WithSubCommand(new Command("level", (args) => {
                    if (args.Count == 0) {
                        return _LogLevel.ToString().ToLowerInvariant();
                    } else {
                        switch (args[0]) {
                        case "debug": _LogLevel = Logger.LogLevel.Debug; break;
                        case "info": _LogLevel = Logger.LogLevel.Info; break;
                        case "warn": _LogLevel = Logger.LogLevel.Warn; break;
                        case "error": _LogLevel = Logger.LogLevel.Error; break;
                        default: throw new Exception($"Unknown log level '{args[0]}");
                        }
                        return "Done.";
                    }
                }));
            }

            return new SGroup {
                Background = new Color(0, 0f, 0f, 0.8f),

                OnUpdateStyle = elem => {
                    elem.Fill(0);
                },

                Children = {
                    new SGroup { // OutputBox
                        Background = new Color(0, 0, 0, 0),
                        AutoLayout = (self) => self.AutoLayoutVertical,
                        ScrollDirection = SGroup.EDirection.Vertical,
                        OnUpdateStyle = (elem) => {
                            elem.Fill(0);
                            elem.Size -= new Vector2(0, elem.Backend.LineHeight);
                        },
                        Children = {
                            new SLabel($"ETGMod v{etgmod.BestMatch?.Instance.StringVersion ?? "?"}") {Color = 0x00A1E7},
                        }
                    },
                    new SGroup { // AutocompleteBox
                        Background = new Color(0.2f, 0.2f, 0.2f, 0.9f),
                        AutoLayout = (self) => self.AutoLayoutVertical,
                        ScrollDirection = SGroup.EDirection.Vertical,
                        OnUpdateStyle = (elem) => {
                            elem.Size.x = elem.Parent.InnerSize.x;
                            elem.Size.y = elem.Parent.InnerSize.y / 10; // 10%
                            elem.Position.y = elem.Parent.InnerSize.y - elem.Parent[(int)WindowChild.InputBox].Size.y - elem.Size.y;
                        },
                        Children = {},
                        Visible = false
                    },
                    new STextField { // InputBox
                        OverrideTab = true,

                        OnUpdateStyle = (elem) => {
                            elem.Size.x = elem.Parent.InnerSize.x;
                            elem.Position.x = 0;
                            elem.Position.y = elem.Parent.InnerSize.y - elem.Size.y;
                        },

                        OnKey = (self, is_down, key) => {
                            if (!is_down || key == KeyCode.Return || key == KeyCode.KeypadEnter) return;

                            switch(key) {
                            case KeyCode.Home:
                                self.MoveCursor(0);
                                break;
                            case KeyCode.Escape:
                            case KeyCode.F2:
                                Hide();
                                break;
                            case KeyCode.Tab:
                                _DoAutoComplete();
                                break;
                            case KeyCode.UpArrow:
                                History.MoveUp();
                                self.MoveCursor(History.Entry.Length);
                                break;
                            case KeyCode.DownArrow:
                                History.MoveDown();
                                self.MoveCursor(History.Entry.Length);
                                break;
                            default:
                                History.LastEntry = self.Text;
                                History.CurrentIndex = History.LastIndex;
                                break;
                            }

                            self.Text = History.Entry;
                        },

                        OnSubmit = (elem, text) => {
                            if (text.Trim().Length == 0) return;
                            History.Push();
                            ExecuteCommandAndPrintResult(text);
                        }
                    }
                }
            };
        }

        public override void OnShow() {
            Window[(int)WindowChild.InputBox].Focus();
        }

        public override void OnHide() {
            _DiscardAutocomplete(dont_reset_text: true);
            History.LastEntry = Text = "";
        }
    }
}

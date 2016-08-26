#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using SGUI;

public class ETGModDebugLogMenu : ETGModMenu {

    public static ETGModDebugLogMenu Instance { get; protected set; }
    public ETGModDebugLogMenu() {
        Instance = this;
    }

    /// <summary>
    /// All debug logged text lines. Feel free to add your lines here!
    /// </summary>
    protected static List<LoggedText> _AllLoggedText = new List<LoggedText>();

    public static Dictionary<LogType, Color> Colors = new Dictionary<LogType, Color>() {
        { LogType.Log,       new Color(1f, 1f, 1f, 0.8f) },
        { LogType.Assert,    new Color(1f, 0.2f, 0.2f, 1f) },
        { LogType.Error,     new Color(1f, 0.4f, 0.4f, 1f) },
        { LogType.Exception, new Color(1f, 0.1f, 0.1f, 1f) },
        { LogType.Warning,   new Color(1f, 1f, 0.2f, 1f) }
    };

    public override void Start() {
        GUI = new SGroup {
            Visible = false,
            Border = 18f,
            Size = new Vector2(/*don't grow horizontally*/ SGUIRoot.Main.Size.x, /*grow vertically (scroll)*/ 0),
            OnUpdateStyle = (SElement elem) => elem.Fill(),
            AutoLayout = (SGroup g) => g.AutoLayoutVertical,
            ScrollDirection = SGroup.EDirection.Vertical,
            Children = {
                new SLabel("THIS LOG IS <color=#ff0000ff>WORK IN PROGRESS</color>."),
                new SLabel("It drops the Unity OnGUI / IMGUI system for SGUI."),
                new SLabel("Some code may still be missing in the SGUI port."),
            }
        };

        // Add everything that wasn't added yet.
        for (int i = 0; i < _AllLoggedText.Count; i++) {
            _AllLoggedText[i].Start();
        }
    }

    public static void Log(string log) {
        Logger(log, LogType.Log);
    }

    public static void LogWarning(string log) {
        Logger(log, LogType.Warning);
    }

    public static void LogError(string log) {
        Logger(log, LogType.Error);
    }

    protected static string GetStackTrace() {
        string stack = System.Environment.StackTrace;
        int index = stack.LastIndexOfInvariant("at UnityEngine.Debug.Log");
        if (index == -1) {
            index = stack.IndexOfInvariant("at UnityEngine.Application.CallLogCallback");
        }
        if (index == -1) {
            index = stack.IndexOfInvariant("at ETGModDebugLogMenu.Logger");
        }
        return stack.Substring(1 + stack.IndexOf('\n', index));
    }

    public static void Logger(string text, LogType type) { Logger(text, null, type); }
    public static void Logger(string text, string stackTrace, LogType type) {
        stackTrace = GetStackTrace(); // Passed stack trace is empty..?!
        LoggedText entry;

        if (_AllLoggedText.Count != 0) {
            entry = _AllLoggedText[_AllLoggedText.Count - 1];
            if (entry.LogMessage == text &&
                entry.Stacktace == stackTrace &&
                entry.LogType == type) {
                entry.LogCount++;
                return;
            }
        }

        entry = new LoggedText(text, stackTrace, type);
        entry.Start();
        _AllLoggedText.Add(entry);
    }

    protected class LoggedText {

        public string LogMessage;
        public string Stacktace;
        public LogType LogType;

        public bool IsStacktraceShown;

        protected int _LogCount = 1;
        public int LogCount {
            get {
                return _LogCount;
            }
            set {
                _LogCount = value;
                if (GUIMessage != null) {
                    GUIMessage.Text = LogMessageFormatted;
                }
            }
        }

        public string LogMessageFormatted {
            get {
                if (LogCount == 1) {
                    return LogMessage;
                }
                return "(" + LogCount + ") " + LogMessage;
            }
        }

        public SButton GUIMessage;
        public SLabel GUIStacktrace;

        public LoggedText(string logMessage, string stackTrace, LogType type) {
            LogMessage = logMessage;
            Stacktace = stackTrace;
            LogType = type;
        }

        public void Start() {
            if (Instance?.GUI == null) {
                return;
            }

            Color color = Colors[LogType];

            GUIMessage = new SButton(LogMessageFormatted) {
                Parent = Instance.GUI,
                Background = new Color(0, 0, 0, 0),
                Foreground = color,
                OnClick = delegate (SButton button) {
                    IsStacktraceShown = !IsStacktraceShown;
                    Instance.GUI.UpdateStyle();
                }
            };
            GUIStacktrace = new SLabel(Stacktace) {
                Parent = Instance.GUI,
                Foreground = color,
                OnUpdateStyle = delegate (SElement elem) {
                    elem.Size.y = IsStacktraceShown ? elem.Size.y : 0f;
                    elem.Visible = IsStacktraceShown;
                }
            };
        }

    }

}

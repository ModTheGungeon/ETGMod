#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using SGUI;

public class ETGModDebugLogMenu : ETGModMenu {

    /// <summary>
    /// All debug logged text lines. Feel free to add your lines here!
    /// </summary>
    protected static List<LoggedText> _AllLoggedText = new List<LoggedText>();

    public static ETGModDebugLogMenu Instance { get; protected set; }
    public ETGModDebugLogMenu() {
        Instance = this;
    }

    public override void Start() {
        GUI = new SGroup {
            Visible = false,
            Border = 18f,
            OnUpdateStyle = (SElement elem) => elem.Fill(),
            AutoLayout = (SGroup g) => g.AutoLayoutVertical,
            AutoLayoutVerticalStretch = false,
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

    public static void LogError(string log) {
        Logger(log, LogType.Error);
    }

    public static void LogWarning(string log) {
        Logger(log, LogType.Warning);
    }

    protected static string GetStackTrace() {
        string stack = System.Environment.StackTrace;
        int index = stack.LastIndexOfInvariant("at UnityEngine.Debug.Log");
        if (index == -1) {
            index = stack.IndexOfInvariant("at ETGModDebugLogMenu.Logger");
        }
        return stack.Substring(1 + stack.IndexOf('\n', index));
    }

    public static void Logger(string text, LogType type) { Logger(text, null, type); }
    public static void Logger(string text, string stackTrace, LogType type) {
        // Passed stack trace is empty..?!
        LoggedText newText = new LoggedText(text, GetStackTrace(), type);
        newText.Start();
        _AllLoggedText.Add(newText);
    }

    protected class LoggedText {

        public string LogMessage;
        public string Stacktace;
        public bool IsStacktraceShown;
        public int LogCount;
        public LogType LogType;

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

            GUIMessage = new SButton(LogMessage) {
                Parent = Instance.GUI,
                Background = new Color(0, 0, 0, 0),
                OnClick = delegate (SButton button) {
                    IsStacktraceShown = !IsStacktraceShown;
                    Instance.GUI.UpdateStyle();
                }
            };
            GUIStacktrace = new SLabel(Stacktace) {
                Parent = Instance.GUI,
                OnUpdateStyle = delegate (SElement elem) {
                    elem.Size.y = IsStacktraceShown ? elem.Size.y : 0f;
                    elem.Visible = IsStacktraceShown;
                }
            };
        }

        public void SetMessageNumber(int number) {
            /*if (number!=0) {
                MessageLabel.Text=number + LogMessage;
            } else {
                MessageLabel.Text=LogMessage;
            }*/

            LogCount = number;
        }
    }

}

#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using SGUI;

public class ETGModDebugLogMenu : ETGModMenu {

    /// <summary>
    /// All debug logged text lines. Feel free to add your lines here!
    /// </summary>
    private static List<LoggedText> allLoggedText = new List<LoggedText>();

    public static ETGModDebugLogMenu Instance;

    public override void Start() {

        GUI=new SGroup {
            Visible=false,
            OnUpdateStyle=(SElement elem) => elem.Fill(),
            AutoLayout=(SGroup g) => g.AutoLayoutRows,
            ScrollDirection=SGroup.EDirection.Vertical,
            Children= {
                        new SLabel("THIS LOG IS <color=#ff0000ff>WORK IN PROGRESS</color>."),
                        new SLabel("It drops the Unity OnGUI / IMGUI system for SGUI."),
                        new SLabel("Some code may still be missing in the SGUI port."),
                        new SLabel()
                    }
        };

        Instance=this;

    }

    public static void Log(string Log) {
        Logger(Log, StackTraceUtility.ExtractStackTrace(), LogType.Log);
    }

    public static void LogError(string Log) {
        Logger(Log, StackTraceUtility.ExtractStackTrace(), LogType.Error);
    }

    public static void LogWarning(string Log) {
        Logger(Log, StackTraceUtility.ExtractStackTrace(), LogType.Warning);
    }

    public static void Logger(string text, string stackTrace, LogType type) {

        try {
            if (allLoggedText.Count>0&&allLoggedText[allLoggedText.Count-1].LogMessage==text) {
                allLoggedText[allLoggedText.Count-1].SetMessageNumber(allLoggedText[allLoggedText.Count-1].LogCount+1);
            } else {

                SGroup logGroup = new SGroup {
                    AutoLayout=(SGroup g) => g.AutoLayoutRows,
                    AutoGrowDirection=SGroup.EDirection.Both,
                    Children={
                    new SButton(text) { Background=new Color(0,0,0,0) },
                    new SLabel (stackTrace) {Visible=false},
                }
                };

                LoggedText newText = new LoggedText(text, StackTraceUtility.ExtractStackTrace(), type) {
                    MessageLabel=(SButton)logGroup.Children[0],
                    StacktraceLabel=(SLabel)logGroup.Children[1]
                };

                newText.Start();

                //Instance.GUI.Children.Add(logGroup);
            }
        }
        catch (System.Exception e) {
            Instance.GUI.Children.Add(new SLabel(e.ToString()));
        }
    }

    class LoggedText {

        public LoggedText(string logMessage, string stackTrace, LogType type) {
            this.Stacktace=stackTrace;
            this.LogType=type;
        }

        public string LogMessage;
        public string Stacktace;
        public bool IsStacktraceShown = false;
        public int LogCount = 0;
        public LogType LogType;

        public SButton MessageLabel;
        public SLabel StacktraceLabel;

        public void Start() {
            MessageLabel.OnClick+=OnClick;
        }

        void OnClick(SButton button) {
            IsStacktraceShown=!IsStacktraceShown;
            StacktraceLabel.Visible=IsStacktraceShown;
        }

        public void SetMessageNumber(int number) {
            if (number!=0) {
                MessageLabel.Text=number.ToString()+LogMessage;
            } else {
                MessageLabel.Text=LogMessage;
            }

            LogCount=number;
        }
    }

}

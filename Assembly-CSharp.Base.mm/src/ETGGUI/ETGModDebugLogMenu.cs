#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;

public class ETGModDebugLogMenu : IETGModMenu {

    /// <summary>
    /// All debug logged text lines. Feel free to add your lines here!
    /// </summary>
    public static List<string> LoggedText = new List<string>();
    public static Vector2 ScrollPos;

    private static Rect _MainBoxRect = new Rect(16, 16, Screen.width-32, Screen.height-32);
    private static Rect _ViewRect = new Rect(16, 16, Screen.width-32, Screen.height-32);

    public void Start() {
    }

    public void Update() {

    }

    public void OnGUI() {

        //Set rect
        _MainBoxRect = new Rect(16, 16, Screen.width - 32, Screen.height - 32);

        //Draw main box
        _DrawMainBox();

        //Draw the logged text
        _DrawLoggedText();
    }

    public void OnDestroy() {

    }

    private void _DrawMainBox() {
        GUI.Box(_MainBoxRect, string.Empty);
    }

    private void _DrawLoggedText() {

        GUILayout.BeginArea(_MainBoxRect);

        ScrollPos=GUILayout.BeginScrollView(ScrollPos);

        for (int i = 0; i<LoggedText.Count; i++) {
            GUILayout.Label(LoggedText[i]);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    public static void Logger(string text, string stackTrace, LogType type) {
        if (text.Contains("\n")) {
            LoggedText.AddRange(text.Split('\n'));
        } else {
            LoggedText.Add(text);
        }
        if (type==LogType.Error||type==LogType.Exception) {
            LoggedText.AddRange(stackTrace.Split('\n'));
        }
        ScrollPos=new Vector2(ScrollPos.x, _ViewRect.height);
    }

}


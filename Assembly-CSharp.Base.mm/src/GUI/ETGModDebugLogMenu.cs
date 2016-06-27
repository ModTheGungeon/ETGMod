#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;

public class ETGModDebugLogMenu : IETGModMenu {

    /// <summary>
    /// All debug logged text lines. Feel free to add your lines here!
    /// </summary>
    public static List<string> LoggedText = new List<string>();
    public static Vector2 scrollPos;

    private Rect mainBoxRect = new Rect(16, 16, Screen.width - 32, Screen.height - 32);
    private Rect viewRect =    new Rect(16, 16, Screen.width - 32, Screen.height - 32);

    public void Start() {
        Application.logMessageReceived += Logger;
    }

    public void Update() {

    }

    public void OnGUI() {
        //Set rect
        mainBoxRect = new Rect(16, 16, Screen.width - 32, Screen.height - 32);

        //Draw main box
        DrawMainBox();

        //Draw the logged text
        DrawLoggedText();
    }

    public void OnDestroy() {

    }

    private void DrawMainBox() {
        GUI.Box(mainBoxRect, string.Empty);
    }

    private void DrawLoggedText() {
        viewRect = new Rect(0, 0, mainBoxRect.width, 36 * LoggedText.Count);
        scrollPos = GUI.BeginScrollView(mainBoxRect, scrollPos, viewRect);

        for (int i = 0; i < LoggedText.Count; i++) {
            GUILayout.Label(LoggedText[i]);
        }

        GUI.EndScrollView();
    }

    public void Logger(string text, string stackTrace, LogType type) {
        LoggedText.Add(text);
        if (type == LogType.Error || type == LogType.Exception) {
            LoggedText.Add(stackTrace);
        }
        scrollPos = new Vector2(scrollPos.x, viewRect.height);
    }

}


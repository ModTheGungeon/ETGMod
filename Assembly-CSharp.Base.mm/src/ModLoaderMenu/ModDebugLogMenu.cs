using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class ModDebugLogMenu {

    public static List<string> LoggedText = new List<string>();
    public static Vector2 scrollPos;

    private Rect mainBoxRect = new Rect(15, 15, Screen.width-30, Screen.height-30);
    private Rect viewRect = new Rect(15, 15, Screen.width-30, Screen.height-30);

    public void Start() {
        Application.logMessageReceived+=Logger;
    }

    public void Update() {

    }

    public void OnGUI() {

        //Set rect
        mainBoxRect=new Rect(15, 15, Screen.width-30, Screen.height-30);

        //Draw main box
        DrawMainBox();

        //Draw the logged text
        DrawLoggedText();

    }

    public void OnDestroy() {

    }

    private void DrawMainBox() {
        GUI.Box(mainBoxRect, "");
    }

    private void DrawLoggedText() {

        viewRect=new Rect(0, 0, mainBoxRect.width, 30*LoggedText.Count);

        scrollPos=GUI.BeginScrollView(mainBoxRect, scrollPos, viewRect);

        for (int i = 0; i<LoggedText.Count; i++) {
            GUILayout.Label(LoggedText[i]);
        }

        GUI.EndScrollView();
    }

    public void Logger(string text, string stackTrace, LogType type) {
        LoggedText.Add(text);
        if (type==LogType.Error||type==LogType.Exception)
            LoggedText.Add(stackTrace);
        scrollPos=new Vector2(scrollPos.x, viewRect.height);
    }


}


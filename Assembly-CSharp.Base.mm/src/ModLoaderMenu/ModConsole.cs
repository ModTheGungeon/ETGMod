using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class ModConsole {

    public static Dictionary<string,Action<string[]>> AllCommandAttributes = new Dictionary<string, Action<string[]>>();

    public static string DebugInputText = "";

    public static List<string> LoggedText = new List<string>();
    public static Vector2 scrollPos;

    private Rect mainBoxRect = new Rect(15, 15, Screen.width-30, Screen.height-30);
    private Rect inputBox = new Rect(15, Screen.height-30, Screen.width-30, 30);
    private Rect viewRect = new Rect(15, 15, Screen.width-30, Screen.height-30);

    GenericLootTable newTable = new GenericLootTable();

    public void Start() {
        AddCommand("Log", Debug.Log);
        AddCommand("RollDistance", DodgeRollDistance);
        AddCommand("RollSpeed", DodgeRollSpeed);
    }

    public void Update() {
        //Deprecated but can't do much else.
        Input.eatKeyPressOnTextFieldFocus=false;
        if (Input.GetKeyDown(KeyCode.Return)&&DebugInputText.Length>0) {
            string[] parts = DebugInputText.Split(' ');

            string[] param = new string[parts.Length-1];

            for(int i = 1; i < parts.Length; i++) {
                param[i-1]=parts[i];
            }

            if (AllCommandAttributes.ContainsKey(parts[0])) {
                AllCommandAttributes[parts[0]](param);
            }

            DebugInputText="";
            LoggedText.Add("Executed command " + parts[0]);
        }
    }

    public void OnGUI() {
        mainBoxRect=new Rect(15, 15, Screen.width-30, Screen.height-60);
        inputBox = new Rect(15, Screen.height-30, Screen.width-30, 30);

        GUI.Box(mainBoxRect, "");
        DebugInputText=GUI.TextArea(inputBox,DebugInputText);

        viewRect=new Rect(0, 0, mainBoxRect.width, 30*LoggedText.Count);

        scrollPos=GUI.BeginScrollView(mainBoxRect, scrollPos, viewRect);

        for (int i = 0; i<LoggedText.Count; i++) {
            GUILayout.Label(LoggedText[i]);
        }

        GUI.EndScrollView();
    }

    public void OnDestroy() {

    }

    public void AddCommand(string commandName, Action<string[]> method) {
        AllCommandAttributes.Add(commandName, method);
    }

    public static void AddMessageToCommandLog(string msg) {
        LoggedText.Add(msg);
    }

    void DodgeRollDistance(string[] param) {

        Debug.Log(param[0]);

        float parse = float.Parse(param[0]);

        if (GameManager.GameManager_0) {
            if (GameManager.GameManager_0.PlayerController_1) {
                GameManager.GameManager_0.PlayerController_1.dodgeRollStats_0.distance=parse;
            }
        }
    }

    void DodgeRollSpeed(string[] param) {

        Debug.Log(param[0]);

        float parse = float.Parse(param[0]);

        if (GameManager.GameManager_0) {
            if (GameManager.GameManager_0.PlayerController_1) {
                GameManager.GameManager_0.PlayerController_1.dodgeRollStats_0.time=parse;
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Steamworks;

class MultiplayerManager : MonoBehaviour {

    public static bool isOnMainMenu {
        get {
            return Foyer.DoMainMenu&&!Foyer.DoIntroSequence;
        }
    }

    public enum MultiplayerMenuState {
        Closed,
        SelectingMode,
        HostingPublic,
        HostingPrivate
    }

    public MultiplayerMenuState state = MultiplayerMenuState.Closed;

    public static void Create() {
        GameObject newObject = new GameObject();
        newObject.AddComponent<MultiplayerManager>();
        newObject.name="MultiplayerManager";
        DontDestroyOnLoad(newObject);
    }

    public void Awake() {

    }

    public void Start() {

    }

    public void Update() {

        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (state==MultiplayerMenuState.SelectingMode) {
                state=MultiplayerMenuState.Closed;
                patch_MainMenuFoyerController.instance.NewGameButton.IsInteractive=true;
                patch_MainMenuFoyerController.instance.QuitGameButton.IsInteractive=true;
                patch_MainMenuFoyerController.instance.ControlsButton.IsInteractive=true;
            } else {
                state=MultiplayerMenuState.SelectingMode;
            }
        }

    }

    public void OnGUI() {

        if (isOnMainMenu&&state==MultiplayerMenuState.Closed) {

            if (GUI.Button(new Rect(Screen.width-210, Screen.height-110, 200, 100), "Multiplayer"))
                state=MultiplayerMenuState.SelectingMode;

        } else if (state==MultiplayerMenuState.SelectingMode) {

            patch_MainMenuFoyerController.instance.NewGameButton.IsInteractive=false;
            patch_MainMenuFoyerController.instance.QuitGameButton.IsInteractive=false;
            patch_MainMenuFoyerController.instance.ControlsButton.IsInteractive=false;

            GUI.Box(new Rect(15, 15, Screen.width-30, Screen.height-30), "Multiplayer menu");

            if (GUI.Button(new Rect(Screen.width/2-125, Screen.height/2-40, 250, 45), "Host Public Game")) {
                state=MultiplayerMenuState.HostingPublic;
            }

            if (GUI.Button(new Rect(Screen.width/2-125, Screen.height/2+40, 250, 45), "Host Private Game")) {
                state=MultiplayerMenuState.HostingPrivate;
            }

        } else if (state==MultiplayerMenuState.HostingPublic) {
            GUILayout.BeginArea(new Rect(15, 15, Screen.width-30, Screen.height-30));

            

            GUILayout.EndArea();
        } else if (state==MultiplayerMenuState.HostingPrivate) {
            GUILayout.BeginArea(new Rect(15, 15, Screen.width-30, Screen.height-30));

            GUILayout.EndArea();
        }

    }

}


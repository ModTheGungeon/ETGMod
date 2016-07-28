using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InControl;
using UnityEngine;
using Steamworks;

class MultiplayerManager : MonoBehaviour {

    public static bool isOnMainMenu {
        get {
            return Foyer.DoMainMenu&&!Foyer.DoIntroSequence;
        }
    }

    public static bool isPlayingMultiplayer = false;

    public enum MultiplayerMenuState {
        Closed,
        SelectingMode,
        PublicLobby,
        PrivateLobby,
    }

    public static MultiplayerMenuState state = MultiplayerMenuState.Closed;

    Texture2D backgroundTex;

    public static List<string> allText = new List<string>();

    public static void Create() {
        GameObject newObject = new GameObject();
        newObject.AddComponent<MultiplayerManager>();
        newObject.name="MultiplayerManager";
        DontDestroyOnLoad(newObject);
    }

    public void Awake() {

    }

    public void Start() {
        backgroundTex=new Texture2D(1, 1);
        backgroundTex.SetPixel(1, 1, Color.white);
        backgroundTex.Apply();
        SteamHelper.Init();
        PacketHelper.Init();
        StartCoroutine(UpdatePlayerNames());
        StartCoroutine(SendNetworkInput());
        StartCoroutine(SendNetworkInput());
    }

    public void Update() {

        if (Input.GetKeyDown(KeyCode.Escape)) {
            CloseGUI();
        }

        PacketHelper.PacketCollectionThread();
        PacketHelper.RunReadyPackets();

    }

    public IEnumerator UpdatePlayerNames() {
        while (true) {
            if (SteamHelper.isInLobby)
                SteamHelper.UpdatePlayerList();
            yield return new WaitForSecondsRealtime(1);
        }
    }

    public IEnumerator SendNetworkInput() {
        yield return new WaitForSeconds(10);
        while (true) {
                if(BraveInput.PrimaryPlayerInstance && BraveInput.PrimaryPlayerInstance.ActiveActions!=null)
                    NetworkInput.SendUpdatePacket(BraveInput.PrimaryPlayerInstance);
            yield return new WaitForSecondsRealtime(0.125f);
        }
    }

    public static void OpenGUI() {
        patch_MainMenuFoyerController.instance.NewGameButton.IsInteractive=false;
        patch_MainMenuFoyerController.instance.QuitGameButton.IsInteractive=false;
        patch_MainMenuFoyerController.instance.ControlsButton.IsInteractive=false;
    }

    public void CloseGUI() {
        if (state==MultiplayerMenuState.SelectingMode) {
            state=MultiplayerMenuState.Closed;
            patch_MainMenuFoyerController.instance.NewGameButton.IsInteractive=true;
            patch_MainMenuFoyerController.instance.QuitGameButton.IsInteractive=true;
            patch_MainMenuFoyerController.instance.ControlsButton.IsInteractive=true;
        } else if (state!=MultiplayerMenuState.Closed) {
            state=MultiplayerMenuState.SelectingMode;
            SteamHelper.LeaveLobby();
        }
    }

    public void OnApplicationQuit() {
        PacketHelper.StopThread();
    }

    string input = "";

    public void OnGUI() {

        foreach (float f in NetworkInput.directions)
            GUILayout.Label(f.ToString());

        GUILayout.Label("Sent bytes:" + NetworkInput.displayBytesSent);
        GUILayout.Label("Recieved bytes:"+NetworkInput.displayBytesRecieved);
        GUILayout.Label("Global packet ID:"+PacketHelper.GlobalPacketID.ToString());

        if (isOnMainMenu&&state==MultiplayerMenuState.Closed) {

            if (GUI.Button(new Rect(Screen.width-210, Screen.height-110, 200, 100), "Multiplayer"))
                state=MultiplayerMenuState.SelectingMode;

        } else if (state==MultiplayerMenuState.SelectingMode) {

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTex);

            OpenGUI();

            GUI.Box(new Rect(15, 15, Screen.width-30, Screen.height-30), "Multiplayer menu");

            if (GUI.Button(new Rect(Screen.width/2-125, Screen.height/2-40, 250, 45), "Host Public Game")) {
                state=MultiplayerMenuState.PublicLobby;
                SteamHelper.CreateLobby(true);
            }

            if (GUI.Button(new Rect(Screen.width/2-125, Screen.height/2+40, 250, 45), "Host Private Game")) {
                state=MultiplayerMenuState.PrivateLobby;
                SteamHelper.CreateLobby(false);
            }

        } else if (state==MultiplayerMenuState.PublicLobby) {

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTex);

            GUI.Box(new Rect(15, 15, Screen.width-30, Screen.height-30), "Public game");

            GUILayout.BeginArea(new Rect(15, 15, Screen.width-30, Screen.height-30));



            GUILayout.EndArea();
        } else if (state==MultiplayerMenuState.PrivateLobby) {

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTex);

            Rect backgroundBox = new Rect(15, 15, Screen.width-30, Screen.height-30);

            GUI.Box(backgroundBox, "Private game");
            GUILayout.BeginArea(backgroundBox);

            Rect UserListBox = new Rect(15, 30, Screen.width/2-30, 145);

            GUI.Box(UserListBox, "Users in lobby");
            GUILayout.BeginArea(UserListBox);

            GUILayout.Space(25);

            foreach (string s in SteamHelper.playerNamesInLobby)
                GUILayout.Label(s);

            GUILayout.EndArea();

            Rect chatLog = new Rect(15, 190, Screen.width/2-30, Screen.height-235);
            GUI.Box(chatLog, "ChatBox");
            GUILayout.BeginArea(chatLog);

            GUILayout.Space(25);
            bool enteredText = Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Return&&input.Length>0;
            input=GUILayout.TextField(input);

            if (enteredText) {
                string msg = "<"+Steamworks.SteamFriends.GetPersonaName()+">:"+input;
                PacketHelper.SendPacketToPlayersInGame("ChatMessage", Encoding.ASCII.GetBytes(msg), true);
                allText.Add(msg);
                input="";
            }

            List<string> msgs = new List<string>(allText);

            msgs.Reverse();

            foreach (string s in msgs)
                GUILayout.Label(s);

            GUILayout.EndArea();

            Rect optionsBox = new Rect(Screen.width/2+45, 30, Screen.width/2-90, Screen.height-75);

            GUI.Box(optionsBox, "Options");
            GUILayout.BeginArea(optionsBox);

            GUILayout.Space(35);

            if (GUILayout.Button("Invite friends")) {
                SteamHelper.OpenInviteMenu();
            }

            if(GUILayout.Button("Start game")) {
                state=MultiplayerMenuState.Closed;
                patch_MainMenuFoyerController.instance.NewGameButton.IsInteractive=true;
                patch_MainMenuFoyerController.instance.QuitGameButton.IsInteractive=true;
                patch_MainMenuFoyerController.instance.ControlsButton.IsInteractive=true;
            }

            GUILayout.EndArea();

            GUILayout.EndArea();
        }
    }

}


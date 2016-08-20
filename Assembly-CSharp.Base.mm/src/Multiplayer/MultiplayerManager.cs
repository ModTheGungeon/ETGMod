using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InControl;
using UnityEngine;
using Steamworks;
using ETGMultiplayer;

public class MultiplayerManager : MonoBehaviour {

    public static MultiplayerManager Instance;

    public static bool MultiplayerEnabled = false;

    public static bool IsOnMainMenu {
        get {
            return Foyer.DoMainMenu && !Foyer.DoIntroSequence;
        }
    }

    public static bool IsPlayingMultiplayer = false;

    public enum MultiplayerMenuState {
        Closed,
        SelectingMode,
        PublicLobby,
        PrivateLobby
    }

    public static MultiplayerMenuState State = MultiplayerMenuState.Closed;

    protected Texture2D _BackgroundTex;

    public static List<string> AllText = new List<string>();

    public static void Create() {
        if (MultiplayerEnabled) {
            Instance = new GameObject("MultiplayerManager").AddComponent<MultiplayerManager>();
        }
    }

    public void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    public void Start() {
        _BackgroundTex = new Texture2D(1, 1);
        _BackgroundTex.SetPixel(0, 0, Color.white);
        _BackgroundTex.Apply();
        SteamHelper.Init();
        PacketHelper.Init();
        StartCoroutine(UpdatePlayerNames());
        StartCoroutine(SendNetworkInput());
    }

    public void Update() {
        if (!SteamManager.Initialized)
            return;

        if (Input.GetKeyDown(KeyCode.Escape)) {
            CloseGUI();
        }

        PacketHelper.PacketCollectionThread();
        PacketHelper.RunReadyPackets();
    }

    public bool KeepUpdatingPlayerNames;
    public IEnumerator UpdatePlayerNames() {
        KeepUpdatingPlayerNames = true;
        while (KeepUpdatingPlayerNames) {
            if (SteamHelper.isInLobby)
                SteamHelper.UpdatePlayerList();
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    public bool KeepSendingNetworkInput;
    public IEnumerator SendNetworkInput() {
        KeepSendingNetworkInput = true;
        yield return new WaitForSeconds(10f);
        NetworkInput.SyncPos =new Position(528f, 326f);
        while (KeepSendingNetworkInput) {
            if (BraveInput.PrimaryPlayerInstance && BraveInput.PrimaryPlayerInstance.ActiveActions != null)
                NetworkInput.SendUpdatePacket(BraveInput.PrimaryPlayerInstance);
            yield return new WaitForSecondsRealtime(0.025f);
        }
    }

    public static void OpenGUI() {
        patch_MainMenuFoyerController.Instance.NewGameButton.IsInteractive = false;
        patch_MainMenuFoyerController.Instance.QuitGameButton.IsInteractive = false;
        patch_MainMenuFoyerController.Instance.ControlsButton.IsInteractive = false;
    }

    public void CloseGUI() {
        if (State == MultiplayerMenuState.SelectingMode) {
            State = MultiplayerMenuState.Closed;
            patch_MainMenuFoyerController.Instance.NewGameButton.IsInteractive = true;
            patch_MainMenuFoyerController.Instance.QuitGameButton.IsInteractive = true;
            patch_MainMenuFoyerController.Instance.ControlsButton.IsInteractive = true;
        } else if (State != MultiplayerMenuState.Closed) {
            State = MultiplayerMenuState.SelectingMode;
            SteamHelper.LeaveLobby();
        }
    }

    public void OnApplicationQuit() {
        PacketHelper.StopThread();
    }

    private string _Input;

    public void OnGUI() {
        if (!SteamManager.Initialized)
            return;

        GUILayout.Label(NetworkInput.displayBytesRecieved + "\n" + NetworkInput.displayBytesSent);

        if (IsOnMainMenu && State == MultiplayerMenuState.Closed) {
            
            if (GUI.Button(new Rect(Screen.width - 210f, Screen.height - 110f, 200f, 100f), "Multiplayer"))
                State = MultiplayerMenuState.SelectingMode;

        } else if (State == MultiplayerMenuState.SelectingMode) {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _BackgroundTex);
            OpenGUI();
            GUI.Box(new Rect(15f, 15f, Screen.width - 30f, Screen.height - 30f), "Multiplayer menu");
            if (GUI.Button(new Rect(Screen.width / 2f - 125f, Screen.height / 2f - 40f, 250f, 45f), "Host Public Game")) {
                State = MultiplayerMenuState.PublicLobby;
                SteamHelper.CreateLobby(true);
            }
            if (GUI.Button(new Rect(Screen.width / 2f - 125f, Screen.height / 2f + 40f, 250f, 45f), "Host Private Game")) {
                State = MultiplayerMenuState.PrivateLobby;
                SteamHelper.CreateLobby(false);
            }

        } else if (State == MultiplayerMenuState.PublicLobby) {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _BackgroundTex);
            GUI.Box(new Rect(15f, 15f, Screen.width - 30f, Screen.height - 30f), "Public game");
            GUILayout.BeginArea(new Rect(15f, 15f, Screen.width - 30f, Screen.height - 30f));
            GUILayout.EndArea();

        } else if (State == MultiplayerMenuState.PrivateLobby) {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _BackgroundTex);
            Rect backgroundBox = new Rect(15f, 15f, Screen.width - 30f, Screen.height - 30f);

            GUI.Box(backgroundBox, "Private game");
            GUILayout.BeginArea(backgroundBox);

            Rect UserListBox = new Rect(15f, 30f, Screen.width / 2f - 30f, 145f);
            GUI.Box(UserListBox, "Users in lobby");
            GUILayout.BeginArea(UserListBox);
            GUILayout.Space(25f);
            for (int i = 0; i < SteamHelper.playerNamesInLobby.Count; i++) {
                GUILayout.Label(SteamHelper.playerNamesInLobby[i]);
            }
            GUILayout.EndArea();

            Rect chatLog = new Rect(15f, 190f, Screen.width / 2f - 30f, Screen.height - 235f);
            GUI.Box(chatLog, "ChatBox");
            GUILayout.BeginArea(chatLog);
            GUILayout.Space(25f);
            bool sendText = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && _Input.Length > 0;
            _Input = GUILayout.TextField(_Input);
            if (sendText) {
                string msg = "<" + SteamFriends.GetPersonaName() + ">:" + _Input;
                PacketHelper.SendRPCToPlayersInGame("ChatMessage", true, msg);
                AllText.Add(msg);
                _Input = "";
            }
            for (int i = AllText.Count - 1; 0 <= i; i--) {
                GUILayout.Label(AllText[i]);
            }
            GUILayout.EndArea();

            Rect optionsBox = new Rect(Screen.width / 2f + 45f, 30f, Screen.width / 2f - 90f, Screen.height - 75f);
            GUI.Box(optionsBox, "Options");
            GUILayout.BeginArea(optionsBox);
            GUILayout.Space(35);
            if (GUILayout.Button("Invite friends")) {
                SteamHelper.OpenInviteMenu();
            }
            if(GUILayout.Button("Start game")) {
                State=MultiplayerMenuState.Closed;
                patch_MainMenuFoyerController.Instance.NewGameButton.IsInteractive=true;
                patch_MainMenuFoyerController.Instance.QuitGameButton.IsInteractive=true;
                patch_MainMenuFoyerController.Instance.ControlsButton.IsInteractive=true;
            }
            GUILayout.EndArea();

            GUILayout.EndArea();
        }
    }

}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;
using ETGMultiplayer;

class SteamHelper {

    public static List<CSteamID> playersInLobby = new List<CSteamID>();
    public static List<CSteamID> playersInLobbyWithoutMe = new List<CSteamID>();
    public static List<string> playerNamesInLobby = new List<string>();
    public static CSteamID CurrentLobby;

    public static bool isInLobby = false;

    //--------------- Callbacks ---------------

    public static Callback<GameLobbyJoinRequested_t> lobbyJoinRequested;
    public static Callback<P2PSessionRequest_t> sessionRequest;

    public static CallResult<LobbyCreated_t> lobbyCreated;
    public static CallResult<LobbyEnter_t> lobbyEntered;

    //-----------------------------------------

    public static void Init() {

        lobbyJoinRequested=Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequest);
        sessionRequest=Callback<P2PSessionRequest_t>.Create(P2PConnectionReqest);

        lobbyCreated=CallResult<LobbyCreated_t>.Create(OnCreatedLobby);
        lobbyEntered=CallResult<LobbyEnter_t>.Create(OnLobbyEntered);

        for (int i = 0; i<ETGMod.LaunchArguments.Length; i++) {
            if (ETGMod.LaunchArguments[i]=="+connect_lobby") {
                ulong parsed = ulong.Parse(ETGMod.LaunchArguments[i+1]);
                CSteamID lobbyID = new CSteamID(parsed);
                MultiplayerManager.State = MultiplayerManager.MultiplayerMenuState.PrivateLobby;
                JoinLobby(lobbyID);
            }
        }
    }

    public static void UpdatePlayerList() {
        playersInLobby.Clear();
        playersInLobbyWithoutMe.Clear();
        playerNamesInLobby.Clear();
        int number = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
        CSteamID myID = SteamUser.GetSteamID();
        for (int i = 0; i<number; i++) {
            CSteamID id = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i);

            playersInLobby.Add(id);
            if (id!=myID)
                playersInLobbyWithoutMe.Add(id);

            playerNamesInLobby.Add(SteamFriends.GetFriendPersonaName(id));
        }
    }

    public static void JoinLobby(CSteamID ID) {
        SteamAPICall_t handle = SteamMatchmaking.JoinLobby(ID);
        lobbyEntered.Set(handle);
    }

    public static void LeaveLobby() {
        SteamMatchmaking.LeaveLobby(CurrentLobby);
        isInLobby=false;
        PacketHelper.GlobalPacketID=0;
        MultiplayerManager.AllText.Clear();
        MultiplayerManager.IsPlayingMultiplayer=false;
    }

    public static void CreateLobby(bool isPublic) {
        Debug.Log("Creating lobby");
        SteamAPICall_t handle = SteamMatchmaking.CreateLobby(isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly, 2);
        lobbyCreated.Set(handle);
    }

    //This will open the menu to invite other players to your lobby.
    public static void OpenInviteMenu() {
        Debug.Log("Opening invite menu");
        SteamFriends.ActivateGameOverlay("LobbyInvite");
    }

    //This is called when you're invited to a lobby, and then accept the invite..
    static void OnLobbyJoinRequest(GameLobbyJoinRequested_t param) {
        Debug.Log("Accepted invite to lobby, joining.");
        if (MultiplayerManager.IsOnMainMenu) {
            JoinLobby(param.m_steamIDLobby);
            MultiplayerManager.State=MultiplayerManager.MultiplayerMenuState.PrivateLobby;
            MultiplayerManager.OpenGUI();
        }
    }

    //This is called when you make a new lobby.
    static void OnCreatedLobby(LobbyCreated_t param, bool fail) {
        Debug.Log("Created a lobby, joining");
        JoinLobby(new CSteamID(param.m_ulSteamIDLobby));
    }

    //This is called when you enter a lobby
    static void OnLobbyEntered(LobbyEnter_t param, bool fail) {
        Debug.Log("Joined lobby");
        isInLobby=true;
        CurrentLobby=new CSteamID(param.m_ulSteamIDLobby);
        MultiplayerManager.IsPlayingMultiplayer=true;
    }

    static void P2PConnectionReqest(P2PSessionRequest_t param) {
        Debug.Log(param.m_steamIDRemote + " is requesting a P2P connection, let's accept");
        SteamNetworking.AcceptP2PSessionWithUser(param.m_steamIDRemote);
        PacketHelper.SendRPCToPlayersInGame("ChatMessage","Player joined game",true);
        MultiplayerManager.IsPlayingMultiplayer=true;
    }

}


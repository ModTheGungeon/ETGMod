using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using ETGMultiplayer;

public class SteamHelper {

    public static List<CSteamID> PlayersInLobby = new List<CSteamID>();
    public static List<CSteamID> PlayersInLobbyWithoutMe = new List<CSteamID>();
    public static List<string> PlayerNamesInLobby = new List<string>();
    public static CSteamID CurrentLobby;

    public static bool IsInLobby = false;

    //--------------- Callbacks ---------------

    public static Callback<GameLobbyJoinRequested_t> LobbyJoinRequested;
    public static Callback<P2PSessionRequest_t> SessionRequest;

    public static CallResult<LobbyCreated_t> LobbyCreated;
    public static CallResult<LobbyEnter_t> LobbyEntered;

    //-----------------------------------------

    public static void Init() {
        LobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequest);
        SessionRequest = Callback<P2PSessionRequest_t>.Create(P2PConnectionReqest);

        LobbyCreated = CallResult<LobbyCreated_t>.Create(OnCreatedLobby);
        LobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);

        for (int i = 0; i < ETGMod.LaunchArguments.Length; i++) {
            if (ETGMod.LaunchArguments[i] == "+connect_lobby") {
                //MultiplayerManager.State = MultiplayerManager.MultiplayerMenuState.PrivateLobby;
                JoinLobby(new CSteamID(ulong.Parse(ETGMod.LaunchArguments[i + 1])));
            }
        }
    }

    public static void UpdatePlayerList() {
        PlayersInLobby.Clear();
        PlayersInLobbyWithoutMe.Clear();
        PlayerNamesInLobby.Clear();
        int number = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
        CSteamID myID = SteamUser.GetSteamID();
        for (int i = 0; i < number; i++) {
            CSteamID id = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i);

            PlayersInLobby.Add(id);
            if (id != myID) PlayersInLobbyWithoutMe.Add(id);

            PlayerNamesInLobby.Add(SteamFriends.GetFriendPersonaName(id));
        }
    }

    public static void JoinLobby(CSteamID ID) {
        SteamAPICall_t handle = SteamMatchmaking.JoinLobby(ID);
        LobbyEntered.Set(handle);
    }

    public static void LeaveLobby() {
        SteamMatchmaking.LeaveLobby(CurrentLobby);
        IsInLobby = false;
        PacketHelper.GlobalPacketID = 0;
        /*MultiplayerManager.AllText.Clear();
        MultiplayerManager.IsPlayingMultiplayer = false;*/
    }

    public static void CreateLobby(bool isPublic) {
        Debug.Log("Creating lobby");
        SteamAPICall_t handle = SteamMatchmaking.CreateLobby(isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly, 2);
        LobbyCreated.Set(handle);
    }

    /// <summary>
    /// This will open the menu to invite other players to your lobby.
    /// </summary>
    public static void OpenInviteMenu() {
        Debug.Log("Opening invite menu");
        SteamFriends.ActivateGameOverlay("LobbyInvite");
    }

    /// <summary>
    /// This is called when you're invited to a lobby, and then accept the invite.
    /// </summary>
    public static void OnLobbyJoinRequest(GameLobbyJoinRequested_t param) {
        Debug.Log("Accepted invite to lobby, joining.");
        /*if (MultiplayerManager.IsOnMainMenu) {
            JoinLobby(param.m_steamIDLobby);
            MultiplayerManager.State=MultiplayerManager.MultiplayerMenuState.PrivateLobby;
            MultiplayerManager.OpenGUI();
        }*/
    }

    /// <summary>
    /// This is called when you make a new lobby.
    /// </summary>
    public static void OnCreatedLobby(LobbyCreated_t param, bool fail) {
        Debug.Log("Created a lobby, joining");
        JoinLobby(new CSteamID(param.m_ulSteamIDLobby));
    }

    /// <summary>
    /// This is called when you enter a lobby.
    /// </summary>
    public static void OnLobbyEntered(LobbyEnter_t param, bool fail) {
        Debug.Log("Joined lobby");
        IsInLobby = true;
        CurrentLobby = new CSteamID(param.m_ulSteamIDLobby);
        //MultiplayerManager.IsPlayingMultiplayer = true;
    }

    public static void P2PConnectionReqest(P2PSessionRequest_t param) {
        Debug.Log(param.m_steamIDRemote + " is requesting a P2P connection, let's accept");
        SteamNetworking.AcceptP2PSessionWithUser(param.m_steamIDRemote);
        PacketHelper.SendRPCToPlayersInGame("ChatMessage", "Player joined game", true);
        //MultiplayerManager.IsPlayingMultiplayer = true;
    }

}


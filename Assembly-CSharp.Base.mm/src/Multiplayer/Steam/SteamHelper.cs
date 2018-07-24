using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using ETGMultiplayer;

public class SteamHelper {

    //--------------- Callbacks ---------------

    public static Callback<GameLobbyJoinRequested_t> LobbyJoinRequested;
    public static Callback<P2PSessionRequest_t> SessionRequest;

    public static CallResult<LobbyCreated_t> LobbyCreated;
    public static CallResult<LobbyEnter_t> LobbyEntered;

    //-----------------------------------------

    public static void Init() {
        LobbyHelper.Init();
    }

}


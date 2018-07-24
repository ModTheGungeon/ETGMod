using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;

namespace ETGMultiplayer {
    class LobbyHelper {

        public static Dictionary<string, CSteamID> otherPlayers = new Dictionary<string, CSteamID>();

        // public static CSteamID currentLobby;

        public static bool isInLobby = false;

        public static void Init() {
            SteamHelper.LobbyCreated=CallResult<LobbyCreated_t>.Create(OnCreatedLobby);
            SteamHelper.LobbyEntered=CallResult<LobbyEnter_t>.Create(OnJoinedLobby);
        }

        public static void JoinLobby(long ID) {

        }

        public static void LeaveLobby() {

        }

        public static void CreateLobby(bool isPublic=false) {
            Debug.Log("Creating lobby");
            SteamAPICall_t handle = SteamMatchmaking.CreateLobby(isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly, 2);
            SteamHelper.LobbyCreated.Set(handle);
        }

        private static void OnJoinedLobby(LobbyEnter_t t, bool failure) {
            Debug.Log("Joined Lobby");


        }

        private static void OnLeftLobby() {

        }

        private static void OnCreatedLobby(LobbyCreated_t t, bool failed) {
            Debug.Log("Created lobby");
            Debug.Log("Lobby ID:" + t.m_ulSteamIDLobby);
        }

    }
}

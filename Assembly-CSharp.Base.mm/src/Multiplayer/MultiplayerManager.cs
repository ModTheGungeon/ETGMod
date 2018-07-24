using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ETGMultiplayer {
    class MultiplayerManager : MonoBehaviour {

        public static MultiplayerManager Instance;

        public static void Create() {
            GameObject mpManagerObject = new GameObject() {
                name="_MPManger"
            };

            Instance=mpManagerObject.AddComponent<MultiplayerManager>();
            mpManagerObject.AddComponent<MultiplayerGUI>();

            SteamHelper.Init();
        }

        public void Awake() {

        }

        public void Start() {

        }

        public void Update() {
            if (Input.GetKey(KeyCode.LeftShift)&&Input.GetKeyDown(KeyCode.M))
                LobbyHelper.CreateLobby();
        }

        public void CreateAndJoinLobby() {

        }

        public void JoinLobby() {

        }

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SGUI;

namespace ETGMultiplayer {
    class MultiplayerGUI : MonoBehaviour {

        public static MultiplayerGUI Instance;

        SGroup MainGUI;

        SGroup LobbyGUI;
        SGroup InGameGUI;

        public void Awake() {
            Instance=this;
        }

        public void Start() {

        }

        public void Update() {

        }

        public void OnGUI() {

        }

    }
}

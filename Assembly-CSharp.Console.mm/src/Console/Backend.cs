using System;
using ETGMod;
using SGUI;
using UnityEngine;

namespace ETGMod.Console {
    public partial class Console : Backend {
        public static Logger Logger = new Logger("Console");
        public override Version Version { get { return new Version(0, 1, 0); } }

        public override void Loaded() {
            Logger.Info($"Console v{Version} loaded");
            GUI.GUI.MenuController.AddMenu<ConsoleMenu>(new KeyCode[] { KeyCode.F2, KeyCode.BackQuote, KeyCode.Slash });
        }
    }
}

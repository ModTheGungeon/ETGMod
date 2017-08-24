using System;
using ETGMod;
using SGUI;
using UnityEngine;

namespace ETGMod.GUI {
    public class GUI : Backend {
        public static Logger Logger = new Logger("GUI");
        public static SGUIRoot GUIRoot;
        public static MenuController MenuController;
        private static GameObject _GameObject = new GameObject("GUI");

        public override Version Version { get { return new Version(0, 1, 0); } }

        public override void Loaded() {}

        public override void NoBackendsLoadedYet() {
            Logger.Info("Initializing SGUI");

            GUIRoot = SGUIRoot.Setup();
            SGUIIMBackend.GetFont = (SGUIIMBackend backend) => FontCache.GungeonFont ?? (FontCache.GungeonFont = FontConverter.DFFontToUnityFont((dfFont)patch_MainMenuFoyerController.Instance.VersionLabel.Font, 2));

            Logger.Info("Initializing menu controller");
            MenuController = _GameObject.AddComponent<MenuController>();
            DontDestroyOnLoad(MenuController);
        }
    }
}

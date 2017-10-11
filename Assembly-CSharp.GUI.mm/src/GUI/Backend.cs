using System;
using System.Collections;
using System.IO;
using ETGMod;
using SGUI;
using UnityEngine;

namespace ETGMod.GUI {
    public class GUI : Backend {
        public static Logger Logger = new Logger("GUI");
        public static SGUIRoot GUIRoot;
        public static MenuController MenuController;
        public static NotificationController NotificationController;
        private static GameObject _GameObject = new GameObject("GUI");

        public override Version Version { get { return new Version(0, 1, 0); } }

        public override void Loaded() {}

        private IEnumerator _NotifyETGModLoaded() {
            yield return new WaitForSeconds(5f);

            NotificationController.Notify(new Notification(
                image: _Bullet,
                title: "ETGMod loaded",
                content: "Thanks for using Mod the Gungeon!"
            ));
        }

        public override void NoBackendsLoadedYet() {
            Logger.Info("Initializing SGUI");

            GUIRoot = SGUIRoot.Setup();
            SGUIIMBackend.GetFont = (SGUIIMBackend backend) => FontCache.GungeonFont ?? (FontCache.GungeonFont = FontConverter.DFFontToUnityFont((dfFont)CorePatches.MainMenuFoyerController.Instance.VersionLabel.Font, 2));

            Logger.Info("Initializing menu controller");
            MenuController = _GameObject.AddComponent<MenuController>();
            DontDestroyOnLoad(MenuController);

            Logger.Info("Initializing notification controller");
            NotificationController = _GameObject.AddComponent<NotificationController>();
            DontDestroyOnLoad(NotificationController);

            _Bullet = UnityUtil.LoadTexture2D(Path.Combine(Paths.ResourcesFolder, "bullet.png"));

            EventHooks.MainMenuLoadedFirstTime += (obj) => {
                Coroutine.Start(_NotifyETGModLoaded());
            };

            AppDomain.CurrentDomain.UnhandledException += (obj, e) => {
                NotificationController.Notify(new Notification(
                    title: "An unhandled error has occured",
                    content: "More information in the log."
                ) {
                    BackgroundColor = UnityUtil.NewColorRGB(57, 7, 7)
                });
            };

            ETGMod.ModLoader.LuaError += (info, method, e) => {
                NotificationController.Notify(new Notification(
                    title: $"An error has occured while loading mod {info.Name}",
                    content: $"The '{method}' method had raised an error. More information in the log."
                ) {
                    BackgroundColor = UnityUtil.NewColorRGB(57, 7, 7)
                });
            };

            ETGMod.ErrorLoadingMod += (filename, e) => {
                NotificationController.Notify(new Notification(
                    title: $"An error has occured while loading '{filename}'",
                    content: "More information in the log."
                ) {
                    BackgroundColor = UnityUtil.NewColorRGB(57, 7, 7)
                });
            };

            ETGMod.ErrorCreatingModsDirectory += (e) => {
                NotificationController.Notify(new Notification(
                    title: $"An error has occured while trying to create the Mods directory",
                    content: "More information in the log."
                ) {
                    BackgroundColor = UnityUtil.NewColorRGB(57, 7, 7)
                });
            };
        }

        private int _TestID = 1;
        private Texture _Bullet;
        public void Update() {
            if (Input.GetKeyDown(KeyCode.F9)) {
                NotificationController.Notify(new Notification(
                    _TestID % 2 == 0 ? _Bullet : null,
                    $"Achivement Unlocked",
                    "Unlocked achievement 'Biggest Wallet'\nCollect 300 casings in one run."
                ) {
                    BackgroundColor = UnityUtil.NewColorRGB(238, 188, 29),
                    TitleColor = UnityUtil.NewColorRGB(0, 0, 0),
                    ContentColor = UnityUtil.NewColorRGB(55, 55, 55)
                });
                _TestID += 1;
            }
        }
    }
}

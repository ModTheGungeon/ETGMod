using System;
namespace ETGMod {
    public static class EventHooks {
        private static Logger _Logger = new Logger("EventHooks");

        public static Action<MainMenuFoyerController> MainMenuLoadedFirstTime;
        public static void InvokeMainMenuLoadedFirstTime(MainMenuFoyerController menu) {
            _Logger.Debug(nameof(MainMenuLoadedFirstTime));
            MainMenuLoadedFirstTime?.Invoke(menu);
        }

        public static Action<GameManager> GameStarted;
        public static void InvokeGameStarted(GameManager game) {
            _Logger.Debug(nameof(GameStarted));
            GameStarted?.Invoke(game);
        }
    }
}

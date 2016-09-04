using System;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod Chest configuration.
    /// </summary>
    public static class Chest {
        public static Action<global::Chest> OnPostSpawn;
        public delegate bool DOnPreOpen(bool shouldOpen, global::Chest chest, PlayerController player);
        public static DOnPreOpen OnPreOpen;
        public static Action<global::Chest, PlayerController> OnPostOpen;
    }

}

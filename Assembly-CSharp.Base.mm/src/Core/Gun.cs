using System;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod Gun configuration.
    /// </summary>
    public static class Gun {
        public static Action<PlayerController, global::Gun> OnPostFired;
        public static Action<PlayerController, global::Gun> OnFinishAttack;
        public static Action<PlayerController, global::Gun> OnInit;
    }

}

using System;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod AIActor configuration.
    /// </summary>
    public static class AIActor {
        public static Action<global::AIActor> OnPreStart;
        public static Action<global::AIActor> OnPostStart;
        public static Action<global::AIActor> OnBlackPhantomnessCheck;
    }

    // Extension methods

    public static void BecomeBlackPhantom(this global::AIActor actor) {
        ((patch_AIActor) actor).INTERNAL_BecomeBlackPhantom();
    }

}

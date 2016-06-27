using System;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Mono.Cecil;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod player configuration.
    /// </summary>
    public static class Player {
        public static bool? InfiniteKeys;
        public static string QuickstartReplacement = "PlayerNinja";
        public static string CoopReplacement = "PlayerCosmonaut";
    }

}

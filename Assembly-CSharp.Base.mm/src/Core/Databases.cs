using System;
using UnityEngine;
using System.Collections.Generic;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod database configuration.
    /// </summary>
    public static class Databases {

        public readonly static ItemDB Items = new ItemDB();
        public readonly static StringDB Strings = new StringDB();
        public readonly static EnemyDB Enemies = new EnemyDB();
        public readonly static CharacterDB Characters = new CharacterDB();
    }

}

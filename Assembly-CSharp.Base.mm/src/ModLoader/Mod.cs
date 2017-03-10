using System;
namespace ETGMod.ModLoader {
    public abstract class Mod {
        public static Metadata Metadata;

        public abstract void Load();
        public abstract void Unload();
    }
}

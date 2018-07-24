using System;

namespace MonoMod {
    /// <summary>
    /// MonoMod on platform ("#ifdef PLATFORM") attribute.
    /// Apply it onto a type / method / field and it gets ignored on non-matching platforms.
    /// </summary>
    public class MonoModOnPlatform : Attribute {
        public MonoModOnPlatform(params Platform[] p) {
        }
    }
}


using System;
using UnityEngine;

namespace ETGMod {
    public abstract class Mod : MonoBehaviour {
        public ModLoader.ModInfo Info;

        public string Name {
            get {
                return Info.ModMetadata.Name;
            }
        }

        protected T Load<T>(string relative_path) {
            if (Info == null) throw new InvalidOperationException("Tried calling Load from a Mod subclass before the mod is loaded");
            return Info.Load<T>(relative_path);
        }

        abstract public void Loaded();
        abstract public void Unloaded();
    }
}

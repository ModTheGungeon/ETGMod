using System;
public static partial class ETGMod {
    public static partial class Assets {
        public abstract class Protocol {
            public Protocol(string prefix) {
                Prefix = prefix;
            }

            public string Prefix;

            public UnityEngine.Object Get(string path) {
                if (!path.StartsWithInvariant(Prefix)) return null;
                return GetObject(path.Substring(Prefix.Length));
            }
            internal abstract UnityEngine.Object GetObject(string path);
        }

        public class ItemProtocol : Protocol {
            public ItemProtocol() : base("ITEMDB:") {}
            internal override UnityEngine.Object GetObject(string path) {
                var moditem = Databases.Items.GetModItemByName(path);
                if (moditem == null) {
                    return null;
                }
                return UnityEngine.Object.Instantiate(moditem);
            }
        }
    }
}
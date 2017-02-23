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

        public class EnemyGuidProtocol : Protocol {
            public EnemyGuidProtocol() : base("ENEMYDB.GUID:") {}
            internal override UnityEngine.Object GetObject(string path) {
                global::AIActor modenemy = Databases.Enemies.GetModEnemyByGuid(path);
                if (modenemy == null) {
                    return null;
                }
                global::AIActor thingy = UnityEngine.Object.Instantiate(modenemy);
                thingy.gameObject.SetActive(true);
                return thingy;
            }
        }

        public class CharacterProtocol : Protocol {
            public CharacterProtocol() : base("CHARACTERDB:") { }
            internal override UnityEngine.Object GetObject(string path) {
                UnityEngine.GameObject modcharacter = Databases.Characters.GetModCharacterByName(path);
                if (modcharacter == null) {
                    return null;
                }
                UnityEngine.GameObject thingy = UnityEngine.Object.Instantiate(modcharacter);
                return thingy;
            }
        }    
    }
}
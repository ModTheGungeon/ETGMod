using System;
using System.Collections;
using System.Collections.Generic;
using ETGMod.API;
using MonoMod;
using UnityEngine;

namespace ETGMod.Lua {
    public class ItemsDB : IDisposable {
        public Dictionary<string, LuaPassiveItem> Passives = new Dictionary<string, LuaPassiveItem>();
        private static Logger _Logger = new Logger("ItemsDB");

        public void Dispose() {
            foreach (var pair in Passives) {
                _Logger.Debug($"Removing passive item {pair.Key}");
                for (int i = EncounterDatabase.Instance.Entries.Count - 1; i >= 0; i--) {
                    var ent = EncounterDatabase.Instance.Entries[i];
                    if (ent.myGuid == $"ETGMod Lua Item {pair.Key}") {
                        _Logger.Debug($"Removing encounter database entry");
                        EncounterDatabase.Instance.Entries.RemoveAt(i);
                        break;
                    }
                }
                ETGMod.Items.Remove(pair.Key);
                pair.Value.Dispose();
            }
        }

        public LuaPassiveItem CreatePassive(ModLoader.ModInfo mod, string shortname) {
            var full_id = shortname;
            if (!shortname.Contains(":")) full_id = $"{mod.Namespace}:{shortname}";

            IDPool<object>.VerifyID(full_id);

            var prefab = CustomPickupObject.Create<LuaPassiveItem>();

            UnityEngine.Object.DontDestroyOnLoad(prefab);
            prefab.PickupObjectId = PickupObjectDatabase.Instance.Objects.Count;
            PickupObjectDatabase.Instance.Objects.Add(prefab);

            if (prefab.gameObject.GetComponent<EncounterTrackable>() == null) {
                prefab.gameObject.AddComponent<EncounterTrackable>();
            }

            prefab.encounterTrackable.journalData = new JournalEntry();
            prefab.encounterTrackable.prerequisites = new DungeonPrerequisite[0];
            prefab.encounterTrackable.journalData.SuppressKnownState = true;

            _Logger.Debug($"Registering passive item {full_id}");
            
            prefab.AmmonomiconEntry = new AmmonomiconEntry {
                DisplayName = "#ETGMOD_UNKNOWNLUAITEM_NAME",
                FullDescription = "#ETGMOD_UNKNOWNLUAITEM_DESC",
                ShortDescription = "#ETGMOD_UNKNOWNLUAITEM_SHORTDESC"
            };

            var ent = new EncounterDatabaseEntry(prefab.encounterTrackable) {
                myGuid = $"ETGMod Lua Item {full_id}",
                unityGuid = $"ETGMod Lua Item {full_id}",
                ProxyEncounterGuid = $"ETGMod Lua Item {full_id}",
                journalData = prefab.encounterTrackable.journalData,

                // May this note be left here forever as a tombstone of the 3 hours of debugging
                // that I have wasted, including copying compiler generated coroutine code
                // and manually modifying it to work as a normal method so that I could debug it,
                // just so that I could find this condition - this one, horrifying condition:
                //   if (this.journalEntries [accumulatedSpriteIndex].path.Contains ("ResourcefulRatNote"))
                // which hid itself from me by being seemingly not relevant (I didn't do anything related
                // to Resourceful Rat notes). Of course only my debugging made me realize the
                // `.path.Contains` - a method call on the object without a prior check for whether
                // it is null or not.
                // Don't act dumb.
                path = "Fuck you."
            };

            EncounterDatabase.Instance.Entries.Add(ent);

            prefab.encounterTrackable.EncounterGuid = $"ETGMod Lua Item {full_id}";

            prefab.encounterTrackable.journalData.AmmonomiconSprite = "jhguftrtdyf6ttghg";
            
            Passives[full_id] = prefab;
            ETGMod.Items.Add(full_id, prefab);

            return prefab;
        }
    }
}

using System;
using Eluant;
using UnityEnginePatches;
using ETGMod.API;

namespace ETGMod.Lua {
    public class LuaPassiveItem : PassiveItem, IDisposable {
        static LuaPassiveItem() {
            ObjectInstantiateHijacker.Add<LuaPassiveItem>((source, instance) => {
                instance.OnPickup = source.OnPickup;
                instance.OnDrop = source.OnDrop;
                instance.Name = source.Name;
            });

            I18N.AddItemString(I18N.ENGLISH, "ETGMOD_UNKNOWNLUAITEM_NAME", "Lua Item");
            I18N.AddItemString(I18N.ENGLISH, "ETGMOD_UNKNOWNLUAITEM_SHORTDESC", "A modded item");
            I18N.AddItemString(I18N.ENGLISH, "ETGMOD_UNKNOWNLUAITEM_DESC", "This item does not appear to have had its Ammonomicon entry changed.");
        }

        public string Name;

        private LuaFunction _OnPickup;
        public LuaFunction OnPickup { 
            get { return _OnPickup; }
            set { _OnPickup = value; value?.TakeOwnership(); }
        }
        private LuaFunction _OnDrop;
        public LuaFunction OnDrop {
            get { return _OnDrop; }
            set { _OnDrop = value; value?.TakeOwnership(); }
        }

        private AmmonomiconEntry? _AmmonomiconEntry = null;
        public AmmonomiconEntry AmmonomiconEntry {
            get {
                if (_AmmonomiconEntry != null) return _AmmonomiconEntry.Value;
                if (encounterTrackable != null && encounterTrackable.journalData != null) {
                    return (_AmmonomiconEntry = AmmonomiconEntry.FromJournalData(encounterTrackable.journalData)).Value;
                }
                return new AmmonomiconEntry {
                    DisplayName = "#ETGMOD_UNKNOWNLUAITEM_NAME",
                    FullDescription = "#ETGMOD_UNKNOWNLUAITEM_DESC",
                    ShortDescription = "#ETGMOD_UNKNOWNLUAITEM_SHORTDESC"
                };
            }
            set {
                _AmmonomiconEntry = value;
                _AmmonomiconEntry.Value.SetJournalData(encounterTrackable.journalData);
            }
        }

        public void Dispose() {
            OnPickup?.Dispose();
            OnDrop?.Dispose();
        }

        public void Test() {
            Console.WriteLine($"OnPickup.Runtime = {OnPickup.Runtime}");
            Console.WriteLine($"OnDrop = {OnDrop}");
        }

        public override void Pickup(PlayerController player) {
            base.Pickup(player);

            OnPickup?.Call(this, player);
        }

        public override DebrisObject Drop(PlayerController player) {
            OnDrop?.Call(this, player);

            return base.Drop(player);
        }
    }
}

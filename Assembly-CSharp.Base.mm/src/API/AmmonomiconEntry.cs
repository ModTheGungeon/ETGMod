using System;
namespace ETGMod {
    public struct AmmonomiconEntry {
        public string DisplayName;
        public string ShortDescription;
        public string FullDescription;

        public static AmmonomiconEntry FromJournalData(JournalEntry ent) {
            return new AmmonomiconEntry {
                DisplayName = ent.PrimaryDisplayName,
                ShortDescription = ent.NotificationPanelDescription,
                FullDescription = ent.AmmonomiconFullEntry
            };
        }

        public void SetJournalData(JournalEntry ent) {
            ent.NotificationPanelDescription = ShortDescription;
            ent.PrimaryDisplayName = DisplayName;
            ent.AmmonomiconFullEntry = FullDescription;
        }
    }
}

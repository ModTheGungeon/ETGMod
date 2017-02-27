using System;
using UnityEngine;
using System.Collections.Generic;

public sealed class ItemDB {

    internal ItemDB() { }

    public PickupObject this[int id] {
        get {
            return PickupObjectDatabase.Instance.InternalGetById(id);
        }
        set {
            PickupObject old = PickupObjectDatabase.Instance.Objects[id];
            if (old != null) {
                old.PickupObjectId = -1;
            }
            if (value != null) {
                value.PickupObjectId = id;
                value.gameObject.SetActive(true);
            }
            PickupObjectDatabase.Instance.Objects[id] = value;
        }
    }

    // Example name: "gshbd"
    // NOT "Test Gun"
    public PickupObject this[string name] {
        get {
            return PickupObjectDatabase.Instance.InternalGetByName(name);
        }
    }

    public int Count {
        get {
            return PickupObjectDatabase.Instance.Objects.Count;
        }
    }

    public List<PickupObject> ModItems = new List<PickupObject>();
    public Dictionary<string, PickupObject> ModItemMap = new Dictionary<string, PickupObject>();
    public Dictionary<string, List<WeightedGameObject>> ModLootPerFloor = new Dictionary<string, List<WeightedGameObject>>();

    /// <summary>
    /// Sprite collection used by guns.
    /// </summary>
    public tk2dSpriteCollectionData WeaponCollection;
    /// <summary>
    /// Sprite collection used by some other guns.
    /// </summary>
    public tk2dSpriteCollectionData WeaponCollection02;
    /// <summary>
    /// Sprite collection used by projectiles.
    /// </summary>
    public tk2dSpriteCollectionData ProjectileCollection;
    /// <summary>
    /// Sprite collection used by items.
    /// </summary>
    public tk2dSpriteCollectionData ItemCollection;

    public int Add(Gun value, tk2dSpriteCollectionData collection = null, string floor = "ANY") {
        collection = collection ?? WeaponCollection;

        if (value.gameObject.GetComponent<GunBehaviour>() == null) {
            value.gameObject.AddComponent<GunBehaviour>();
        }

        int id = Add(value, false, floor);
        return id;
    }
    public int Add(PickupObject value, bool updateSpriteCollections = false, string floor = "ANY") {
        int id = PickupObjectDatabase.Instance.Objects.Count;
        PickupObjectDatabase.Instance.Objects.Add(value);
        ModItems.Add(value);
        if (value != null) {
            UnityEngine.Object.DontDestroyOnLoad(value.gameObject);
            ModItemMap[value.name] = value;
            value.PickupObjectId = id;

            EncounterDatabaseEntry edbEntry = new EncounterDatabaseEntry(value.encounterTrackable);
            edbEntry.ProxyEncounterGuid =
            edbEntry.myGuid = value.encounterTrackable.EncounterGuid;
            edbEntry.path = "Assets/Resources/ITEMDB:" + value.name + ".prefab";
            EncounterDatabase.Instance.Entries.Add(edbEntry);

            WeightedGameObject lootGameObject = new WeightedGameObject() {
                gameObject = value.gameObject,
                weight = 1f,
                additionalPrerequisites = new DungeonPrerequisite[0]
            };
            if (value is Gun) {
                GameManager.Instance.RewardManager.GunsLootTable.defaultItemDrops.Add(lootGameObject);
            } else {
                GameManager.Instance.RewardManager.ItemsLootTable.defaultItemDrops.Add(lootGameObject);
            }
            List<WeightedGameObject> loot;
            if (!ModLootPerFloor.TryGetValue(floor, out loot)) {
                loot = new List<WeightedGameObject>();
            }
            loot.Add(lootGameObject);
            ModLootPerFloor[floor] = loot;
        }
        if (updateSpriteCollections) {
            AmmonomiconController.ForceInstance.EncounterIconCollection.Handle();
            if (value is Gun) {
                WeaponCollection.Handle();
                WeaponCollection02.Handle();
                ProjectileCollection.Handle();
            } else {
                ItemCollection.Handle();
            }
        }
        return id;
    }

    public void DungeonStart() {
        List<WeightedGameObject> loot;

        if (ModLootPerFloor.TryGetValue("ANY", out loot)) {
            GameManager.Instance.Dungeon.baseChestContents.defaultItemDrops.elements.AddRange(loot);
        }

        string floorNameKey = GameManager.Instance.Dungeon.DungeonFloorName;
        string floorName = floorNameKey.Substring(1, floorNameKey.IndexOf('_') - 1);
        if (ModLootPerFloor.TryGetValue(floorName, out loot)) {
            GameManager.Instance.Dungeon.baseChestContents.defaultItemDrops.elements.AddRange(loot);
        }
    }

    private Gun _GunGivenPrototype;
    public Gun NewGun(string gunName, string gunNameShort = null) {
        if (_GunGivenPrototype == null) {
            _GunGivenPrototype = (Gun) ETGMod.Databases.Items["Pea_Shooter"];
            ProjectileCollection = _GunGivenPrototype.DefaultModule.projectiles[0].GetComponentInChildren<tk2dBaseSprite>().Collection;
        }

        return NewGun(gunName, _GunGivenPrototype, gunNameShort);
    }
    public Gun NewGun(string gunName, Gun baseGun, string gunNameShort = null) {
        if (gunNameShort == null) {
            gunNameShort = gunName.Replace(' ', '_');
        }

        GameObject go = UnityEngine.Object.Instantiate(baseGun.gameObject);
        go.name = gunNameShort;

        Gun gun = go.GetComponent<Gun>();
        SetupItem(gun, gunName);
        gun.gunName = gunName;
        gun.gunSwitchGroup = gunNameShort;

        gun.modifiedVolley = null;
        gun.singleModule = null;

        gun.RawSourceVolley = ScriptableObject.CreateInstance<ProjectileVolleyData>();
        gun.Volley.projectiles = new List<ProjectileModule>();

        gun.SetBaseMaxAmmo(300);
        gun.reloadTime = 0.625f;

        //FIXME Terrible workaround. Disgusting!
        Gungeon.Game.Items.Add($"outdated_gun_mods:{gunName.ToLowerInvariant().Replace(' ', '_')}", gun);

        return gun;
    }
    //FIXME NewGun<> causing issues (MonoMod)
    /*
    public Gun NewGun<T>(string gunName, string gunNameShort = null) where T : GunBehaviour {
        Gun gun = NewGun(gunName, gunNameShort);
        gun.gameObject.AddComponent<T>();
        return gun;
    }
    public Gun NewGun<T>(string gunName, Gun baseGun, string gunNameShort = null) where T : GunBehaviour {
        Gun gun = NewGun(gunName, baseGun, gunNameShort);
        gun.gameObject.AddComponent<T>();
        return gun;
    }
    */

    public void SetupItem(PickupObject item, string name) {
        if (item.encounterTrackable == null) item.encounterTrackable = item.gameObject.AddComponent<EncounterTrackable>();
        if (item.encounterTrackable.journalData == null) item.encounterTrackable.journalData = new JournalEntry();

        item.encounterTrackable.EncounterGuid = item.name;

        item.encounterTrackable.prerequisites = new DungeonPrerequisite[0];
        item.encounterTrackable.journalData.SuppressKnownState = true;

        string keyName = "#" + item.name.Replace(" ", "").ToUpperInvariant();
        item.encounterTrackable.journalData.PrimaryDisplayName = keyName + "_ENCNAME";
        item.encounterTrackable.journalData.NotificationPanelDescription = keyName + "_SHORTDESC";
        item.encounterTrackable.journalData.AmmonomiconFullEntry = keyName + "_LONGDESC";
        item.encounterTrackable.journalData.AmmonomiconSprite = item.name.Replace(' ', '_') + "_idle_001";
        item.SetName(name);
    }

    public PickupObject GetModItemByName(string name) {
        PickupObject item;
        if (ModItemMap.TryGetValue(name, out item)) {
            return item;
        }
        return null;
    }

}
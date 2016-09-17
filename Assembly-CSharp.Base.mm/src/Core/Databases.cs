using System;
using UnityEngine;
using System.Collections.Generic;

public static partial class ETGMod {

    /// <summary>
    /// ETGMod database configuration.
    /// </summary>
    public static class Databases {

        public readonly static ItemDB Items = new ItemDB();

    }

}

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
                weight = 1f
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

    public void SetText(string key, string value) {
        StringTableManager.StringCollection value_ = new StringTableManager.SimpleStringCollection();
        value_.AddString(value, 0f);
        StringTableManager.ItemTable[key] = value_;
        JournalEntry.ReloadDataSemaphore++;
    }

}
public static class ItemDBExt {
    public static void SetName(this PickupObject item, string text) {
        ETGMod.Databases.Items.SetText(item.encounterTrackable.journalData.PrimaryDisplayName, text);
    }
    public static void SetShortDescription(this PickupObject item, string text) {
        ETGMod.Databases.Items.SetText(item.encounterTrackable.journalData.NotificationPanelDescription, text);
    }
    public static void SetLongDescription(this PickupObject item, string text) {
        ETGMod.Databases.Items.SetText(item.encounterTrackable.journalData.AmmonomiconFullEntry, text);
    }

    public static void UpdateAnimations(this Gun gun, tk2dSpriteCollectionData collection = null) {
        collection = collection ?? ETGMod.Databases.Items.WeaponCollection;

        gun.idleAnimation               = gun.UpdateAnimation("idle",               collection      );
        gun.introAnimation              = gun.UpdateAnimation("intro",              collection, true);
        gun.emptyAnimation              = gun.UpdateAnimation("empty",              collection      );
        gun.shootAnimation              = gun.UpdateAnimation("fire",               collection, true);
        gun.reloadAnimation             = gun.UpdateAnimation("reload",             collection, true);
        gun.chargeAnimation             = gun.UpdateAnimation("charge",             collection      );
        gun.outOfAmmoAnimation          = gun.UpdateAnimation("out_of_ammo",        collection      );
        gun.dischargeAnimation          = gun.UpdateAnimation("discharge",          collection      );
        gun.finalShootAnimation         = gun.UpdateAnimation("final_fire",         collection, true);
        gun.emptyReloadAnimation        = gun.UpdateAnimation("empty_reload",       collection, true);
        gun.criticalFireAnimation       = gun.UpdateAnimation("critical_fire",      collection, true);
        gun.enemyPreFireAnimation       = gun.UpdateAnimation("enemy_pre_fire",     collection      );
        gun.alternateShootAnimation     = gun.UpdateAnimation("alternate_shoot",    collection, true);
        gun.alternateReloadAnimation    = gun.UpdateAnimation("alternate_reload",   collection, true);
    }
    public static string UpdateAnimation(this Gun gun, string name, tk2dSpriteCollectionData collection = null, bool returnToIdle = false) {
        collection = collection ?? ETGMod.Databases.Items.WeaponCollection;

        string clipName = gun.name + "_" + name;
        string prefix = clipName + "_";
        int prefixLength = prefix.Length;

        List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
        for (int i = 0; i < collection.spriteDefinitions.Length; i++) {
            tk2dSpriteDefinition sprite = collection.spriteDefinitions[i];
            if (sprite.Valid && sprite.name.StartsWithInvariant(prefix)) {
                frames.Add(new tk2dSpriteAnimationFrame() {
                    spriteCollection = collection,
                    spriteId = i
                });
            }
        }

        if (frames.Count == 0) {
            return null;
        }

        tk2dSpriteAnimationClip clip = gun.spriteAnimator.Library.GetClipByName(clipName);
        if (clip == null) {
            clip = new tk2dSpriteAnimationClip();
            clip.name = clipName;
            clip.fps = 15;
            if (returnToIdle) {
                clip.wrapMode = tk2dSpriteAnimationClip.WrapMode.Once;
            }
            Array.Resize(ref gun.spriteAnimator.Library.clips, gun.spriteAnimator.Library.clips.Length + 1);
            gun.spriteAnimator.Library.clips[gun.spriteAnimator.Library.clips.Length - 1] = clip;
        }

        frames.Sort((x, y) =>
            int.Parse(collection.spriteDefinitions[x.spriteId].name.Substring(prefixLength)) -
            int.Parse(collection.spriteDefinitions[y.spriteId].name.Substring(prefixLength))
           );
        clip.frames = frames.ToArray();

        return clipName;
    }

    public static void SetAnimationFPS(this Gun gun, int fps) {
        gun.SetAnimationFPS(gun.idleAnimation,              fps);
        gun.SetAnimationFPS(gun.introAnimation,             fps);
        gun.SetAnimationFPS(gun.emptyAnimation,             fps);
        gun.SetAnimationFPS(gun.shootAnimation,             fps);
        gun.SetAnimationFPS(gun.reloadAnimation,            fps);
        gun.SetAnimationFPS(gun.chargeAnimation,            fps);
        gun.SetAnimationFPS(gun.outOfAmmoAnimation,         fps);
        gun.SetAnimationFPS(gun.dischargeAnimation,         fps);
        gun.SetAnimationFPS(gun.finalShootAnimation,        fps);
        gun.SetAnimationFPS(gun.emptyReloadAnimation,       fps);
        gun.SetAnimationFPS(gun.criticalFireAnimation,      fps);
        gun.SetAnimationFPS(gun.enemyPreFireAnimation,      fps);
        gun.SetAnimationFPS(gun.alternateShootAnimation,    fps);
        gun.SetAnimationFPS(gun.alternateReloadAnimation,   fps);
    }
    public static void SetAnimationFPS(this Gun gun, string name, int fps) {
        if (string.IsNullOrEmpty(name)) {
            return;
        }

        tk2dSpriteAnimationClip clip = gun.spriteAnimator.Library.GetClipByName(name);
        if (clip == null) {
            return;
        }
        clip.fps = fps;
    }

    public static Projectile ClonedPrefab(this Projectile orig) {
        if (orig == null) return null;

        orig.gameObject.SetActive(false);
        Projectile clone = UnityEngine.Object.Instantiate(orig.gameObject).GetComponent<Projectile>();
        orig.gameObject.SetActive(true);

        clone.name = orig.name;
        UnityEngine.Object.DontDestroyOnLoad(clone.gameObject);

        return clone;
    }
    public static Projectile EnabledClonedPrefab(this Projectile projectile) {
        if (projectile == null) return null;

        if (!projectile.gameObject.activeSelf) {
            string name = projectile.name;
            // TODO The clone of the clone fixes the projectile "breaking", but may cause performance issues.
            projectile = (Projectile) UnityEngine.Object.Instantiate(projectile, projectile.transform.parent);
            projectile.name = name;
            projectile.gameObject.SetActive(true);
        }

        return projectile;
    }

    public static Projectile AddProjectileFrom(this Gun gun, string other, bool cloned = true) {
        return gun.AddProjectileFrom((Gun) ETGMod.Databases.Items[other], cloned);
    }
    public static Projectile AddProjectileFrom(this Gun gun, Gun other, bool cloned = true) {
        if (other.DefaultModule.projectiles.Count == 0) return null;
        Projectile p = other.DefaultModule.projectiles[0];
        if (p == null) return null;
        return gun.AddProjectile(!cloned ? p : p.ClonedPrefab());
    }
    public static Projectile AddProjectile(this Gun gun, Projectile projectile) {
        gun.DefaultModule.projectiles.Add(projectile);
        return projectile;
    }

    public static ProjectileModule AddProjectileModuleFrom(this Gun gun, string other, bool cloned = true, bool clonedProjectiles = true) {
        return gun.AddProjectileModuleFrom((Gun) ETGMod.Databases.Items[other], cloned, clonedProjectiles);
    }
    public static ProjectileModule AddProjectileModuleFrom(this Gun gun, Gun other, bool cloned = true, bool clonedProjectiles = true) {
        ProjectileModule module = other.DefaultModule;
        if (!cloned) return gun.AddProjectileModule(module);

        ProjectileModule clone = ProjectileModule.CreateClone(module, false);
        clone.projectiles = new List<Projectile>(module.projectiles.Capacity);
        for (int i = 0; i < module.projectiles.Count; i++) {
            clone.projectiles.Add(!clonedProjectiles ? module.projectiles[i] : module.projectiles[i].ClonedPrefab());
        }
        return gun.AddProjectileModule(clone);
    }
    public static ProjectileModule AddProjectileModule(this Gun gun, ProjectileModule projectile) {
        gun.Volley.projectiles.Add(projectile);
        return projectile;
    }

    public static void SetupSprite(this Gun gun, tk2dSpriteCollectionData collection = null, string defaultSprite = null, int fps = 0) {
        AmmonomiconController.ForceInstance.EncounterIconCollection.Handle();
        collection = collection ?? ETGMod.Databases.Items.WeaponCollection;
        collection.Handle();

        if (defaultSprite != null) {
            gun.encounterTrackable.journalData.AmmonomiconSprite = defaultSprite;
        }

        gun.UpdateAnimations(collection);
        gun.GetSprite().SetSprite(collection, gun.DefaultSpriteID = collection.GetSpriteIdByName(gun.encounterTrackable.journalData.AmmonomiconSprite));

        if (fps != 0) {
            gun.SetAnimationFPS(fps);
        }
    }

}

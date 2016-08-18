using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Mono.Cecil;

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

    public int Count {
        get {
            return PickupObjectDatabase.Instance.Objects.Count;
        }
    }

    public List<PickupObject> ModItems = new List<PickupObject>();

    /// <summary>
    /// Sprite collection used by the guns.
    /// </summary>
    public tk2dSpriteCollectionData WeaponCollection;
    /// <summary>
    /// Sprite collection used for the gun collected popup.
    /// </summary>
    public tk2dSpriteCollectionData WeaponCollection02;
    /// <summary>
    /// Sprite collection used by the items.
    /// </summary>
    public tk2dSpriteCollectionData ItemCollection;

    public int Add(Gun value, bool updateSpriteCollections = false, bool updateAnimations = false) {
        int id = Add((PickupObject) value, updateSpriteCollections);
        if (updateAnimations) {
            value.UpdateAnimations();
            value.GetSprite().SetSprite(WeaponCollection, WeaponCollection.GetSpriteIdByName(value.encounterTrackable.journalData.AmmonomiconSprite));
            value.DefaultSpriteID = value.GetSprite().spriteId;
        }
        return id;
    }
    public int Add(PickupObject value, bool updateSpriteCollections = true) {
        int id = PickupObjectDatabase.Instance.Objects.Count;
        PickupObjectDatabase.Instance.Objects.Add(value);
        ModItems.Add(value);
        if (value != null) {
            UnityEngine.Object.DontDestroyOnLoad(value.gameObject);
            value.PickupObjectId = id;
            EncounterDatabaseEntry edbEntry = new EncounterDatabaseEntry(value.encounterTrackable);
            edbEntry.ProxyEncounterGuid =
            edbEntry.myGuid = value.encounterTrackable.EncounterGuid;
            edbEntry.path = "Assets/Resources/ITEMDB:" + value.name + ".prefab";
            EncounterDatabase.Instance.Entries.Add(edbEntry);
        }
        if (updateSpriteCollections) {
            AmmonomiconController.ForceInstance.EncounterIconCollection.Handle();
            if (value is Gun) {
                WeaponCollection.Handle();
                WeaponCollection02.Handle();
            } else {
                ItemCollection.Handle();
            }
            ETGMod.Assets.Packer.Apply();
        }
        return id;
    }

    public void Remap() {
        for (int i = 0; i < ModItems.Count; i++) {
            PickupObject item = ModItems[i];
            if (item == null) {
                continue;
            }
            PickupObjectDatabase.Instance.Objects[item.PickupObjectId] = item;
        }
    }

    private Gun _GunGivenPrototype;
    public Gun NewGun(string gunName, string gunNameShort = null) {
        if (_GunGivenPrototype == null) {
            _GunGivenPrototype = (Gun) PickupObjectDatabase.GetByName("Pea_Shooter");
            WeaponCollection02 = _GunGivenPrototype.sprite.Collection;
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

    public void SetupItem(PickupObject item, string name) {
        item.encounterTrackable.EncounterGuid = 
        item.encounterTrackable.TrueEncounterGuid = item.name;
        // DON'T SET PROXY.

        string keyName = "#" + item.name.Replace(" ", "").ToUpperInvariant();
        item.encounterTrackable.journalData.PrimaryDisplayName = keyName + "_ENCNAME";
        item.encounterTrackable.journalData.NotificationPanelDescription = keyName + "_SHORTDESC";
        item.encounterTrackable.journalData.AmmonomiconFullEntry = keyName + "_LONGDESC";
        item.encounterTrackable.journalData.AmmonomiconSprite = item.name.Replace(' ', '_') + "_idle_001";
        item.SetName(name);
    }

    public PickupObject GetModItemByName(string name) {
        for (int i = 0; i < ModItems.Count; i++) {
            PickupObject item = ModItems[i];
            if (item != null && item.name == name) {
                return item;
            }
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

    public static void UpdateAnimations(this Gun gun) {
        gun.idleAnimation               = gun.UpdateAnimation("idle");
        gun.introAnimation              = gun.UpdateAnimation("intro", true);
        gun.emptyAnimation              = gun.UpdateAnimation("empty");
        gun.shootAnimation              = gun.UpdateAnimation("fire", true);
        gun.reloadAnimation             = gun.UpdateAnimation("reload", true);
        gun.chargeAnimation             = gun.UpdateAnimation("charge");
        gun.outOfAmmoAnimation          = gun.UpdateAnimation("out_of_ammo");
        gun.dischargeAnimation          = gun.UpdateAnimation("discharge");
        gun.finalShootAnimation         = gun.UpdateAnimation("final_fire", true);
        gun.emptyReloadAnimation        = gun.UpdateAnimation("empty_reload", true);
        gun.criticalFireAnimation       = gun.UpdateAnimation("critical_fire", true);
        gun.enemyPreFireAnimation       = gun.UpdateAnimation("enemy_pre_fire");
        gun.alternateShootAnimation     = gun.UpdateAnimation("alternate_shoot", true);
        gun.alternateReloadAnimation    = gun.UpdateAnimation("alternate_reload", true);
    }
    public static string UpdateAnimation(this Gun gun, string name, bool returnToIdle = false) {
        string clipName = gun.name + "_" + name;
        string prefix = clipName + "_";
        int prefixLength = prefix.Length;

        List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
        tk2dSpriteCollectionData sprites = ETGMod.Databases.Items.WeaponCollection;
        for (int i = 0; i < sprites.spriteDefinitions.Length; i++) {
            tk2dSpriteDefinition sprite = sprites.spriteDefinitions[i];
            if (sprite.Valid && sprite.name.StartsWithInvariant(prefix)) {
                frames.Add(new tk2dSpriteAnimationFrame() {
                    spriteCollection = sprites,
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
            int.Parse(sprites.spriteDefinitions[x.spriteId].name.Substring(prefixLength)) -
            int.Parse(sprites.spriteDefinitions[y.spriteId].name.Substring(prefixLength))
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

    public static Projectile AddProjectileFrom(this Gun gun, string other) {
        return gun.AddProjectileFrom(UnityEngine.Object.Instantiate(PickupObjectDatabase.GetByName(other).gameObject).GetComponent<Gun>());
    }
    public static Projectile AddProjectileFrom(this Gun gun, Gun other) {
        if (other.DefaultModule.projectiles.Count == 0) {
            return null;
        }
        return gun.AddProjectile(other.DefaultModule.projectiles[0]);
    }
    public static Projectile AddProjectile(this Gun gun, Projectile projectile) {
        gun.DefaultModule.projectiles.Add(projectile);
        return projectile;
    }

    public static ProjectileModule AddProjectileModuleFrom(this Gun gun, string other) {
        return gun.AddProjectileModuleFrom(UnityEngine.Object.Instantiate(PickupObjectDatabase.GetByName(other).gameObject).GetComponent<Gun>());
    }
    public static ProjectileModule AddProjectileModuleFrom(this Gun gun, Gun other) {
        return gun.AddProjectileModule(other.DefaultModule);
    }
    public static ProjectileModule AddProjectileModule(this Gun gun, ProjectileModule projectile) {
        gun.Volley.projectiles.Add(projectile);
        return projectile;
    }

    public static void SetupSprite(this Gun gun, string defaultSprite = null) {
        AmmonomiconController.ForceInstance.EncounterIconCollection.Handle();
        ETGMod.Databases.Items.WeaponCollection.Handle();
        ETGMod.Databases.Items.WeaponCollection02.Handle();

        if (defaultSprite != null) {
            gun.encounterTrackable.journalData.AmmonomiconSprite = defaultSprite;
        }

        gun.UpdateAnimations();
        gun.GetSprite().SetSprite(ETGMod.Databases.Items.WeaponCollection, ETGMod.Databases.Items.WeaponCollection.GetSpriteIdByName(gun.encounterTrackable.journalData.AmmonomiconSprite));
        gun.DefaultSpriteID = gun.GetSprite().spriteId;
    }

}

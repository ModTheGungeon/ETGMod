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

    public int Add(Gun value, bool updateSpriteCollections = true, bool updateAnimations = true) {
        int id = Add((PickupObject) value, updateSpriteCollections);
        if (updateAnimations) {
            value.UpdateAnimations();
            value.GetSprite().SetSprite(WeaponCollection, WeaponCollection.GetSpriteIdByName(value.encounterTrackable.journalData.AmmonomiconSprite));
            value.DefaultSpriteID = value.GetSprite().spriteId;
            value.GetSprite().ForceUpdateMaterial();
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

        gun.Volley = null;
        gun.modifiedVolley = null;
        gun.singleModule.runtimeGuid = go.name;

        gun.SetBaseMaxAmmo(300);
        gun.singleModule.numberOfShotsInClip = 10;
        gun.reloadTime = 0.7f;
        gun.singleModule.shootStyle = ProjectileModule.ShootStyle.SemiAutomatic;

        gun.singleModule.customAmmoType = null;
        gun.singleModule.ammoType = GameUIAmmoType.AmmoType.MEDIUM_BULLET;

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
        gun.introAnimation              = gun.UpdateAnimation("intro");
        gun.emptyAnimation              = gun.UpdateAnimation("empty");
        gun.shootAnimation              = gun.UpdateAnimation("fire", true);
        gun.reloadAnimation             = gun.UpdateAnimation("reload");
        gun.chargeAnimation             = gun.UpdateAnimation("charge");
        gun.outOfAmmoAnimation          = gun.UpdateAnimation("out_of_ammo");
        gun.dischargeAnimation          = gun.UpdateAnimation("discharge");
        gun.finalShootAnimation         = gun.UpdateAnimation("final_fire");
        gun.emptyReloadAnimation        = gun.UpdateAnimation("empty_reload");
        gun.criticalFireAnimation       = gun.UpdateAnimation("critical_fire");
        gun.enemyPreFireAnimation       = gun.UpdateAnimation("enemy_pre_fire");
        gun.alternateShootAnimation     = gun.UpdateAnimation("alternate_shoot");
        gun.alternateReloadAnimation    = gun.UpdateAnimation("alternate_reload");
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

        if (returnToIdle) {
            if (!string.IsNullOrEmpty(gun.idleAnimation)) {
                tk2dSpriteAnimationClip idle = gun.spriteAnimator.Library.GetClipByName(gun.idleAnimation);
                frames.Add(idle.frames[idle.frames.Length - 1]);
            }
        }

        tk2dSpriteAnimationClip clip = gun.spriteAnimator.Library.GetClipByName(clipName);
        if (clip == null) {
            clip = new tk2dSpriteAnimationClip();
            clip.name = clipName;
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

    public static void SetProjectileFrom(this Gun gun, string other) {
        gun.SetProjectileFrom((Gun) PickupObjectDatabase.GetByName(other));
    }
    public static void SetProjectileFrom(this Gun gun, Gun other) {
        gun.SetProjectile(other.DefaultModule.projectiles[0]);
    }
    public static void SetProjectile(this Gun gun, Projectile projectile) {
        gun.DefaultModule.projectiles.Clear();
        gun.DefaultModule.projectiles.Add(projectile);
    }

}

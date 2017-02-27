using System;
using System.Collections.Generic;

public static class GunExt {
    public static void SetName(this PickupObject item, string text) {
        ETGMod.Databases.Strings.Items.Set(item.encounterTrackable.journalData.PrimaryDisplayName, text);
    }
    public static void SetShortDescription(this PickupObject item, string text) {
        ETGMod.Databases.Strings.Items.Set(item.encounterTrackable.journalData.NotificationPanelDescription, text);
    }
    public static void SetLongDescription(this PickupObject item, string text) {
        ETGMod.Databases.Strings.Items.Set(item.encounterTrackable.journalData.AmmonomiconFullEntry, text);
    }

    public static void UpdateAnimations(this Gun gun, tk2dSpriteCollectionData collection = null) {
        collection = collection ?? ETGMod.Databases.Items.WeaponCollection;

        gun.idleAnimation = gun.UpdateAnimation("idle", collection);
        gun.introAnimation = gun.UpdateAnimation("intro", collection, true);
        gun.emptyAnimation = gun.UpdateAnimation("empty", collection);
        gun.shootAnimation = gun.UpdateAnimation("fire", collection, true);
        gun.reloadAnimation = gun.UpdateAnimation("reload", collection, true);
        gun.chargeAnimation = gun.UpdateAnimation("charge", collection);
        gun.outOfAmmoAnimation = gun.UpdateAnimation("out_of_ammo", collection);
        gun.dischargeAnimation = gun.UpdateAnimation("discharge", collection);
        gun.finalShootAnimation = gun.UpdateAnimation("final_fire", collection, true);
        gun.emptyReloadAnimation = gun.UpdateAnimation("empty_reload", collection, true);
        gun.criticalFireAnimation = gun.UpdateAnimation("critical_fire", collection, true);
        gun.enemyPreFireAnimation = gun.UpdateAnimation("enemy_pre_fire", collection);
        gun.alternateShootAnimation = gun.UpdateAnimation("alternate_shoot", collection, true);
        gun.alternateReloadAnimation = gun.UpdateAnimation("alternate_reload", collection, true);
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
        gun.SetAnimationFPS(gun.idleAnimation, fps);
        gun.SetAnimationFPS(gun.introAnimation, fps);
        gun.SetAnimationFPS(gun.emptyAnimation, fps);
        gun.SetAnimationFPS(gun.shootAnimation, fps);
        gun.SetAnimationFPS(gun.reloadAnimation, fps);
        gun.SetAnimationFPS(gun.chargeAnimation, fps);
        gun.SetAnimationFPS(gun.outOfAmmoAnimation, fps);
        gun.SetAnimationFPS(gun.dischargeAnimation, fps);
        gun.SetAnimationFPS(gun.finalShootAnimation, fps);
        gun.SetAnimationFPS(gun.emptyReloadAnimation, fps);
        gun.SetAnimationFPS(gun.criticalFireAnimation, fps);
        gun.SetAnimationFPS(gun.enemyPreFireAnimation, fps);
        gun.SetAnimationFPS(gun.alternateShootAnimation, fps);
        gun.SetAnimationFPS(gun.alternateReloadAnimation, fps);
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
            projectile = (Projectile)UnityEngine.Object.Instantiate(projectile, projectile.transform.parent);
            projectile.name = name;
            projectile.gameObject.SetActive(true);
        }

        return projectile;
    }

    public static Projectile AddProjectileFrom(this Gun gun, string other, bool cloned = true) {
        return gun.AddProjectileFrom((Gun)ETGMod.Databases.Items[other], cloned);
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
        return gun.AddProjectileModuleFrom((Gun)ETGMod.Databases.Items[other], cloned, clonedProjectiles);
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
        ETGMod.Databases.Items.ProjectileCollection.Handle();
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

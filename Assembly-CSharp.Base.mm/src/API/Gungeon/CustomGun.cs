using System;
using System.Collections.Generic;

namespace Gungeon {
    public class CustomGun : Gun {
        // GunExt mappings
        // Kind of annoying to copy all of this over
        // Maybe we can find a better way at some point?
        
        public string ShortDescription {
            get {
                //TODO update when I18N exists
                return ETGMod.Databases.Strings.Items.Get(encounterTrackable.journalData.NotificationPanelDescription);
            }
            set {
                this.SetShortDescription(value);
            }
        }

        public string LongDescription {
            get {
                return ETGMod.Databases.Strings.Items.Get(encounterTrackable.journalData.AmmonomiconFullEntry);
            }
            set {
                this.SetLongDescription(value);
            }
        }

        public string Name {
            get {
                return ETGMod.Databases.Strings.Items.Get(encounterTrackable.journalData.PrimaryDisplayName);
            }
            set {
                this.SetName(value);
            }
        }

        public void UpdateAnimations(tk2dSpriteCollectionData collection = null) {
            GunExt.UpdateAnimations(this, collection);
        }

        public string UpdateAnimation(string name, tk2dSpriteCollectionData collection = null, bool returnToIdle = false) {
            return GunExt.UpdateAnimation(this, name, collection, returnToIdle);
        }

        public void SetAnimationFPS(int fps) {
            GunExt.SetAnimationFPS(this, fps);
        }

        public void SetAnimationFPS(string name, int fps) {
            GunExt.SetAnimationFPS(this, name, fps);
        }

        public Projectile AddProjectileFrom(string other, bool cloned = true) {
            return GunExt.AddProjectileFrom(this, other, cloned);
        }

        public Projectile AddProjectileFrom(Gun other, bool cloned = true) {
            return GunExt.AddProjectileFrom(this, other, cloned);
        }

        public Projectile AddProjectile(Projectile projectile) {
            return GunExt.AddProjectile(this, projectile);
        }

        public ProjectileModule AddProjectileModuleFrom(string other, bool cloned = true, bool clonedProjectiles = true) {
            return GunExt.AddProjectileModuleFrom(this, other, cloned, clonedProjectiles);
        }

        public ProjectileModule AddProjectileModuleFrom(Gun other, bool cloned = true, bool clonedProjectiles = true) {
            return GunExt.AddProjectileModuleFrom(this, other, cloned, clonedProjectiles);
        }

        public ProjectileModule AddProjectileModule(ProjectileModule projectile) {
            return GunExt.AddProjectileModule(this, projectile);
        }

        public void SetupSprite(tk2dSpriteCollectionData collection = null, string defaultSprite = null, int fps = 0) {
            GunExt.SetupSprite(this, collection, defaultSprite, fps);
        }

        public CustomGun(string id) {
            base.OnInitializedWithOwner += OnInitializedWithOwner;
            base.PostProcessProjectile += PostProcessProjectile;
            base.OnDropped += OnDropped;
            base.OnAutoReload += OnAutoReload;
            base.OnReloadPressed += OnReloadPressed;
            base.OnFinishAttack += OnFinishAttack;
            base.OnPostFired += OnPostFired;
            base.OnAmmoChanged += OnAmmoChanged;
            base.OnPreFireProjectileModifier += OnPreFireProjectileModifier;

            encounterTrackable.EncounterGuid = id;
        }

        public virtual new void OnInitializedWithOwner(GameActor actor) {
        }

        public virtual new void PostProcessProjectile(Projectile projectile) {
        }

        public virtual new void OnDropped() {
        }

        public virtual new void OnAutoReload(PlayerController player, Gun gun) {
        }

        public virtual new void OnReloadPressed(PlayerController player, Gun gun, bool bSOMETHING) {
        }

        public virtual new void OnFinishAttack(PlayerController player, Gun gun) {
        }

        public virtual new void OnPostFired(PlayerController player, Gun gun) {
        }

        public virtual new void OnAmmoChanged(PlayerController player, Gun gun) {
        }

        public virtual new Projectile OnPreFireProjectileModifier(Gun gun, Projectile projectile) {
            return projectile.EnabledClonedPrefab();
        }
    }

}


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ETGMod.API {
    public class CustomPickupObject {
        internal static GameObject BasePickupGameObject;
        private static Logger _Logger = new Logger("CustomPickupObject");

        internal static void InitAPI() {
            BasePickupGameObject = FakePrefab.Clone(ETGMod.Items["gungeon:wax_wings"].gameObject);
            UnityEngine.Object.Destroy(BasePickupGameObject.GetComponent<PickupObject>());
        }

        public static T Create<T>() where T : PickupObject {
            var clone = FakePrefab.Clone(BasePickupGameObject);
            var components = clone.GetComponents<PickupObject>();
            for (int i = 0; i < components.Length; i++) UnityEngine.Object.Destroy(components[i]);
            return clone.AddComponent<T>();
        }

        public static void Test() {
            var pf = Create<WingsItem>();

            _Logger.Debug($"Checking");
            _Logger.Debug($"pf.sprite = {pf.sprite}");
            _Logger.Debug($"pf.sprite.name = {pf.sprite?.name}");
            _Logger.Debug($"pf.sprite.Collection = {pf.sprite?.Collection}");
            _Logger.Debug($"pf.sprite.Collection.spriteCollectionName = {pf.sprite?.Collection?.spriteCollectionName}");
            _Logger.Debug($"pf.spriteAnimator = {pf.spriteAnimator}");
            _Logger.Debug($"pf.spriteAnimator.Library = {pf.spriteAnimator?.Library}");
            _Logger.Debug($"pf.spriteAnimator.Library.clips.Length = {pf.spriteAnimator?.Library?.clips?.Length}");
            var inst = FakePrefab.Instantiate(pf.gameObject);

            inst.gameObject.DestroyComponents<tk2dBaseSprite>();
            var sprite = inst.gameObject.AddComponent<tk2dSprite>();
            sprite.Collection = GameManager.Instance.PrimaryPlayer.sprite.Collection;
            sprite.spriteId = 4;
            sprite.ForceUpdateMaterial();

            var debris = LootEngine.SpawnItem(inst.gameObject, GameManager.Instance.PrimaryPlayer.CenterPosition, Vector2.zero, 0f, true, false, false);
            debris.gameObject.DestroyComponents<tk2dBaseSprite>();
            debris.sprite = debris.gameObject.AddComponent<tk2dSprite>();
            debris.sprite.Collection = GameManager.Instance.PrimaryPlayer.sprite.Collection;
            debris.sprite.spriteId = 4;
            debris.sprite.ForceUpdateMaterial();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using YamlDotNet.Serialization;

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

        public static void Test(string id, Animation anim = null) {
            //var pf = Create<WingsItem>();

            //_Logger.Debug($"Checking");
            //_Logger.Debug($"pf.sprite = {pf.sprite}");
            //_Logger.Debug($"pf.sprite.name = {pf.sprite?.name}");
            //_Logger.Debug($"pf.sprite.Collection = {pf.sprite?.Collection}");
            //_Logger.Debug($"pf.sprite.Collection.spriteCollectionName = {pf.sprite?.Collection?.spriteCollectionName}");
            //_Logger.Debug($"pf.spriteAnimator = {pf.spriteAnimator}");
            //_Logger.Debug($"pf.spriteAnimator.Library = {pf.spriteAnimator?.Library}");
            //_Logger.Debug($"pf.spriteAnimator.Library.clips.Length = {pf.spriteAnimator?.Library?.clips?.Length}");
            //var inst = FakePrefab.Instantiate(pf.gameObject);

            var pf = ETGMod.Items[id];
            var inst = FakePrefab.Instantiate(pf.gameObject);

            //inst.gameObject.DestroyComponents<tk2dBaseSprite>();
            //var sprite = inst.gameObject.AddComponent<tk2dSprite>();
            //sprite.Collection = GameManager.Instance.PrimaryPlayer.sprite.Collection;
            //sprite.spriteId = 4;
            //sprite.ForceUpdateMaterial();

            var guns = UnityEngine.Object.FindObjectsOfType<Gun>();
            Gun yellow_chamber = null;

            foreach (var gun in guns) {
                Console.WriteLine(gun.gunName);
                if (gun.gunName == "Shotgrub") yellow_chamber = gun;
            }

            _Logger.Debug($"Components: ");
            foreach (var com in yellow_chamber.GetComponents<UnityEngine.Component>()) {
                _Logger.DebugIndent($"- {com.GetType()}");
            }

            Console.WriteLine($"Sprite: {yellow_chamber.sprite}");
            Console.WriteLine($"SpriteAnimator: {yellow_chamber.spriteAnimator}");
            Console.WriteLine($"Debris Sprite: {yellow_chamber.debris?.sprite}");
            Console.WriteLine($"Debris SpriteAnimator: {yellow_chamber.debris?.spriteAnimator}");
            var ycdebris = LootEngine.SpawnItem(yellow_chamber.gameObject, GameManager.Instance.PrimaryPlayer.CenterPosition, Vector2.zero, 0f, true, false, false);
            Console.WriteLine($"Post-spawn Debris Sprite: {ycdebris.sprite}");
            Console.WriteLine($"Post-spawn Debris SpriteAnimator: {ycdebris.spriteAnimator}");

            if (ycdebris.spriteAnimator != null) {
                Console.WriteLine($"{ycdebris.spriteAnimator.Library.clips.Length} clips in spriteAnimator");
                Console.WriteLine($"sprite.spriteAnimator set? {ycdebris.sprite?.spriteAnimator == ycdebris.spriteAnimator}");
                Console.WriteLine($"sprite.spriteAnimator null? {ycdebris.sprite?.spriteAnimator == null}");
            }

            Console.WriteLine($"== PASSIVEITEM ==");
            Console.WriteLine(Tools.ObjectDumper.Dump(yellow_chamber, depth: 10));
            Console.WriteLine($"== DEBRIS ==");
            Console.WriteLine(Tools.ObjectDumper.Dump(ycdebris, depth: 10));
            Console.WriteLine($"== SPRITE ==");
            Console.WriteLine(Tools.ObjectDumper.Dump(ycdebris.sprite, depth: 10));
            Console.WriteLine($"== SPRITE ANIMATOR ==");
            Console.WriteLine(Tools.ObjectDumper.Dump(ycdebris.spriteAnimator, depth: 10));

            var debris = LootEngine.SpawnItem(inst.gameObject, GameManager.Instance.PrimaryPlayer.CenterPosition, Vector2.zero, 0f, true, false, false);
            debris.gameObject.DestroyComponents<tk2dBaseSprite>();

            if (anim == null) {
                debris.sprite = debris.gameObject.AddComponent<tk2dSprite>();
                debris.sprite.Collection = GameManager.Instance.PrimaryPlayer.sprite.Collection;
                debris.sprite.spriteId = 4;
                debris.sprite.ForceUpdateMaterial();
            } else {
                _Logger.Info($"Adding animator {anim}");
                var animator = debris.gameObject.AddComponent<tk2dSpriteAnimator>();
                debris.spriteAnimator = animator;
                anim.ApplyAnimator(debris.gameObject);
                debris.sprite.Collection = animator.Library.clips[0].frames[0].spriteCollection;
                animator.Play("test");
            }
        }
    }
}
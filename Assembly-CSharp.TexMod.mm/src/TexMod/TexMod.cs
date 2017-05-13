using System;
using System.Collections.Generic;
using System.IO;
using ETGMod;
using YamlDotNet.Serialization;

namespace TexMod {
	public class TexMod : Backend {
        public static Logger Logger = new Logger("TexMod");

        private static Deserializer _Deserializer = new DeserializerBuilder().Build();

        internal static Dictionary<string, Animation> AnimationMap = new Dictionary<string, Animation>();
        internal static Dictionary<string, Animation.Collection> CollectionMap = new Dictionary<string, Animation.Collection>();

        public static string GenerateSpriteAnimationName(tk2dBaseSprite sprite) {
            if (sprite.spriteAnimator == null) return $"{sprite.name}";
            return sprite.spriteAnimator.Library.name;
        }

        public override Version Version { get { return new Version(0, 1, 0); } }

        public override void Loaded() {
            Logger.Info("TexMod initialized, adding ModLoader hooks");

            ETGMod.ETGMod.ModLoader.PostLoadMod += (ModLoader.ModInfo info) => {
                var texmod_data = info.ModMetadata.ExtraData<TexModExtra>("texmod");
                if (texmod_data == null) return;

                Logger.Info($"Mod {info.Name} uses TexMod");
                var base_dir = Path.Combine(info.Resources.Path, texmod_data.Dir);

                if (!Directory.Exists(base_dir)) {
                    Logger.Error($"TexMod directory '{texmod_data.Dir}' doesn't exist!");
                    return;
                }

                var anim_dir = Path.Combine(base_dir, texmod_data.Animations);
                var coll_dir = Path.Combine(base_dir, texmod_data.Collections);

                var anim_resources_dir = Path.Combine(texmod_data.Dir, texmod_data.Animations);
                var coll_resources_dir = Path.Combine(texmod_data.Dir, texmod_data.Collections);

                if (!Directory.Exists(anim_dir) && !Directory.Exists(coll_dir)) {
                    Logger.Warn("Both animation and collection TexMod directories don't exist");
                    return;
                }

                if (Directory.Exists(anim_dir)) {
                    var entries = Directory.GetFileSystemEntries(anim_dir);
                    for (int i = 0; i < entries.Length; i++) {
                        var full_entry = entries[i];

                        if (!full_entry.EndsWithInvariant(".yml")) continue;

                        var deser = _Deserializer.Deserialize<Animation.YAML.Animation>(File.ReadAllText(full_entry));
                        AnimationMap[deser.Name] = new Animation(info, deser, base_dir: anim_resources_dir);
                        Logger.Debug($"TexMod ANIM MAPPED '{deser.Name}'");
                    }
                }

                if (Directory.Exists(coll_dir)) {
                    var entries = Directory.GetFileSystemEntries(coll_dir);
                    for (int i = 0; i < entries.Length; i++) {
                        var full_entry = entries[i];

                        if (!full_entry.EndsWithInvariant(".yml")) continue;

                        var deser = _Deserializer.Deserialize<Animation.YAML.Collection>(File.ReadAllText(full_entry));
                        CollectionMap[deser.Name] = new Animation.Collection(info, deser, base_dir: coll_resources_dir);
                        Logger.Debug($"TexMod COLL MAPPED '{deser.Name}'");
                    }
                }
            };
        }
    }
}

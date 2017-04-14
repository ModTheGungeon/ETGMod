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
                var full_path = Path.Combine(info.RealPath, texmod_data.Dir);

                if (!Directory.Exists(full_path)) {
                    Logger.Error($"TexMod directory '{texmod_data.Dir}' doesn't exist!");
                    return;
                }

                var entries = Directory.GetFileSystemEntries(full_path);
                for (int i = 0; i < entries.Length; i++) {
                    var full_entry = entries[i];

                    if (!full_entry.EndsWithInvariant(".yml")) continue;

                    var deser = _Deserializer.Deserialize<Animation.YAML.Animation>(File.ReadAllText(full_entry));
                    AnimationMap[deser.Name] = new Animation(info, deser);
                    Logger.Debug($"TexMod MAPPED '{deser.Name}'");
                }
            };
        }
    }
}

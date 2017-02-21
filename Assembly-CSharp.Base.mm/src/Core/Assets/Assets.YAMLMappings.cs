using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using YamlDotNet;
using YamlDotNet.Serialization;

public static partial class ETGMod {
    public static partial class Assets {
        private static class YAML {
            public class Clip {
                public static readonly tk2dSpriteAnimationClip.WrapMode DEFAULT_WRAP_MODE = tk2dSpriteAnimationClip.WrapMode.Once;

                [YamlMember(Alias = "wrap")]
                private string _Wrap { set; get; } = "single";

                public tk2dSpriteAnimationClip.WrapMode WrapMode {
                    set {
                        string stringified = Regex.Replace(value.ToString(), "([A-Z])", "_$0", RegexOptions.Compiled).RemovePrefix("_");
                        _Wrap = stringified;
                    }

                    get {
                        string underscores_stripped = _Wrap.Replace("_", string.Empty);
                        return (tk2dSpriteAnimationClip.WrapMode)Enum.Parse(typeof(tk2dSpriteAnimationClip.WrapMode), underscores_stripped, true);
                    }
                }

                [YamlMember(Alias = "frames")]
                public List<String> Frames { set; get; }

                [YamlMember(Alias = "fps")]
                public int FPS { set; get; } = Animation.DEFAULT_FPS;
            }

            public class Animation {
                public static readonly string DEFAULT_NAME = "unnamed";
                public static readonly int DEFAULT_FPS = 12;

                [YamlMember(Alias = "clips")]
                public Dictionary<string, Clip> Clips { set; get; }

                [YamlMember(Alias = "name")]
                public string Name { set; get; } = DEFAULT_NAME;

                [YamlMember(Alias = "fps")]
                public int FPS { set; get; } = DEFAULT_FPS;
            }
        }
    }
}
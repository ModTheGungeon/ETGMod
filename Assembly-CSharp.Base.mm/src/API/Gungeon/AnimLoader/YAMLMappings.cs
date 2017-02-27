using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using YamlDotNet;
using YamlDotNet.Serialization;

namespace Gungeon {
    public static partial class AnimLoader {
        internal static class YAML {
            public static Deserializer Deserializer = new DeserializerBuilder().Build();

            public class FidgetDuration {
                [YamlMember(Alias = "max")]
                public float Max { set; get; }

                [YamlMember(Alias = "min")]
                public float Min { set; get; }
            }

            public class FrameSize {
                public static readonly int DEFAULT_WIDTH = 32;
                public static readonly int DEFAULT_HEIGHT = 32;

                [YamlMember(Alias = "width")]
                public int Width { set; get; } = DEFAULT_WIDTH;

                [YamlMember(Alias = "height")]
                public int Height { set; get; } = DEFAULT_HEIGHT;
            }

            public class Position {
                [YamlMember(Alias = "x")]
                public int X { set; get; }

                [YamlMember(Alias = "y")]
                public int Y { set; get; }
            }

            public class Frame {
                [YamlMember(Alias = "x")]
                public int X { set; get; }

                [YamlMember(Alias = "y")]
                public int Y { set; get; }

                [YamlMember(Alias = "position")]
                public Position Position { set; get; }

                // OPTIONAL //
                [YamlMember(Alias = "w")]
                public int? W { set; get; }

                [YamlMember(Alias = "h")]
                public int? H { set; get; }

                // FILLED IN WHEN CREATING THE COLLECTION //
                public int SpriteDefinitionId;
            }

            public class Clip {
                public static readonly tk2dSpriteAnimationClip.WrapMode DEFAULT_WRAP_MODE = tk2dSpriteAnimationClip.WrapMode.Once;

                [YamlMember(Alias = "wrap_mode")]
                public string WrapMode { set; get; } = "single";

                [YamlMember(Alias = "frame_size")]
                public FrameSize FrameSize { set; get; }

                [YamlMember(Alias = "frames")]
                public List<Frame> Frames { set; get; }

                [YamlMember(Alias = "fps")]
                public int FPS { set; get; } = Animation.DEFAULT_FPS;

                [YamlMember(Alias = "invincible")]
                public List<int> Invincible { set; get; }

                [YamlMember(Alias = "off_ground")]
                public List<int> OffGround { set; get; }

                [YamlMember(Alias = "fidget_duration")]
                public FidgetDuration FidgetDuration { set; get; }
            }

            public class Animation {
                public static readonly string DEFAULT_NAME = "unnamed";
                public static readonly int DEFAULT_FPS = 12;

                [YamlMember(Alias = "spritesheet")]
                public string Spritesheet { set; get; }

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
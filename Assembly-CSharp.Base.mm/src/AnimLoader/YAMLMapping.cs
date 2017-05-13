using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using YamlDotNet;
using YamlDotNet.Serialization;

namespace ETGMod {
    public partial class Animation {
        public static class YAML {
            public class Collection {
                [YamlMember(Alias = "name")]
                public string Name { set; get; }

                [YamlMember(Alias = "spritesheet")]
                public string Spritesheet { set; get; }

                [YamlMember(Alias = "definitions")]
                public Dictionary<string, Definition> Definitions { set; get; }

                [YamlMember(Alias = "def_size")]
                public FrameSize DefSize { set; get; }

                [YamlMember(Alias = "offset")]
                public Position Offset { set; get; } = new Position {
                    X = 0,
                    Y = 0
                };
            }

            public class Definition {
                [YamlMember(Alias = "spritesheet")]
                public string Spritesheet { set; get; }

                [YamlMember(Alias = "x")]
                public int X { set; get; }

                [YamlMember(Alias = "y")]
                public int Y { set; get; }

                [YamlMember(Alias = "w")]
                public int? W { set; get; } = null;

                [YamlMember(Alias = "h")]
                public int? H { set; get; } = null;

                [YamlMember(Alias = "offsx")]
                public int? OffsetX { set; get; } = null;

                [YamlMember(Alias = "offsy")]
                public int? OffsetY { set; get; } = null;

                // FILLED IN WHEN CREATING THE COLLECTION //
                internal int SpriteDefinitionId;
            }

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

                public FrameSize() {
                    Width = 0;
                    Height = 0;
                }
            }

            public class Position {
                [YamlMember(Alias = "x")]
                public int X { set; get; }

                [YamlMember(Alias = "y")]
                public int Y { set; get; }

                public Position() {
                    X = 0;
                    Y = 0;
                }
            }

            public class EventLerpEmissive {
                [YamlMember(Alias = "time")]
                public float Time { set; get; } = 0.5f;

                [YamlMember(Alias = "power")]
                public float Power { set; get; } = 30f;
            }

            public class Event {
                public readonly static EventLerpEmissive DEFAULT_LERP_EMISSIVE = new EventLerpEmissive();

                [YamlMember(Alias = "name")]
                public string Name { set; get; } = "";

                [YamlMember(Alias = "audio")]
                public string Audio { set; get; } = "";

                [YamlMember(Alias = "int")]
                public int Int { set; get; } = 0;

                [YamlMember(Alias = "float")]
                public float Float { set; get; } = 0f;

                [YamlMember(Alias = "play_vfx")]
                public string PlayVFX { set; get; } = "";

                [YamlMember(Alias = "stop_vfx")]
                public string StopVFX { set; get; } = "";

                [YamlMember(Alias = "lerp_emissive")]
                public EventLerpEmissive LerpEmissive { set; get; }

                [YamlMember(Alias = "outline")]
                public string Outline { set; get; }
            }

            public class Frame {
                public static readonly Event DEFAULT_EVENT = new Event();

                [YamlMember(Alias = "definition")]
                public string Definition { set; get; }

                [YamlMember(Alias = "event")]
                public Event Event { set; get; }
            }

            public class Clip {
                public static readonly tk2dSpriteAnimationClip.WrapMode DEFAULT_WRAP_MODE = tk2dSpriteAnimationClip.WrapMode.Once;

                [YamlMember(Alias = "duplicate")]
                public string Duplicate { set; get; }

                [YamlMember(Alias = "wrap_mode")]
                public string WrapMode { set; get; } = "once";

                [YamlMember(Alias = "frames")]
                public List<Frame> Frames { set; get; } = new List<Frame>();

                [YamlMember(Alias = "fps")]
                public int? FPS { set; get; } = null;

                [YamlMember(Alias = "invulnerable")]
                public List<int> Invulnerable { set; get; } = new List<int>();

                [YamlMember(Alias = "off_ground")]
                public List<int> OffGround { set; get; } = new List<int>();

                [YamlMember(Alias = "all_invulnerable")]
                public bool AllInvulnerable { set; get; }

                [YamlMember(Alias = "all_off_ground")]
                public bool AllOffGround { set; get; }

                [YamlMember(Alias = "fidget_duration")]
                public FidgetDuration FidgetDuration { set; get; }
            }

            public class Animation {
                public static readonly string DEFAULT_NAME = "unnamed";
                public static readonly int DEFAULT_FPS = 12;

                [YamlMember(Alias = "collection")]
                public string Collection { set; get; }

                [YamlMember(Alias = "frame_size")]
                public FrameSize FrameSize { set; get; } = new FrameSize {
                    Width = 32,
                    Height = 32
                };

                [YamlMember(Alias = "clips")]
                public Dictionary<string, Clip> Clips { set; get; }

                [YamlMember(Alias = "name")]
                public string Name { set; get; } = null;

                [YamlMember(Alias = "fps")]
                public int? FPS { set; get; } = null;
            }
        }
    }
}
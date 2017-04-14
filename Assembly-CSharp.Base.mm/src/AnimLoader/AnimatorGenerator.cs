using System;
using UnityEngine;
using System.Collections.Generic;
using Random = System.Random;

namespace ETGMod {
    public partial class Animation {
        public class AnimatorGenerator {
            public static Logger Logger = new Logger("AnimatorGenerator");
            public static Shader DefaultSpriteShader = ShaderCache.Acquire("Sprites/Default");

            public static Vector2[] GenerateUVs(Texture2D texture, int x, int y, int width, int height) {
                return new Vector2[] {
                    new Vector2((x        ) / (float) texture.width, (y         ) / (float) texture.height),
                    new Vector2((x + width) / (float) texture.width, (y         ) / (float) texture.height),
                    new Vector2((x        ) / (float) texture.width, (y + height) / (float) texture.height),
                    new Vector2((x + width) / (float) texture.width, (y + height) / (float) texture.height),
                };
            }

            /// <summary>
            /// Generates a tk2d positionX value.
            /// </summary>
            /// <returns>The value to feed into positionX.</returns>
            /// <param name="x">The x coordinate (in pixels).</param>
            /// <param name="y">The y coordinate (in pixels).</param>
            public static Vector3 GenerateTK2DPosition(float x, float y) {
                // Unity units
                // 1 pixel = 1/16
                return new Vector3(x / 16f, y / 16f, 0f);
            }

            public ModLoader.ModInfo ModInfo;
            public YAML.Animation Mapping;
            public Dictionary<string, int> SpritesheetIDMap = new Dictionary<string, int>();
            public Texture2D[] Textures;
            private int _LastSpritesheetID = 0;

            private int _SpritesheetID(string path) {
                int id;
                var normalized_path = path.NormalizePath();
                if (!SpritesheetIDMap.TryGetValue(normalized_path, out id)) {
                    id = _LastSpritesheetID;
                    SpritesheetIDMap[normalized_path] = id;
                    _LastSpritesheetID += 1;
                }
                return id;
            }

            public AnimatorGenerator(ModLoader.ModInfo mod_info, YAML.Animation mapping) {
                ModInfo = mod_info;
                Mapping = mapping;

                // initialize texture array and spritesheet mappings
                Logger.Debug("Generating spritesheet->id->texture mapping");
                var tex_list = new List<Texture2D>();
                // first the general spritesheet, if it exists
                if (mapping.Spritesheet != null) {
                    _SpritesheetID(mapping.Spritesheet);
                    tex_list.Add(mod_info.Load<Texture2D>(mapping.Spritesheet));
                    Logger.Debug($"New spritesheet: '{mapping.Spritesheet}', ID: {_LastSpritesheetID - 1}");
                }

                // ...then the clip spritesheets
                foreach (var clip in mapping.Clips) {
                    if (clip.Value.Spritesheet != null) {
                        _SpritesheetID(clip.Value.Spritesheet);
                        tex_list.Add(mod_info.Load<Texture2D>(clip.Value.Spritesheet));
                        Logger.Debug($"New spritesheet: '{clip.Value.Spritesheet}', ID: {_LastSpritesheetID - 1}");
                    }
                }

                // and then turn the list into an array
                Textures = tex_list.ToArray();
            }

            internal tk2dSpriteAnimationClip[] ConstructClips(tk2dSpriteCollectionData collection) {
                var clips = new List<tk2dSpriteAnimationClip>();
                var dupe_clips = new Dictionary<string, string>();

                foreach (var mclip in Mapping.Clips) {
                    Console.WriteLine($"CONSTRUCTING CLIP {mclip.Key}");

                    if (mclip.Value.Duplicate != null) {
                        Console.WriteLine($"DUPECLIP OF {mclip.Value.Duplicate}!");
                        dupe_clips[mclip.Key] = mclip.Value.Duplicate;
                        continue;
                    }

                    var frames = ConstructFrames(mclip.Value, collection);
                    var wrap_mode = (tk2dSpriteAnimationClip.WrapMode)Enum.Parse(typeof(tk2dSpriteAnimationClip.WrapMode), mclip.Value.WrapMode.Replace('_', ' ').ToTitleCaseInvariant().Replace(" ", ""));
                    Console.WriteLine($"CLIP WRAP MODE: {wrap_mode}");

                    var clip_fps = mclip.Value.FPS ?? Mapping.FPS ?? YAML.Animation.DEFAULT_FPS;

                    var clip = new tk2dSpriteAnimationClip {
                        fps = clip_fps,
                        frames = frames,
                        name = mclip.Key,
                        loopStart = 0,
                        wrapMode = wrap_mode
                    };

                    if (mclip.Value.FidgetDuration != null) {
                        clip.maxFidgetDuration = mclip.Value.FidgetDuration.Max;
                        clip.minFidgetDuration = mclip.Value.FidgetDuration.Min;
                    }

                    for (int i = 0; i < frames.Length; i++) {
                        var frame = frames[i];

                        if (mclip.Value.Invulnerable.Contains(i)) frame.invulnerableFrame = true;
                        if (mclip.Value.OffGround.Contains(i)) frame.groundedFrame = false;
                    }

                    clips.Add(clip);
                }

                if (dupe_clips.Count > 0) {
                    foreach (var dupe in dupe_clips) {
                        tk2dSpriteAnimationClip found_clip = null;
                        for (int i = 0; i < clips.Count; i++) {
                            if (clips[i].name == dupe.Value) found_clip = clips[i];
                        }
                        if (found_clip == null) {
                            Debug.Log($"Could not find clip to duplicate: {dupe.Value}");
                        }
                        var new_clip = new tk2dSpriteAnimationClip {
                            name = dupe.Key,
                            fps = found_clip.fps,
                            frames = found_clip.frames,
                            loopStart = found_clip.loopStart,
                            maxFidgetDuration = found_clip.maxFidgetDuration,
                            minFidgetDuration = found_clip.minFidgetDuration,
                            wrapMode = found_clip.wrapMode
                        };
                        clips.Add(new_clip);
                    }
                }

                return clips.ToArray();
            }

            internal tk2dSpriteAnimationFrame[] ConstructFrames(YAML.Clip clip, tk2dSpriteCollectionData collection) {
                var frames = new List<tk2dSpriteAnimationFrame>();

                for (int i = 0; i < clip.Frames.Count; i++) {
                    var mframe = clip.Frames[i];

                    var frame = new tk2dSpriteAnimationFrame {
                        spriteCollection = collection,
                        spriteId = mframe.SpriteDefinitionId,
                        groundedFrame = true,
                        invulnerableFrame = false
                    };

                    if (clip.AllInvulnerable) frame.invulnerableFrame = true;
                    if (clip.AllOffGround) frame.groundedFrame = false;

                    var @event = YAML.Frame.DEFAULT_EVENT;
                    if (mframe.Event != null) {
                        frame.triggerEvent = true;
                        @event = mframe.Event;
                    }
                    frame.eventInfo = @event.Name;
                    frame.eventVfx = @event.PlayVFX;
                    frame.eventStopVfx = @event.StopVFX;
                    frame.eventInt = @event.Int;
                    frame.eventFloat = @event.Float;
                    frame.eventAudio = @event.Audio;

                    var outline = tk2dSpriteAnimationFrame.OutlineModifier.Unspecified;

                    if (@event.Outline != null) {
                        outline = (tk2dSpriteAnimationFrame.OutlineModifier)Enum.Parse(typeof(tk2dSpriteAnimationFrame.OutlineModifier), @event.Outline.Replace('_', ' ').ToTitleCaseInvariant().Replace(" ", ""));
                    }

                    frame.eventOutline = outline;

                    var lerp_emissive = YAML.Event.DEFAULT_LERP_EMISSIVE;
                    if (@event.LerpEmissive != null) {
                        lerp_emissive = @event.LerpEmissive;
                        frame.eventLerpEmissive = true;
                    }


                    frame.eventLerpEmissiveTime = lerp_emissive.Time;
                    frame.eventLerpEmissivePower = lerp_emissive.Power;

                    // TODO reverse order, or not reverse order?
                    frames.Add(frame);
                }

                return frames.ToArray();
            }

            internal tk2dSpriteCollectionData ConstructCollection() {
                var collection = new tk2dSpriteCollectionData();

                var name = Mapping.Name ?? YAML.Animation.DEFAULT_NAME;

                collection.assetName = name;
                collection.allowMultipleAtlases = false;
                collection.buildKey = 0x0ade;
                collection.dataGuid = "what even is this for";
                collection.spriteCollectionGUID = name;
                collection.spriteCollectionName = name;

                var material_arr = new Material[Textures.Length];

                for (int i = 0; i < Textures.Length; i++) {
                    material_arr[i] = new Material(DefaultSpriteShader);
                    material_arr[i].mainTexture = Textures[i];
                }

                collection.textures = Textures;
                collection.textureInsts = Textures;

                collection.materials = material_arr;
                collection.materialInsts = material_arr;

                collection.needMaterialInstance = false;

                collection.spriteDefinitions = ConstructDefinitions(material_arr);

                return collection;
            }

            internal tk2dSpriteDefinition[] ConstructDefinitions(Material[] materials) {
                var defs = new List<tk2dSpriteDefinition>();
                foreach (var mclip in Mapping.Clips) {
                    for (int i = 0; i < mclip.Value.Frames.Count; i++) {
                        defs.Add(ConstructDefinition(i, mclip.Key, materials, mclip.Value, mclip.Value.Frames[i]));
                        mclip.Value.Frames[i].SpriteDefinitionId = defs.Count - 1;
                    }
                }
                return defs.ToArray();
            }

            internal tk2dSpriteDefinition ConstructDefinition(int index, string clipname, Material[] materials, YAML.Clip clip, YAML.Frame frame) {
                var width = frame.W ?? clip.FrameSize?.Width ?? Mapping.FrameSize.Width;
                var height = frame.H ?? clip.FrameSize?.Height ?? Mapping.FrameSize.Height;

                var x = frame.OffsetX ?? clip.Offset.X;
                var y = frame.OffsetY ?? clip.Offset.Y;

                var spritesheet_id = _SpritesheetID(clip.Spritesheet ?? Mapping.Spritesheet);
                var texture = Textures[spritesheet_id];


                System.Console.WriteLine($"Spritesheet ID Map XDXDXD {SpritesheetIDMap.Count}");
                System.Console.WriteLine($"Textures XDXDXD {Textures.Length}");

                var spritedef = new tk2dSpriteDefinition {
                    normals = new Vector3[] {
                        new Vector3(0.0f, 0.0f, -1.0f),
                        new Vector3(0.0f, 0.0f, -1.0f),
                        new Vector3(0.0f, 0.0f, -1.0f),
                        new Vector3(0.0f, 0.0f, -1.0f),
                    },
                    tangents = new Vector4[] {
                        new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                        new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                        new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                        new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                    },
                    texelSize = new Vector2(0.1f, 0.1f),
                    extractRegion = false,
                    regionX = 0,
                    regionY = 0,
                    regionW = 0,
                    regionH = 0,
                    flipped = tk2dSpriteDefinition.FlipMode.None,
                    complexGeometry = false,
                    physicsEngine = tk2dSpriteDefinition.PhysicsEngine.Physics3D,
                    colliderType = tk2dSpriteDefinition.ColliderType.Box,
                    collisionLayer = CollisionLayer.PlayerHitBox,
                    position0 = GenerateTK2DPosition(x, y),
                    position1 = GenerateTK2DPosition(x + width, y),
                    position2 = GenerateTK2DPosition(x, y + height),
                    position3 = GenerateTK2DPosition(x + width, y + height),
                    materialId = spritesheet_id,
                    material = materials[spritesheet_id],
                    materialInst = materials[spritesheet_id],
                    name = $"{Mapping.Name}_{clipname}_{index}",
                    uvs = GenerateUVs(texture, frame.X, texture.height - 2 - height - frame.Y, width, height)
                };

                Console.WriteLine($"FRAME IN CLIP {clipname}");
                Console.WriteLine($"POSITION: {spritedef.position0}:{spritedef.position1}:{spritedef.position2}:{spritedef.position3}");

                return spritedef;
            }
        }
    }
}

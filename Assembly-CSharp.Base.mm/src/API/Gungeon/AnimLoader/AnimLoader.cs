using System;
using UnityEngine;
using System.Collections.Generic;

namespace Gungeon {
    public static partial class AnimLoader {
        public static tk2dSpriteAnimator FromResource(string path) {
            return ConstructAnimator(YAML.Deserializer.Deserialize<YAML.Animation>(Resources.Load<TextAsset>(path).text));
        }

        private static tk2dSpriteAnimator ConstructAnimator(YAML.Animation mapping, string name = "ANIMLOADER") {
            var go = new GameObject(name);

            var animation = ConstructAnimation(mapping, go);

            var animator = go.AddComponent<tk2dSpriteAnimator>();

            animator.enabled = true;
            animator.Library = animation;
            animator.ClipFps = mapping.FPS;

            return animator;
        }

        private static tk2dSpriteAnimation ConstructAnimation(YAML.Animation mapping, GameObject go) {
            var animation = go.AddComponent<tk2dSpriteAnimation>();

            var clips = ConstructClips(mapping, go);

            animation.enabled = true;
            animation.clips = clips;

            return animation;
        }

        private static tk2dSpriteAnimationClip[] ConstructClips(YAML.Animation mapping, GameObject go) {
            var clips = new List<tk2dSpriteAnimationClip>();

            foreach (var mclip in mapping.Clips) {
                Console.WriteLine($"CONSTRUCTING CLIP {mclip.Key}");

                var frames = ConstructFrames(mapping, go, mclip.Value);

                var clip = new tk2dSpriteAnimationClip {
                    fps = mclip.Value.FPS,
                    frames = frames,
                    name = mclip.Key,
                    loopStart = 0,
                    wrapMode = (tk2dSpriteAnimationClip.WrapMode)Enum.Parse(typeof(tk2dSpriteAnimationClip.WrapMode), mclip.Value.WrapMode)
                };

                if (mclip.Value.FidgetDuration != null) {
                    clip.maxFidgetDuration = mclip.Value.FidgetDuration.Max;
                    clip.minFidgetDuration = mclip.Value.FidgetDuration.Min;
                }

                clips.Add(clip);
            }

            return clips.ToArray();
        }

        private static tk2dSpriteAnimationFrame[] ConstructFrames(YAML.Animation mapping, GameObject go, YAML.Clip clip) {
            var frames = new List<tk2dSpriteAnimationFrame>();

            var collection = ConstructCollection(mapping, go, Resources.Load<Texture2D>(mapping.Spritesheet));

            for (int i = 0; i < clip.Frames.Count; i++) {
                var mframe = clip.Frames[i];

                var frame = new tk2dSpriteAnimationFrame {
                    spriteCollection = collection,
                    spriteId = mframe.SpriteDefinitionId,
                };
            }

            return frames.ToArray();
        }

        private static tk2dSpriteCollectionData ConstructCollection(YAML.Animation mapping, GameObject go, Texture2D texture, string name = "ANIMLOADER") {
            var collection = go.AddComponent<tk2dSpriteCollectionData>();

            collection.assetName = name;
            collection.allowMultipleAtlases = false;
            collection.buildKey = 0x0ade;
            collection.dataGuid = "what even is this for";
            collection.spriteCollectionGUID = name;
            collection.spriteCollectionName = name;

            var texture_arr = new Texture2D[] { texture };

            var material = new Material(ETGMod.Assets.DefaultSpriteShader);
            var material_insts = new Material[] { material };

            collection.textures = texture_arr;
            collection.textureInsts = texture_arr;

            material.mainTexture = texture;

            collection.material = material;
            collection.materials = material_insts;
            collection.materialInsts = material_insts;

            collection.spriteDefinitions = ConstructDefinitions(mapping, go, texture, material);

            return collection;
        }

        private static tk2dSpriteDefinition[] ConstructDefinitions(YAML.Animation mapping, GameObject go, Texture2D texture, Material material) {
            var defs = new List<tk2dSpriteDefinition>();
            foreach (var mclip in mapping.Clips) {
                for (int i = 0; i < mclip.Value.Frames.Count; i++) {
                    defs.Add(ConstructDefinition(mclip.Value, mclip.Value.Frames[i], texture, material));
                    mclip.Value.Frames[i].SpriteDefinitionId = defs.Count - 1;
                }
            }
            return defs.ToArray();
        }

        private static tk2dSpriteDefinition ConstructDefinition(YAML.Clip clip, YAML.Frame frame, Texture2D texture, Material material) {
            var width = frame.W ?? clip.FrameSize.Width;
            var height = frame.H ?? clip.FrameSize.Height;

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
                position0 = new Vector3(16/frame.Position.X, 16/frame.Position.Y),
                position1 = new Vector3(16/frame.Position.X + width, 16/frame.Position.Y),
                position2 = new Vector3(16/frame.Position.X, 16/frame.Position.Y + height),
                position3 = new Vector3(16/frame.Position.X + width, 16/frame.Position.Y + height),
                material = material,
                materialInst = material,
                materialId = 0,
            };

            spritedef.uvs = ETGMod.Assets.GenerateUVs(texture, frame.X, frame.Y, width, height);

            return spritedef;
        }
    }
}

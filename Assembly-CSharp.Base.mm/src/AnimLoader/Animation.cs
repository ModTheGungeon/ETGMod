using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using YamlDotNet.Serialization;
using System.Reflection;

namespace ETGMod {
    public partial class Animation {
        private static Logger _Logger = new Logger("Animation");

        private static Deserializer _Deserializer = new DeserializerBuilder().Build();

        private ModLoader.ModInfo _ModInfo;
        private YAML.Animation _DeserializedYAMLDoc;
        private AnimatorGenerator _Generator;
        public Collection _Collection;
        private tk2dSpriteAnimationClip[] _Clips;
        private int? _FPS = YAML.Animation.DEFAULT_FPS;

        public Animation(ModLoader.ModInfo info, string text, string base_dir = null)
            : this(info, _Deserializer.Deserialize<YAML.Animation>(text), base_dir) {}

        public Animation(ModLoader.ModInfo info, YAML.Animation deserialized, string base_dir = null) {
            _ModInfo = info;
            _DeserializedYAMLDoc = deserialized;
            if (base_dir == null) base_dir = info.Resources.BaseDir;

            _Generator = new AnimatorGenerator(_ModInfo, base_dir, _DeserializedYAMLDoc);
            _Collection = _Generator.Collection;
            _Clips = _Generator.ConstructClips(_Collection.CollectionData);
        }

        public Animation(Collection collection, tk2dSpriteAnimationClip[] clips, int? fps = null) {
            _Collection = collection;
            _Clips = clips;
            _FPS = fps;
        }

        public bool HasClip(string name) {
            for (int i = 0; i < _Clips.Length; i++) {
                if (_Clips[i].name == name) return true;
            }
            return false;
        }

        public int? GetClipID(string name) {
            for (int i = 0; i < _Clips.Length; i++) {
                if (_Clips[i].name == name) return i;
            }
            return null;
        }

        #region I wish C# had macros
        public static void CopyAnimator(tk2dSpriteAnimator source, tk2dSpriteAnimator target) {
            new MMILAccess.BatchAccess<tk2dSpriteAnimator>(source).CopyTo(target);
        }

        public static void CopyCollection(tk2dSpriteCollectionData source, tk2dSpriteCollectionData target) {
            // Commented out lines cause NullReferenceExceptions
            // Most likely because `source` isn't assigned to a game object
            // Accessing `gameObject` causes a NRE too
            // /shrug

            target.allowMultipleAtlases = source.allowMultipleAtlases;
            target.assetName = source.assetName;
            target.buildKey = source.buildKey;
            target.dataGuid = source.dataGuid;
            // target.enabled = source.enabled;
            target.halfTargetHeight = source.halfTargetHeight;
            target.hasPlatformData = source.hasPlatformData;
            // target.hideFlags = source.hideFlags;
            target.invOrthoSize = source.invOrthoSize;
            target.loadable = source.loadable;
            target.managedSpriteCollection = source.managedSpriteCollection;
            target.material = source.material;
            target.materialIdsValid = source.materialIdsValid;
            target.materialInsts = source.materialInsts;
            target.materialPngTextureId = source.materialPngTextureId;
            target.materials = source.materials;
            // target.name = source.name;
            target.needMaterialInstance = source.needMaterialInstance;
            target.pngTextures = source.pngTextures;
            target.premultipliedAlpha = source.premultipliedAlpha;
            target.shouldGenerateTilemapReflectionData = source.shouldGenerateTilemapReflectionData;
            target.spriteCollectionGUID = source.spriteCollectionGUID;
            target.spriteCollectionName = source.spriteCollectionName;
            target.spriteCollectionPlatformGUIDs = source.spriteCollectionPlatformGUIDs;
            target.spriteCollectionPlatforms = source.spriteCollectionPlatforms;
            target.SpriteDefinedAnimationSequences = source.SpriteDefinedAnimationSequences;
            target.SpriteDefinedAttachPoints = source.SpriteDefinedAttachPoints;
            target.SpriteDefinedBagelColliders = source.SpriteDefinedBagelColliders;
            target.SpriteDefinedAnimationSequences = source.SpriteDefinedAnimationSequences;
            target.SpriteDefinedIndexNeighborDependencies = source.SpriteDefinedIndexNeighborDependencies;
            target.spriteDefinitions = source.spriteDefinitions;
            target.SpriteIDsWithAttachPoints = source.SpriteIDsWithAttachPoints;
            target.SpriteIDsWithBagelColliders = source.SpriteIDsWithBagelColliders;
            target.SpriteIDsWithAnimationSequences = source.SpriteIDsWithAnimationSequences;
            target.SpriteIDsWithNeighborDependencies = source.SpriteIDsWithNeighborDependencies;
            // target.tag = source.tag;
            target.textureFilterMode = source.textureFilterMode;
            target.textureInsts = source.textureInsts;
            target.textureMipMaps = source.textureMipMaps;
            target.textures = source.textures;
            target.Transient = source.Transient;
            // target.useGUILayout = source.useGUILayout;
            target.version = source.version;
        }

        public static void CopyDefinition(tk2dSpriteDefinition source, tk2dSpriteDefinition target) {
            target.boundsDataCenter = source.boundsDataCenter;
            target.boundsDataExtents = source.boundsDataExtents;
            target.colliderConvex = source.colliderConvex;
            target.colliderSmoothSphereCollisions = source.colliderSmoothSphereCollisions;
            target.colliderType = source.colliderType;
            target.colliderVertices = source.colliderVertices;
            target.collisionLayer = source.collisionLayer;
            target.complexGeometry = source.complexGeometry;
            target.extractRegion = source.extractRegion;
            target.flipped = source.flipped;
            target.indices = source.indices;
            target.material = source.material;
            target.materialId = source.materialId;
            target.materialInst = source.materialInst;
            target.metadata = source.metadata;
            target.normals = source.normals;
            target.name = source.name;
            target.physicsEngine = source.physicsEngine;
            target.position0 = source.position0;
            target.position1 = source.position1;
            target.position2 = source.position2;
            target.position3 = source.position3;
            target.regionH = source.regionH;
            target.regionW = source.regionW;
            target.regionX = source.regionX;
            target.regionY = source.regionY;
            target.tangents = source.tangents;
            target.texelSize = source.texelSize;
            target.untrimmedBoundsDataCenter = source.untrimmedBoundsDataCenter;
            target.untrimmedBoundsDataExtents = source.untrimmedBoundsDataExtents;
            target.uvs = source.uvs;
        }

        public static void CopyFrame(tk2dSpriteAnimationFrame source, tk2dSpriteAnimationFrame target) {
            target.eventAudio = source.eventAudio;
            target.eventFloat = source.eventFloat;
            target.eventInfo = source.eventInfo;
            target.eventInt = source.eventInt;
            target.eventLerpEmissive = source.eventLerpEmissive;
            target.eventLerpEmissivePower = source.eventLerpEmissivePower;
            target.eventLerpEmissiveTime = source.eventLerpEmissiveTime;
            target.eventOutline = source.eventOutline;
            target.eventStopVfx = source.eventStopVfx;
            target.eventVfx = source.eventVfx;
            target.finishedSpawning = source.finishedSpawning;
            target.forceMaterialUpdate = source.forceMaterialUpdate;
            target.groundedFrame = source.groundedFrame;
            target.invulnerableFrame = source.invulnerableFrame;
            target.requiresOffscreenUpdate = source.requiresOffscreenUpdate;
            target.spriteCollection = source.spriteCollection;
            target.spriteId = source.spriteId;
            target.triggerEvent = source.triggerEvent;
        }

        public static void CopyClip(tk2dSpriteAnimationClip source, tk2dSpriteAnimationClip target) {
            target.name = source.name;
            target.fps = source.fps;
            target.frames = source.frames;
            target.loopStart = source.loopStart;
            target.maxFidgetDuration = source.maxFidgetDuration;
            target.minFidgetDuration = source.minFidgetDuration;
            target.wrapMode = source.wrapMode;
        }
        #endregion

        private tk2dSpriteAnimator _CreateAnimator(GameObject go, tk2dSpriteCollectionData collection, bool replace_component = true) {
            var animation = _CreateAnimation(go, collection, replace_component);
            if (replace_component) {
                _Logger.Debug("Trying to destroy existing tk2dSpriteAnimator!");
                var component = go.GetComponent<tk2dSpriteAnimator>();
                if (component != null) {
                    _Logger.Debug($"Component found");
                    UnityEngine.Object.Destroy(component);
                } else _Logger.Debug($"Component not found");
            }
            var animator = go.AddComponent<tk2dSpriteAnimator>();

            animator.enabled = true;
            animator.Library = animation;
            animator.ClipFps = _FPS ?? YAML.Animation.DEFAULT_FPS;

            return animator;
        }

        private tk2dSpriteAnimation _CreateAnimation(GameObject go, tk2dSpriteCollectionData collection, bool replace_component = true) {
            if (replace_component) {
                _Logger.Debug("Trying to destroy existing tk2dSpriteAnimation!");
                var component = go.GetComponent<tk2dSpriteAnimation>();
                if (component != null) {
                    _Logger.Debug($"Component found");
                    UnityEngine.Object.Destroy(component);
                } else _Logger.Debug($"Component not found");
            }

            var animation = go.AddComponent<tk2dSpriteAnimation>();

            var clips = _Clips.Clone() as tk2dSpriteAnimationClip[];

            for (int i = 0; i < clips.Length; i++) {
                var clip = clips[i];

                for (int j = 0; j < clip.frames.Length; j++) {
                    var frame = clip.frames[j];

                    frame.spriteCollection = collection;
                }
            }

            animation.enabled = true;
            animation.clips = clips;

            return animation;
        }

        private void _UpdateCollectionInSprite(GameObject go, tk2dSpriteCollectionData collection) {
            go.GetComponent<tk2dBaseSprite>().Collection = collection;
        }

        public tk2dSpriteAnimator ApplyAnimator(GameObject go, bool patch_collection = false) {
            _Logger.Debug($"Applying animator on GameObject '{go.name}'");

            tk2dSpriteCollectionData collection;
            if (patch_collection) {
                collection = go.GetComponent<tk2dBaseSprite>().Collection;
                _Collection.PatchCollection(collection);
            } else {
                collection = _Collection.CollectionData;
                _Collection.ApplyCollection(go);
            }
            return _CreateAnimator(go, collection);
        }

        public tk2dSpriteAnimator ApplyAnimator(Component co) {
            return ApplyAnimator(co.gameObject);
        }

        private void _PatchClipsInAnimation(tk2dSpriteAnimationClip[] patch_clips, tk2dSpriteAnimation target, tk2dSpriteCollectionData collection, int sprite_id_offset, Action<tk2dSpriteAnimationClip> each_clip = null) {
            _Logger.Debug($"Patching clips - clips length before: {target.clips.Length}");
            // init a hash of the patch clip names
            // we'll use this later for lookup when we create the new clip list
            // if a patch defines a clip 'dodge_bw', then the patch's clip should be used
            // instead of the target's, and this is how we decide that
            // HashSet instead of List because HashSet lookup is O(1) while List is O(n)
            var clip_name_set = new HashSet<string>();
            for (int i = 0; i < patch_clips.Length; i++) {
                var patch_clip = patch_clips[i];
                clip_name_set.Add(patch_clip.name);
            }

            // not using target.clips in the constructor here
            // because we want to validate each target clip against clip_name_set
            // before we add them here
            var clip_list = new List<tk2dSpriteAnimationClip>();

            for (int i = 0; i < target.clips.Length; i++) {
                var target_clip = target.clips[i];
                if (!clip_name_set.Contains(target_clip.name)) {
                    clip_list.Add(target_clip);
                } else _Logger.Debug($"Not adding original clip '{target_clip.name}', because a patch clip exists");
            }

            for (int i = 0; i < patch_clips.Length; i++) {
                var patch_clip = patch_clips[i];
                var new_clip = new tk2dSpriteAnimationClip();
                CopyClip(patch_clip, new_clip);

                var new_frames = new_clip.frames.Clone() as tk2dSpriteAnimationFrame[];
                for (int j = 0; j < new_frames.Length; j++) {
                    var new_frame = new_frames[j];

                    // we use the sprite ID offset from _PatchCollection here!
                    new_frame.spriteId += sprite_id_offset;
                    // and update the sprite collection 
                    new_frame.spriteCollection = collection;
                }

                new_clip.frames = new_frames;

                if (each_clip != null) each_clip.Invoke(new_clip);

                _Logger.Debug($"Adding patch clip '{new_clip.name}'");
                clip_list.Add(new_clip);
            }

            target.clips = clip_list.ToArray();
            _Logger.Debug($"Patching clips - clips length after: {target.clips.Length}");
        }

        private static FieldInfo _clipFps = typeof(tk2dSpriteAnimator).GetField("clipFps", BindingFlags.Instance | BindingFlags.NonPublic);

        public tk2dSpriteAnimator PatchAnimator(tk2dSpriteAnimator animator, Action<tk2dSpriteAnimationClip> each_clip = null) {
            _Logger.Debug($"Patching animator on GameObject '{animator.gameObject.name}'");

            var collection = animator.gameObject.GetComponent<tk2dBaseSprite>().Collection;

            int sprite_id_offset = _Collection.PatchCollection(collection);
            _PatchClipsInAnimation(_Clips, animator.Library, collection, sprite_id_offset, each_clip);

            if (_FPS != null) {
                _clipFps.SetValue(animator, _FPS.Value);
            }

            return animator;
        }
    }
}

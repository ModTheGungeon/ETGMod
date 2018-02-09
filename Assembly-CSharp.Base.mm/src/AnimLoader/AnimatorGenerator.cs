using System;
using UnityEngine;
using System.Collections.Generic;
using Random = System.Random;
using System.IO;

namespace ETGMod {
    public partial class Animation {
        public class AnimatorGenerator {
            public static Logger _Logger = new Logger("AnimatorGenerator");

            public ModLoader.ModInfo ModInfo;
            public YAML.Animation Mapping;
            public Collection Collection;

            public AnimatorGenerator(ModLoader.ModInfo mod_info, string base_dir, YAML.Animation mapping) {
                ModInfo = mod_info;
                Mapping = mapping;

                Collection = mod_info.LoadCollection(Path.Combine(base_dir, mapping.Collection));
            }

            internal tk2dSpriteAnimationClip[] ConstructClips(tk2dSpriteCollectionData collection) {
                var clips = new List<tk2dSpriteAnimationClip>();
                var dupe_clips = new Dictionary<string, string>();

                foreach (var mclip in Mapping.Clips) {
                    _Logger.Debug($"CONSTRUCTING CLIP {mclip.Key}");

                    if (mclip.Value.Duplicate != null) {
                        _Logger.Debug($"DUPECLIP OF {mclip.Value.Duplicate}!");
                        dupe_clips[mclip.Key] = mclip.Value.Duplicate;
                        continue;
                    }

                    var frames = ConstructFrames(mclip.Value, collection);
                    var wrap_mode = (tk2dSpriteAnimationClip.WrapMode)Enum.Parse(typeof(tk2dSpriteAnimationClip.WrapMode), mclip.Value.WrapMode.Replace('_', ' ').ToTitleCaseInvariant().Replace(" ", ""));
                    _Logger.Debug($"CLIP WRAP MODE: {wrap_mode}");

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

                    _Logger.Debug($"Clip has {frames.Length} frames");

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

                    _Logger.Debug($"Frame {i}: spritedef {mframe.Definition}");

                    var sprite_id = Collection.GetSpriteDefinitionIndex(mframe.Definition);
                    if (sprite_id == null) {
                        _Logger.Error($"Definition '{mframe.Definition}' doesn't exist!");
                        continue;
                    }

                    var frame = new tk2dSpriteAnimationFrame {
                        spriteCollection = collection,
                        spriteId = Collection.GetSpriteDefinitionIndex(mframe.Definition).Value,
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
        }
    }
}

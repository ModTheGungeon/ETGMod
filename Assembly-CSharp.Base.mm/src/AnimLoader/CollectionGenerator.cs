using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ETGMod {
    public partial class Animation {
        public partial class Collection {
            public class CollectionGenerator {
                public static Logger Logger = new Logger("AnimatorGenerator");
                public static Shader DefaultSpriteShader = ShaderCache.Acquire("Sprites/Default");

                public GameObject TargetGameObject;
                public YAML.Collection Mapping;
                public Dictionary<string, int> SpritesheetIDMap = new Dictionary<string, int>();
                public Texture2D[] Textures;
                private int _LastSpritesheetID = 0;

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
                public static Vector3 TK2DPosition(float x, float y) {
                    // Texel size = 1/16, 1/16
                    return new Vector3(x / 16f, y / 16f, 0f);
                }

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

                // Raw mode - you provide the path->texture map
                public CollectionGenerator(Dictionary<string, Texture2D> textures, string base_dir, YAML.Collection mapping, GameObject gameobj) {
                    TargetGameObject = gameobj;
                    Mapping = mapping;

                    var tex_list = new List<Texture2D>();
                    // first the general spritesheet, if it exists
                    if (mapping.Spritesheet != null) {
                        _SpritesheetID(mapping.Spritesheet);
                        tex_list.Add(textures[mapping.Spritesheet]);
                        Logger.Debug($"New spritesheet: '{mapping.Spritesheet}', ID: {_LastSpritesheetID - 1}");
                    }

                    // ...then the clip spritesheets
                    foreach (var def in mapping.Definitions) {
                        if (def.Value.Spritesheet != null) {
                            _SpritesheetID(def.Value.Spritesheet);
                            tex_list.Add(textures[def.Value.Spritesheet]);
                            Logger.Debug($"New spritesheet: '{def.Value.Spritesheet}', ID: {_LastSpritesheetID - 1}");
                        }
                    }

                    Textures = tex_list.ToArray();
                }

                // Mod mode - use ModResources facilities to load textures
                public CollectionGenerator(ModLoader.ModInfo mod_info, string base_dir, YAML.Collection mapping, GameObject gameobj) {
                    TargetGameObject = gameobj;
                    Mapping = mapping;

                    // initialize texture array and spritesheet mappings
                    Logger.Debug("Generating spritesheet->id->texture mapping");
                    var tex_list = new List<Texture2D>();
                    // first the general spritesheet, if it exists
                    if (mapping.Spritesheet != null) {
                        _SpritesheetID(mapping.Spritesheet);
                        tex_list.Add(mod_info.LoadTexture(Path.Combine(base_dir, mapping.Spritesheet)));
                        Logger.Debug($"New spritesheet: '{mapping.Spritesheet}', ID: {_LastSpritesheetID - 1}");
                    }

                    // ...then the clip spritesheets
                    foreach (var def in mapping.Definitions) {
                        if (def.Value.Spritesheet != null) {
                            _SpritesheetID(def.Value.Spritesheet);
                            tex_list.Add(mod_info.LoadTexture(Path.Combine(base_dir, def.Value.Spritesheet)));
                            Logger.Debug($"New spritesheet: '{def.Value.Spritesheet}', ID: {_LastSpritesheetID - 1}");
                        }
                    }

                    // and then turn the list into an array
                    Textures = tex_list.ToArray();
                }

                internal tk2dSpriteCollectionData ConstructCollection() {
                    var collection = TargetGameObject.AddComponent<tk2dSpriteCollectionData>();
                    UnityEngine.Object.DontDestroyOnLoad(collection);

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

                    int i = 0;
                    foreach (var mdef in Mapping.Definitions) {
                        defs.Add(ConstructDefinition(i, mdef.Key, materials, mdef.Value));
                        mdef.Value.SpriteDefinitionId = defs.Count - 1; //TODO this is probably useless now
                        i++;
                    }
                    return defs.ToArray();
                }

                // a bunch of random constants with values very unlikely to be used anywhere
                // used for identifying whether the definition has been generated by ETGMod
                // since I can't add fields to the definition (it just crashes natively
                // somewhere if I do)
                internal const int ETGMOD_ID_X = 1381053425;
                internal const int ETGMOD_ID_Y = -1385198451;
                internal const int ETGMOD_ID_W = -918596149;
                internal const int ETGMOD_ID_H = 109461384;

                internal tk2dSpriteDefinition ConstructDefinition(int index, string name, Material[] materials, YAML.Definition definition) {
                    var width = definition.W ?? Mapping.DefSize?.Width ?? YAML.FrameSize.DEFAULT_WIDTH;
                    var height = definition.H ?? Mapping.DefSize?.Height ?? YAML.FrameSize.DEFAULT_HEIGHT;

                    var x = definition.OffsetX ?? Mapping.Offset?.X ?? 0;
                    var y = definition.OffsetY ?? Mapping.Offset?.Y ?? 0;

                    var wscale = definition.ScaleW ?? Mapping.DefScale?.WScale ?? 1;
                    var hscale = definition.ScaleH ?? Mapping.DefScale?.HScale ?? 1;

                    var spritesheet_id = _SpritesheetID(definition.Spritesheet ?? Mapping.Spritesheet);
                    var texture = Textures[spritesheet_id];

                    var w = width * wscale;
                    var h = height * hscale;

                    var texelf = 1 / 16f;

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
                        texelSize = new Vector2(texelf, texelf),

                        // identification
                        extractRegion = false,
                        regionX = ETGMOD_ID_X,
                        regionY = ETGMOD_ID_Y,
                        regionW = ETGMOD_ID_W,
                        regionH = ETGMOD_ID_H,

                        flipped = tk2dSpriteDefinition.FlipMode.None,
                        complexGeometry = false,
                        physicsEngine = tk2dSpriteDefinition.PhysicsEngine.Physics3D,
                        colliderType = tk2dSpriteDefinition.ColliderType.Unset,
                        collisionLayer = CollisionLayer.PlayerHitBox,
                        materialId = spritesheet_id,
                        material = materials[spritesheet_id],
                        materialInst = materials[spritesheet_id],
                        name = name,
                        position0 = TK2DPosition(x, y - h),
                        position1 = TK2DPosition(x + w, y - h),
                        position2 = TK2DPosition(x, y),
                        position3 = TK2DPosition(x + w, y),
                        uvs = GenerateUVs(texture, definition.X, texture.height - 2 - height - definition.Y, width, height),
                        boundsDataCenter = new Vector3((w * texelf) / 2f, (w * texelf) / 2f, 0f),
                        boundsDataExtents = new Vector3((w * texelf) / 2f, (h * texelf) / 2f, 0f),
                        untrimmedBoundsDataCenter = new Vector3((w * texelf) / 3f, (h * texelf) / 3f, 0f),
                        untrimmedBoundsDataExtents = new Vector3((w * texelf) / 3f, (h * texelf) / 3f, 0f),
                    };
                     
                    Logger.Debug($"DEFINITION {spritedef.name}");
                    Logger.Debug($"POSITION: {spritedef.position0}:{spritedef.position1}:{spritedef.position2}:{spritedef.position3}");

                    return spritedef;
                }
            }
        }
    }
}

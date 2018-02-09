using System;
using System.Collections.Generic;
using UnityEngine;

namespace ETGMod {
    public partial class Animation {
        public partial class Collection {
            public struct DumpData {
                public DumpData(YAML.Collection collection, Dictionary<string, Texture > textures) {
                    Collection = collection;
                    Textures = textures;
                }

                public YAML.Collection Collection;
                public Dictionary<string, Texture> Textures;

                public string SerializeCollection() {
                    return YAML.Serializer.Serialize(Collection);
                }
            }

            public DumpData Dump() {
                return Dump(CollectionData);
            }

            public static DumpData Dump(tk2dSpriteCollectionData coll) {
                var textures = new Dictionary<string, Texture>();
                var textureid_to_texture_map = new Dictionary<int, string>();
                var texture_datas = new Dictionary<int, GridPacker.PackedData>();
                
                var yml = new YAML.Collection();

                string first_spritesheet = "[ERROR! ERROR! ERROR!]";

                // TODO
                // get the list of textures in a better way (using the
                // actual textures instead of materials, that is, making sure
                // there are no duplicate textures)

                Console.WriteLine($"PREMAT");
                

                Material first_material = coll.material;
                if (coll.materials != null && coll.materials.Length == 1) {
                    first_material = coll.materials[0];
                }

                Console.WriteLine($"PREMAT2");
                if (coll.materials == null || coll.materials.Length == 1) {
                    Console.WriteLine($"IF");
                    first_spritesheet = coll.spriteCollectionName;
                    textures[first_spritesheet] = first_material.mainTexture;
                    textureid_to_texture_map[0] = first_spritesheet;
                } else {
                    for (int i = 0; i < coll.materialInsts.Length; i++) {
                        _Logger.Debug($"MATERIALINST: [{coll.materialInsts[i]}]");
                    }
                    for (int i = 0; i < coll.materials.Length; i++) {
                        _Logger.Debug($"MATERIAL: [{coll.materials[i]}]");
                    }

                    Console.WriteLine($"ELSE");
                    for (int i = 0; i < coll.materialInsts.Length; i++) {
                        _Logger.Debug($"Digging in material ID {i}");
                        var mat = coll.materialInsts[i];

                        var spritesheet_name = $"{coll.spriteCollectionName}_{i}";
                        if (i == 0) first_spritesheet = spritesheet_name;
                        
                        List<GridPacker.Element> elements = null;
                        for (int j = 0; j < coll.spriteDefinitions.Length; j++) {
                            _Logger.Debug($"Sprite definition #{j}, looking for material ID {i}");
                            var idef = coll.spriteDefinitions[j];
                            if (idef == null) continue;

                            _Logger.Debug($"Definition: {idef.name}, materialId: {idef.materialId}, wanted materialId: {i}");
                            _Logger.Debug($"post def");
                            if (idef.materialId != i) continue;
                            _Logger.Debug($"Found element for spritesheet ID {i}: {idef.name}");
                            if (elements == null) elements = new List<GridPacker.Element>();
                            _Logger.Debug($"Adding grid packer element {idef.name}");
                            elements.Add(new GridPacker.Element((Texture2D)idef.material.mainTexture, idef.uvs, idef.name));
                        }

                        _Logger.Debug($"Looked through all spriteDefs");

                        if (elements != null) {
                            _Logger.Debug($"Sorting elements");
                            elements.Sort((x, y) => string.Compare(x.Name, y.Name));
                            _Logger.Debug($"Packing {elements.Count} entries for spritesheet ID {i}");
                            GridPacker.PackedData data = GridPacker.Pack(elements, force_pot: true);
                            texture_datas[i] = data;
                            textures[spritesheet_name] = data.Output;
                        } else {
                            _Logger.Debug($"elements == null mat:[{mat}] mat.mainTexture:[{mat?.mainTexture}]");
                            if (mat != null) {
                                textures[spritesheet_name] = ((Texture2D)mat.mainTexture).GetRW();
                            } else {
                                _Logger.Warn($"Material ID #{i} is null and has no assigned sprite defs, it will therefore be ignored");
                            }
                        }

                        _Logger.Debug($"Mapping ID {i} to texture {spritesheet_name}");
                        textureid_to_texture_map[i] = spritesheet_name;
                    }
                }

                yml.Name = coll.spriteCollectionName;
                yml.Spritesheet = first_spritesheet;
                yml.Definitions = new Dictionary<string, YAML.Definition>();

                //var elements = new List<GridPacker.Element>();
                for (int i = 0; i < coll.spriteDefinitions.Length; i++) {
                    var def = coll.spriteDefinitions[i];

                    GridPacker.PackedData data;
                    if (!texture_datas.TryGetValue(def.materialId, out data)) {
                        throw new Exception($"Couldn't get cached PackedData of texture of material with ID {def.materialId} - this should never happen");
                    }

                    _Logger.Debug($"Reading packed texture entry of sprite definition {def.name} ({i})");
                    foreach (var ent in  data.Entries) {
                        Console.WriteLine($"{ent.Key}");
                    }

                    var entry = data.Entries[def.name];

                    Console.WriteLine($"=== UVS ===");
                    Console.WriteLine($"{def.uvs[0].x} {def.uvs[0].y}");
                    Console.WriteLine($"{def.uvs[1].x} {def.uvs[1].y}");
                    Console.WriteLine($"{def.uvs[2].x} {def.uvs[2].y}");
                    Console.WriteLine($"{def.uvs[3].x} {def.uvs[3].y}");

                    for (int j = 0; j < def.uvs.Length; j++) {
                        var uv = def.uvs[j];
                    }
                    var ymldef = yml.Definitions[def.name] = new YAML.Definition();

                    var tex_id = def.materialId;
                    var spritesheet_name = textureid_to_texture_map[tex_id];
                    var spritesheet_tex = textures[spritesheet_name];

                    var etgmod_def = ((BasePatches.tk2dSpriteDefinition)def);

                    ymldef.X = entry.X;
                    ymldef.Y = entry.Y;
                    ymldef.W = entry.Width;
                    ymldef.H = entry.Height;
                    ymldef.OffsetX = etgmod_def.GetETGModOffsetX();
                    ymldef.OffsetY = etgmod_def.GetETGModOffsetY();
                    ymldef.ScaleW = etgmod_def.GetScaleW(spritesheet_tex);
                    ymldef.ScaleH = etgmod_def.GetScaleH(spritesheet_tex);
                }

                // TODO ymldef.DefSize
                // TODO ymldef.DefScale
                // TODO ymldef.Offset
                // right now always handled per-def

                if (!System.IO.Directory.Exists(System.IO.Path.Combine(Paths.ManagedFolder, "textures_test"))) {
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Paths.ManagedFolder, "textures_test"));
                }
                foreach (var tex in textures) {
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(Paths.ManagedFolder, $"textures_test/{tex.Key}.png"), ((Texture2D)tex.Value).EncodeToPNG());
                }

                return new DumpData(yml, textures);
            }
        }
    }
}
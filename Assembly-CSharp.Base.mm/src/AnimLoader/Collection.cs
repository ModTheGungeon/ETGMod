using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ETGMod {
    public partial class Animation {
        public partial class Collection {
            private static Logger _Logger = new Logger("Animation");

            private static Deserializer _Deserializer = new DeserializerBuilder().Build();

            public GameObject GameObject;
            private YAML.Collection _DeserializedYAMLDoc;
            private CollectionGenerator _Generator;
            public tk2dSpriteCollectionData CollectionData;

            public Collection(ModLoader.ModInfo info, string text, string base_dir = null)
                : this(info, _Deserializer.Deserialize<YAML.Collection>(text), base_dir) { }

            public Collection(ModLoader.ModInfo info, YAML.Collection deserialized, string base_dir = null) {
                _DeserializedYAMLDoc = deserialized;
                if (base_dir == null) base_dir = info.Resources.BaseDir;

                GameObject = new GameObject($"ETGMod Collection '{deserialized.Name}'");

                _Generator = new CollectionGenerator(info, base_dir, _DeserializedYAMLDoc, GameObject);
                CollectionData = _Generator.ConstructCollection();
            }

            public Collection(Dictionary<string, Texture2D> textures, YAML.Collection deserialized, string base_dir) {
                _DeserializedYAMLDoc = deserialized;
                GameObject = new GameObject($"ETGMod Collection '{deserialized.Name}'");

                _Generator = new CollectionGenerator(textures, base_dir, _DeserializedYAMLDoc, GameObject);
                CollectionData = _Generator.ConstructCollection();
            }

            public tk2dSpriteDefinition GetSpriteDefinition(string name) {
                return CollectionData.GetSpriteDefinition(name);
            }

            public int? GetSpriteDefinitionIndex(string name) {
                int id = CollectionData.GetSpriteIdByName(name);
                if (id == -1) return null;
                return id;
            }

            public void ApplyCollection(GameObject go) {
                var collection = go.GetComponent<tk2dSpriteCollectionData>();
                if (collection != null) {
                    _Logger.Info("Removing already existing collection from game object");
                    UnityEngine.Object.Destroy(collection);
                }
                go.GetComponent<tk2dBaseSprite>().Collection = CollectionData;
            }

            public int PatchCollection(tk2dSpriteCollectionData target) {
                // since we modify the target, we have to save these now
                int material_id_offset = target.materials.Length;

                // the reason we calculate the sprite ID offset right now and
                // don't even use it in this method (but instead just return it)
                // is because we modify target.spriteDefinitions later on
                // therefore, any attempt at calculating the offset after running this method
                // will yield totally wrong results
                int sprite_id_offset = target.spriteDefinitions.Length;

                _Logger.Debug($"Material ID offset: {material_id_offset}, sprite ID offset: {sprite_id_offset}");

                // list of merged materials from both target and source
                // note the layout - first target materials, then source materials
                // this means that the length of *just* the target materials
                // will always be the where our target materials start
                // therefore, `material_id_offset`
                var materials_list = new List<Material>(target.materials);
                for (int i = 0; i < CollectionData.materials.Length; i++) {
                    _Logger.Debug($"Adding patch material #{i}");
                    materials_list.Add(CollectionData.materials[i]);
                }
                var materials_arr = materials_list.ToArray();

                // try to do the most sensible thing here
                // if target is a bit more unusual and target.materialInsts doesn't have the same refs
                // as target.materials, we do the same thing as above for materialInsts
                // except that we use target.materialInsts
                // in the current version of the animator generation materialInsts has the same refs as materials
                // but just in case that's changed in the future, this also pulls from materialInsts on the
                // patch
                if (!target.materials.SequenceEqual(target.materialInsts)) {
                    _Logger.Debug($"target.materials sequence-inequal to target.materialInsts");

                    var materialinsts_list = new List<Material>(target.materialInsts);
                    for (int i = 0; i < CollectionData.materialInsts.Length; i++) {
                        materialinsts_list.Add(CollectionData.materialInsts[i]);
                    }
                    target.materialInsts = materialinsts_list.ToArray();
                } else {
                    // of course, if the refs are the same,
                    // then save on allocations and just assign the same array
                    // to both fields
                    target.materialInsts = materials_arr;
                }

                target.materials = materials_arr;

                var patched_in_order_lookup = new HashSet<tk2dSpriteDefinition>();
                var definitions_list = new List<tk2dSpriteDefinition>();
                var patch_definition_name_lookup = new Dictionary<string, tk2dSpriteDefinition>();

                for (int i = 0; i < CollectionData.spriteDefinitions.Length; i++) {
                    var def = CollectionData.spriteDefinitions[i];
                    patch_definition_name_lookup[def.name] = def;
                }

                for (int i = 0; i < target.spriteDefinitions.Length; i++) {
                    var def = target.spriteDefinitions[i];
                    tk2dSpriteDefinition source_definition = null;
                    if (patch_definition_name_lookup.TryGetValue(def.name, out source_definition)) {
                        _Logger.Debug($"Patching definition '{def.name}' at ID {i}");

                        var new_definition = new tk2dSpriteDefinition();
                        CopyDefinition(source_definition, new_definition);

                        // this is why we saved material_id_offset before
                        // we can update the definition's material data by simply adding the offset to the id
                        // and updating the materials
                        new_definition.materialId += material_id_offset;
                        new_definition.material = target.materials[new_definition.materialId];
                        new_definition.materialInst = target.materialInsts[new_definition.materialId];

                        definitions_list.Add(new_definition);

                        patched_in_order_lookup.Add(source_definition);
                    } else {
                        definitions_list.Add(def);
                    }
                }

                int total_index = definitions_list.Count - 1;
                for (int i = 0; i < CollectionData.spriteDefinitions.Length; i++) {
                    var source_definition = CollectionData.spriteDefinitions[i];
                    if (patched_in_order_lookup.Contains(source_definition)) continue;

                    total_index++;
                    _Logger.Debug($"Adding definition '{source_definition.name}' at ID {total_index}");
                    var new_definition = new tk2dSpriteDefinition();
                    CopyDefinition(source_definition, new_definition);

                    new_definition.materialId += material_id_offset;
                    new_definition.material = target.materials[new_definition.materialId];
                    new_definition.materialInst = target.materialInsts[new_definition.materialId];

                    definitions_list.Add(new_definition);
                }

                target.spriteDefinitions = definitions_list.ToArray();

                // finally, return the sprite id offset
                return sprite_id_offset;
            }
        }
    }
}

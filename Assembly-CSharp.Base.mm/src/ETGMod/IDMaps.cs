using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ETGMod {
    public partial class ETGMod : Backend {
        public static IDPool<PickupObject> Items;

        private IDPool<T> _ReadIDMap<T>(IList<T> list, string path) where T : UnityEngine.Object {
            var pool = new IDPool<T>();

            using (var file = File.OpenRead(path)) {
                using (var reader = new StreamReader(file)) {
                    var line_id = 0;

                    while (!reader.EndOfStream) {
                        line_id += 1;
                        var line = reader.ReadLine().Trim();
                        if (line.StartsWithInvariant("#")) continue;
                        if (line.Length == 0) continue;

                        var split = line.Split(' ');
                        if (split.Length < 2) {
                            throw new Exception($"Failed parsing ID map file: not enough columns at line {line_id} (need at least 2, ID and the name)");
                        }

                        int id;
                        if (!int.TryParse(split[0], out id)) throw new Exception($"Failed parsing ID map file: ID column at line {line_id} was not an integer");

                        try {
                            pool[$"gungeon:{split[1]}"] = list[id];
                        } catch (Exception e) {
                            throw new Exception($"Failed loading ID map file: Error while adding entry to ID pool ({e.Message})");
                        }
                    }
                }
            }

            pool.LockNamespace("gungeon");
            return pool;
        }

        private void _InitIDs() {
            var id_pool_base = Path.Combine(Paths.ResourcesFolder, "idmaps");
            Items = _ReadIDMap(PickupObjectDatabase.Instance.Objects, Path.Combine(id_pool_base, "items.txt"));
        }
    }
}
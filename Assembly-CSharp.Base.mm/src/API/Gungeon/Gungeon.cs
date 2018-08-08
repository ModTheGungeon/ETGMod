using System;
using System.IO;
using System.Reflection;
namespace Gungeon {
    public static partial class Game {
        public static IDPool<PickupObject> Items = new IDPool<PickupObject>();
        public static IDPool<AIActor> Enemies = new IDPool<AIActor>();

        private static Assembly _Assembly = Assembly.GetExecutingAssembly();

        private static void _SetupPool<T>(string map_file_name, IDPool<T> pool, Action<string, string> add_method) {
            using (Stream stream = _Assembly.GetManifestResourceStream($"Content/gungeon_id_map/{map_file_name}")) {
                using (StreamReader reader = new StreamReader(stream)) {
                    string line;
                    while (true) {
                        line = reader.ReadLine();
                        if (line == null) break;
                        Console.WriteLine($"STARTS WITH HASH? {line.StartsWithInvariant("#")}");
                        if (line.StartsWithInvariant("#")) continue;
                        string[] split = line.Split(' ');
                        add_method.Invoke(split[0], split[1]);
                    }
                }
            }
            pool.LockNamespace("gungeon");
        }

        public static void Initialize() {
            _SetupPool("items.txt", Items, (string real, string mapped) => {
                int id;
                if (!int.TryParse(real, out id)) throw new Exception("Failed parsing item id map");
                Items.Add(mapped, PickupObjectDatabase.GetById(id)); // adding resolves the id (nonamespace becomes gungeon:nonamespace)
            });
            _SetupPool("enemies.txt", Enemies, (string real, string mapped) => {
                Enemies.Add(mapped, EnemyDatabase.GetOrLoadByGuid(real));
            });
        }
    }
}

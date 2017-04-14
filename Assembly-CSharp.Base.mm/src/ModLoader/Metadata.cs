using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using YamlDotNet.Serialization;

namespace ETGMod {
    public partial class ModLoader {
        public partial class ModInfo {
            /// <summary>
            /// Mod metadata class (the YML file is parsed into this object)
            /// </summary>
            public class Metadata {
                public class Dependency {
                    [YamlMember(Alias = "name")]
                    public string Name { set; get; } = "Unknown";

                    [YamlMember(Alias = "version")]
                    public Version Version { set; get; } = new Version(1, 0, 0);
                }
                [YamlMember(Alias = "name")]
                public string Name { set; get; } = "Unknown";

                [YamlMember(Alias = "version")]
                public Version Version { set; get; } = new Version(1, 0, 0);

                [YamlMember(Alias = "description")]
                public string Description { set; get; } = "";

                [YamlMember(Alias = "author")]
                public string Author { set; get; } = "Unknown";

                [YamlMember(Alias = "url")]
                public string URL { set; get; } = "";

                [YamlMember(Alias = "dll")]
                public string DLL { set; get; } = null;

                [YamlMember(Alias = "dependencies")]
                public List<Dependency> Dependencies { set; get; } = new List<Dependency>();

                [YamlMember(Alias = "modpack_dir")]
                public string ModPackDir { set; get; } = null;

                /// <summary>
                /// Extra information for other backends.
                /// Deserialized and reparsed when needed.
                /// </summary>
                [YamlMember(Alias = "extra")]
                public Dictionary<string, object> Extra { set; get; } = null;

                public bool HasDLL {
                    get {
                        return DLL != null;
                    }
                }

                public bool IsModPack {
                    get {
                        return ModPackDir != null;
                    }
                }

                private Deserializer _ExtraDeserializer = new DeserializerBuilder().Build();
                private Serializer _ExtraSerializer = new SerializerBuilder().Build();
                public T ExtraData<T>(string id) {
                    if (Extra == null) return default(T);

                    object extra;
                    if (!Extra.TryGetValue(id, out extra)) {
                        return default(T);
                    }

                    // HACK
                    string extra_ser = _ExtraSerializer.Serialize(extra);
                    return (T)_ExtraDeserializer.Deserialize<T>(extra_ser);
                }
            }
        }
    }
}

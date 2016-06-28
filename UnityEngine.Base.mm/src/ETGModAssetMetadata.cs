using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace UnityEngine {
    /// <summary>
    /// ETGMod asset metadata.
    /// </summary>
    public class ETGModAssetMetadata {

        public static Dictionary<string, ETGModAssetMetadata> Map = new Dictionary<string, ETGModAssetMetadata>();

        public string Zip;
        public string File;
        public long Offset;
        public int Length;

        public ETGModAssetMetadata() {
        }

        public ETGModAssetMetadata(string file)
            : this(file, 0, 0) {
        }
        public ETGModAssetMetadata(string file, long offset, int length)
            : this() {
            File = file;
            Offset = offset;
            Length = length;
        }

        public ETGModAssetMetadata(string zip, string file)
            : this() {
            Zip = zip;
            File = file;
        }

    }
}
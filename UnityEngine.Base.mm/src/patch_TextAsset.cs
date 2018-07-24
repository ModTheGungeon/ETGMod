#pragma warning disable 0626
#pragma warning disable 0649

namespace UnityEngine {
    // must be public as it's accessed from outside
    public class patch_TextAsset : TextAsset {

        // TODO hook get_text, get_bytes

        public string textOverride {
            get; set;
        }

    }
}

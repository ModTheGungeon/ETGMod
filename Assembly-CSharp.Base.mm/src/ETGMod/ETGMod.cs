using System;
namespace ETGMod {
    public class ETGMod : Backend {
        public new static Version Version = new Version(0, 1, 0);

#if DEBUG
        public static string VersionTag = "DEBUG";
#elif RELEASE
        public static string VersionTag = "";
#endif

        private static string _FullVersion;
        public static string FullVersion {
            // extremely tiny optimization
            get {
                if (_FullVersion != null) return _FullVersion;
                return _FullVersion = $"{Version}-{VersionTag}";
            }
        }

        public new static void Init() {
            DefaultLogger.Info($"Core ETGMod init {FullVersion}");
            ETGModBehaviour.Add();
        }

        public static void Awake() {
        }

        public static void Update() {
            
        }

        public static void FixedUpdate() {
            
        }
    }
}

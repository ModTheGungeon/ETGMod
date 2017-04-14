using System;
using System.Collections.Generic;
using System.Reflection;

namespace ETGMod {
    public abstract class Backend : UnityEngine.MonoBehaviour {
        public struct Info {
            public string Name;
            public Version Version;
            public Type Type;
            public Backend Instance;
            public string StringVersion;
        }

        public abstract Version Version {
            get;
        }

        public string StringVersion {
            get {
                return Version.ToString();
            }
        }

        public static UnityEngine.GameObject GameObject {
            get;
            internal set;
        }

        public static List<Info> AllBackends = new List<Info>();

        public static void AddBackendInfo(Info info) {
            AllBackends.Add(info);
        }

        public struct SearchResult {
            public enum Type {
                NoMatch,
                OnlyVersionMatches,
                OnlyNameMatches,
                PerfectMatch
            }

            private Type _Result;
            public Info? BestMatch;

            public bool NameMatched() {
                return _Result == Type.PerfectMatch || _Result == Type.OnlyNameMatches;
            }

            public bool VersionMatched() {
                return _Result == Type.PerfectMatch || _Result == Type.OnlyVersionMatches;
            }

            public bool FullyMatched() {
                return _Result == Type.PerfectMatch;
            }

            public bool NotMatched() {
                return _Result == Type.NoMatch;
            }

            public bool OnlyNameMatched() {
                return NameMatched() && !FullyMatched();
            }

            public bool OnlyVersionMatched() {
                return VersionMatched() && !FullyMatched();
            }

            public SearchResult(Type result, Info? best_match) {
                _Result = result;
                BestMatch = best_match;
            }
        }

        public static SearchResult SearchForBackend(string name, Version version = null) {
            Info? best_match = null;
            SearchResult.Type result = SearchResult.Type.NoMatch;

            for (int i = 0; i < AllBackends.Count; i++) {
                var backend = AllBackends[i];

                if (result != SearchResult.Type.PerfectMatch && backend.Name == name && backend.Version == version) {
                    Loader.Logger.Debug($"Perfect match for search (name={name},version={version}) found: {backend.Name} {backend.Version}");
                    best_match = backend;
                    result = SearchResult.Type.PerfectMatch;
                    break; // break early, since the perfect match has been found
                } else if (result != SearchResult.Type.OnlyNameMatches && backend.Name == name && backend.Version != version) {
                    Loader.Logger.Debug($"Name match for search (name={name},version={version}) found: {backend.Name} {backend.Version}");
                    best_match = backend;
                    result = SearchResult.Type.OnlyNameMatches;
                    // continue on, in an attempt to find a perfect match
                } else if (result != SearchResult.Type.OnlyNameMatches && backend.Name != name && backend.Version == version) {
                    Loader.Logger.Debug($"Version match for search (name={name},version={version}) found: {backend.Name} {backend.Version}");
                    best_match = backend;
                    result = SearchResult.Type.OnlyVersionMatches;
                }
            }

            if (best_match == null) {
                Loader.Logger.Debug($"No match for search (name={name},version={version}) found");
            }

            return new SearchResult(result, best_match);
        }

        public abstract void Loaded();
        public virtual void AllBackendsLoaded() {}
    }
}

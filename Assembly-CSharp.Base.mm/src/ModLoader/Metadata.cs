using System;
using System.IO;
using System.Collections.Generic;

namespace ETGMod.ModLoader {
    public class Metadata {
        public struct Dependency {
            public string Name;
            public Version Version;
        }

        public static char[] SPLIT_SEPARATOR_ARRAY = { ':' };

        public string Name = "Unknown";
        public string Version = "1.0.0";
        public string Author = "Unknown";
        public string URL = "None";
        public string DLL = "mod.dll";
        public List<Dependency> Dependencies = new List<Dependency>();

        public class ParseException : Exception {
            public ParseException() : base("Failed parsing mod metadata") { }
        }

        private void Set(string key, string value) {
            switch(key) {
            case "name": Name = value; break;
            case "version": Version = value; break;
            case "author": Author = value; break;
            case "url": URL = value; break;
            case "dll": DLL = value; break;
            case "depends":
                var dependencies = value.Split(';');
                for (int i = 0; i < dependencies.Length; i++) {
                    var dep = dependencies[i].Trim();
                    var name_and_version = dep.Split(' ');

                    if (name_and_version.Length < 2) throw new ParseException();

                    var name = name_and_version[0].Trim();
                    var version = name_and_version[1].Trim();

                    DefaultLogger.Debug($"Mod dependency: {dep}");

                    Dependencies.Add(new Dependency {
                        Name = name,
                        Version = new Version(version)
                    });
                }
                break;
            default:
                DefaultLogger.Warn($"Unknown mod metadata key: {key}");
                break;
            }
        }

        public void ParseLine(string line) {
            var split = line.Split(SPLIT_SEPARATOR_ARRAY, 2);

            if (split.Length < 2) throw new ParseException();

            var key = split[0].Trim();
            var value = split[1].Trim();

            Set(key, value);
        }

        public Metadata(Stream stream) {
            var reader = new StreamReader(stream);

            while (!reader.EndOfStream) {
                ParseLine(reader.ReadLine());
            }
        }

        public Metadata(string data) {
            var reader = new StringReader(data);

            string line;
            while ((line = reader.ReadLine()) != null) {
                ParseLine(line);
            }
        }

        public Metadata() {}
    }
}

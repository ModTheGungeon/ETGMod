using System;
using System.Collections.Generic;
using System.Reflection;

namespace ETGMod {
    internal class AssemblyNameMap {
        private Dictionary<string, string> _AsmNameToPathMap = new Dictionary<string, string>();
        private Dictionary<string, string> _PathToAsmNameMap = new Dictionary<string, string>();

        public void AddAssembly(string path) {
            var asmname = AssemblyName.GetAssemblyName(path).Name;
            _AsmNameToPathMap[asmname] = path;
            _PathToAsmNameMap[path] = asmname;
        }

        public string GetAssemblyName(string path) {
            return _PathToAsmNameMap[path];
        }

        public string GetPath(string asmname) {
            return _AsmNameToPathMap[asmname];
        }

        public bool TryGetAssemblyName(string path, out string result) {
            return _AsmNameToPathMap.TryGetValue(path, out result);
        }

        public bool TryGetPath(string asmname, out string result) {
            return _PathToAsmNameMap.TryGetValue(asmname, out result);
        }

        public bool ContainsPath(string path) {
            return _PathToAsmNameMap.ContainsKey(path);
        }

        public bool ContainsAssemblyName(string asmname) {
            return _AsmNameToPathMap.ContainsKey(asmname);
        }
    }
}

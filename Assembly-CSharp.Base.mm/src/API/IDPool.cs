using System;
using System.Collections.Generic;
using UnityEngine;

namespace ETGMod {
    public class IDPool<T>
    {
        private Dictionary<string, T> _Storage = new Dictionary<string, T>();
        private HashSet<string> _LockedNamespaces = new HashSet<string>();
        private HashSet<string> _Namespaces = new HashSet<string>();

        public T this[string id]
        {
            set
            {
                Set(Resolve(id), value);
            }
            get
            {
                return Get(id);
            }
        }

        public int Count
        {
            get
            {
                return _Storage.Count;
            }
        }

        //Exceptions
        public class IDPoolException : Exception {
            public IDPoolException(string msg) : base(msg) {}
        }

        public class NonExistantIDException : IDPoolException
        {
            public NonExistantIDException(string id) : base($"Object with ID {id} doesn't exist") { }
        }

        public class BadIDElementException : IDPoolException
        {
            public BadIDElementException(string name) : base($"The ID's {name} can not contain any colons or whitespace") { }
        }

        public class LockedNamespaceException : IDPoolException
        {
            public LockedNamespaceException(string namesp) : base($"The ID namespace {namesp} is locked") { }
        }

        public class ItemIDExistsException : IDPoolException
        {
            public ItemIDExistsException(string id) : base($"Item {id} already exists") { }
        }

        public class BadlyFormattedIDException : IDPoolException
        {
            public BadlyFormattedIDException(string id) : base($"ID was improperly formatted: {id}") { }
        }

        //Methods
        public void LockNamespace(string namesp)
        {
            _LockedNamespaces.Add(namesp);
        }

        public void Set(string id, T obj)
        {
            id = Resolve(id);
            VerifyID(id);
            var entry = Split(id);
            if (_LockedNamespaces.Contains(entry.Namespace)) throw new LockedNamespaceException(entry.Namespace);
            var ws = false;
            for (int i = 0; i < id.Length; i++) {
                if (char.IsWhiteSpace(id[i])) {
                    ws = true;
                    break;
                }
            }
            if (ws) throw new BadIDElementException("name");
            _Storage[id] = obj;
            if (!_Namespaces.Contains(entry.Namespace))
            {
                _Namespaces.Add(entry.Namespace);
            }
        }

        public void Add(string id, T obj)
        {
            id = Resolve(id);
            if (_Storage.ContainsKey(id)) throw new ItemIDExistsException(id);
            Set(id, obj);
        }

        public T Get(string id)
        {
            id = Resolve(id);
            if (!_Storage.ContainsKey(id)) throw new NonExistantIDException(id);
            return _Storage[id];
        }

        public void Remove(string id, bool destroy = true)
        {
            id = Resolve(id);
            var split = Split(id);
            if (_LockedNamespaces.Contains(split.Namespace)) throw new LockedNamespaceException(split.Namespace);
            if (!_Storage.ContainsKey(id)) throw new NonExistantIDException(id);
            if (_Storage[id] is UnityEngine.Object && destroy) UnityEngine.Object.Destroy(_Storage[id] as UnityEngine.Object);
            _Storage.Remove(id);
        }

        public void Rename(string source, string target)
        {
            source = Resolve(source);
            target = Resolve(target);
            var target_entry = Split(target);
            if (_LockedNamespaces.Contains(target_entry.Namespace)) throw new LockedNamespaceException(target_entry.Namespace);
            if (!_Storage.ContainsKey(source)) throw new NonExistantIDException(source);

            var obj = _Storage[source];
            _Storage.Remove(source);
            _Storage[target] = obj;
        }

        public static void VerifyID(string id)
        {
            if (id.Count(':') > 1) throw new BadlyFormattedIDException(id);
        }

        public static string Resolve(string id)
        {
            id = id.Trim();
            if (id.Contains(":"))
            {
                VerifyID(id);
                return id;
            }
            else
            {
                return $"gungeon:{id}";
            }
        }

        //Strut
        public struct Entry
        {
            public string Namespace;
            public string Name;

            public Entry(string namesp, string name)
            {
                Namespace = namesp;
                Name = name;
            }
        }

        public static Entry Split(string id)
        {
            VerifyID(id);
            string[] split = id.Split(':');
            if (split.Length != 2) throw new BadlyFormattedIDException(id);
            return new Entry(split[0], split[1]);
        }

        //bools
        public bool ContainsID(string id)
        {
            return _Storage.ContainsKey(Resolve(id));
        }

        public bool NamespaceIsLocked(string namesp)
        {
            return _LockedNamespaces.Contains(namesp);
        }

        public string[] AllIDs
        {
            get
            {
                var ary = new string[_Storage.Count];
                var i = 0;
                foreach (var k in _Storage.Keys) {
                    ary[i] = k;
                    i += 1;
                }
                return ary;
            }
        }

        //IEnumerables
        public IEnumerable<T> Entries
        {
            get
            {
                foreach (var v in _Storage.Values)
                {
                    yield return v;
                }
            }
        }

        public IEnumerable<string> IDs
        {
            get
            {
                foreach (var k in _Storage.Keys)
                {
                    yield return k;
                }
            }
        }

        public IEnumerable<KeyValuePair<string, T>> Pairs
        {
            get
            {
                foreach (var kv in _Storage)
                {
                    yield return new KeyValuePair<string, T>(kv.Key, kv.Value);
                }
            }
        }

        public T RandomValue
        {
            get
            {
                var count = _Storage.Count;
                var idx = UnityEngine.Random.Range(0, count - 1);

                var i = 0;
                foreach (var v in _Storage.Values) {
                    if (idx == i) return v;
                    i += 1;
                }

                throw new Exception("shouldn't happen");
            }
        }

        public string RandomKey {
            get {
                var count = _Storage.Count;
                var idx = UnityEngine.Random.Range(0, count - 1);

                var i = 0;
                foreach (var k in _Storage.Keys) {
                    if (idx == i) return k;
                    i += 1;
                }

                throw new Exception("shouldn't happen");
            }
        }

        public KeyValuePair<string, T> RandomPair {
            get {
                var count = _Storage.Count;
                var idx = UnityEngine.Random.Range(0, count - 1);

                var i = 0;
                foreach (var pair in _Storage) {
                    if (idx == i) return pair;
                    i += 1;
                }

                throw new Exception("shouldn't happen");
            }
        }
    }
}

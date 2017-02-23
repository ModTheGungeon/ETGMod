using System;
using System.Collections.Generic;
using System.Linq;

public class IDPool<T> {
    private Dictionary<string, T> _Storage = new Dictionary<string, T>();
    private HashSet<string> _LockedNamespaces = new HashSet<string>();
    private HashSet<string> _Namespaces = new HashSet<string>();

    public T this[string id] {
        set {
            Set(Resolve(id), value);
        }
        get {
            return Get(id);
        }
    }

    public class NonExistantIDException : Exception {
        public NonExistantIDException(string id) : base($"Object with ID {id} doesn't exist") { }
    }

    public class BadIDElementException : Exception {
        public BadIDElementException(string name) : base($"The ID's {name} can not contain any colons or whitespace") { }
    }

    public class LockedNamespaceException : Exception {
        public LockedNamespaceException(string namesp) : base($"The ID namespace {namesp} is locked") { }
    }

    public class ItemIDExistsException : Exception {
        public ItemIDExistsException(string id) : base($"Item {id} already exists") { }
    }

    public class BadlyFormattedIDException : Exception {
        public BadlyFormattedIDException(string id) : base($"ID was improperly formatted: {id}") { }
    }

    public void LockNamespace(string namesp) {
        _LockedNamespaces.Add(namesp);
    }

    private void Set(string id, T obj) {
        _Storage[id] = obj;
        var entry = Split(id);
        if (!_Namespaces.Contains(entry.Namespace)) {
            _Namespaces.Add(entry.Namespace);
        }
    }

    public void Set(string namesp, string id, T obj) {
        if (_LockedNamespaces.Contains(namesp)) throw new LockedNamespaceException(namesp);
        if (namesp.Contains(":")) throw new BadIDElementException("namespace");
        if (id.Contains(":")) throw new BadIDElementException("name");
        if (namesp.Any(char.IsWhiteSpace)) throw new BadIDElementException("namespace");
        if (id.Any(char.IsWhiteSpace)) throw new BadIDElementException("name");
        Set($"{namesp}:{id}", obj);
    }

    public void Add(string namesp, string id, T obj) {
        if (_Storage.ContainsKey($"{namesp}:{id}")) throw new ItemIDExistsException($"{namesp}:{id}");
        Set(namesp, id, obj);
    }

    public T Get(string id) {
        Console.WriteLine($"GETTING {id}");
        id = Resolve(id);
        if (!_Storage.ContainsKey(id)) throw new NonExistantIDException(id);
        return _Storage[id];
    }

    public void Remove(string id) {
        id = Resolve(id);
        var split = Split(id);
        if (_LockedNamespaces.Contains(split.Namespace)) throw new LockedNamespaceException(split.Namespace);
        if (!_Storage.ContainsKey(id)) throw new NonExistantIDException(id);
        _Storage.Remove(id);
    }

    protected static void VerifyID(string id) {
        if (id.Count(':') > 1) throw new BadlyFormattedIDException(id);
    }

    public static string Resolve(string id) {
        Console.WriteLine($"RESOLVING {id}");
        id = id.Trim();
        if (id.Contains(":")) {
            Console.WriteLine("CONTAINS COLON, VERIFY AND GOOOOO");
            VerifyID(id);
            return id;
        } else {
            Console.WriteLine($"DOESN'T CONTAIN COLON, RETURN gungeon:{id}");
            return $"gungeon:{id}";
        }
    }

    public struct Entry {
        public string Namespace;
        public string Name;

        public Entry(string namesp, string name) {
            Namespace = namesp;
            Name = name;
        }
    }

    public static Entry Split(string id) {
        VerifyID(id);
        string[] split = id.Split(':');
        if (split.Length != 2) throw new BadlyFormattedIDException(id);
        return new Entry(split[0], split[1]);
    }

    public bool ContainsID(string id) {
        return _Storage.ContainsKey(Resolve(id));
    }

    public bool NamespaceIsLocked(string namesp) {
        return _LockedNamespaces.Contains(namesp);
    }

    public string[] IDs {
        get {
            return _Storage.Keys.ToArray();
        }
    }
}

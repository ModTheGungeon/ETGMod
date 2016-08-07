using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public static class JSONHelper {

    public static class META {

        /// <summary>
        /// The metadata object marker. Use it inside metadata objects to specify which type of metadata it is.
        /// </summary>
        public const string MARKER = ".";
        /// <summary>
        /// The property "marker". Use it in normal objects as property name and the metadata object as value.
        /// </summary>
        public const string PROP = ":";
        /// <summary>
        /// The ValueType / struct "marker". Use it as value to PROP in value types.
        /// </summary>
        public const string VALUETYPE = "~";

        public const string REF                 = "ref";
        public const int REF_NONE               = -1;
        public const string REF_ID              = "#";
        public const string REF_TYPE            = "=";
        public const int REF_TYPE_SAMEREF       = 0;
        public const int REF_TYPE_EQUAL         = 1;

        public const string OBJTYPE         = "objtype";
        public const string TYPE            = "type";
        public const string TYPE_FULLNAME   = "name";
        public const string TYPE_SPLIT      = "split";
        public const string TYPE_GENPARAMS  = "params";

        public const string EXTERNAL                = "external";
        public const string EXTERNAL_PATH           = "path";
        public const string EXTERNAL_IN             = "in";
        public const string EXTERNAL_IN_RESOURCES   = "Resources.Load";
        public const string EXTERNAL_IN_RELATIVE    = "relative";
        public const string EXTERNAL_IN_SHARED      = "shared";

        public const string ARRAYAT         = "at";
        public const string ARRAYAT_INDEX   = "index";
        public const string ARRAYAT_VALUE   = "value";

    }

    private static Assembly _Asm = Assembly.GetCallingAssembly();
    private static Type[] _Types = _Asm.GetExportedTypes();
    private static Type t_JSONConfig = typeof(JSONConfig);
    private static object[] a_object_0 = new object[0];

    public static Dictionary<Type, JSONConfig> Configs = new Dictionary<Type, JSONConfig>();
    public static JSONConfig ConfigValueType = new JSONValueTypeConfig();

    public static string DumpDir;
    private static Dictionary<UnityEngine.Object, string> _DumpObjPathMap = new Dictionary<UnityEngine.Object, string>();
    private static Dictionary<string, int> _DumpNameIdMap = new Dictionary<string, int>();

    static JSONHelper() {
        List<Type> _Types_Assignable = new List<Type>(_Types.Length);
        for (int i = 0; i < _Types.Length; i++) {
            Type t = _Types[i];
            if (!t_JSONConfig.IsAssignableFrom(t)) {
                continue;
            }
            _Types_Assignable.Add(t);
        }
        _Types = _Types_Assignable.ToArray();
    }

    public static JSONConfig GetJSONConfig(this object obj) {
        return obj.GetType().GetJSONConfig();
    }
    public static JSONConfig GetJSONConfig(this Type type_) {
        Type type = type_;
        JSONConfig config;
        if (Configs.TryGetValue(type_, out config)) {
            return config;
        }
        if (type.IsValueType) {
            return Configs[type_] = new JSONValueTypeConfig().Fill(type_);
        }

        while (type != null) {
            for (int i = 0; i < _Types.Length; i++) {
                Type t = _Types[i];
                Type bi = t;
                while ((bi = bi.BaseType) != null) {
                    if (!bi.IsGenericType || bi.GetGenericTypeDefinition() != typeof(JSONConfig<>)) {
                        continue;
                    }
                    if (type == bi.GetGenericArguments()[0]) {
                        return Configs[type_] = ((JSONConfig) t.GetConstructor(Type.EmptyTypes).Invoke(a_object_0)).Fill(type_);
                    }
                }
            }
            type = type.BaseType;
        }

        return Configs[type_] = new JSONConfig().Fill(type_);
    }

    public static JsonHelperWriter WriteJSON(string path) {
        File.Delete(path);
        Stream stream = File.OpenWrite(path);
        StreamWriter text = new StreamWriter(stream);
        JsonHelperWriter json = new JsonHelperWriter(text);
        json.Formatting = Formatting.Indented;
        return json;
    }

    public static void WriteJSON(this object obj, string path) {
        if (obj == null) {
            return;
        }
        if (obj is JToken) {
            File.Delete(path);
            File.WriteAllText(path, obj.ToString());
            return;
        }

        Type type = obj.GetType();
        if (obj is Enum || obj is string || obj is byte[] || type.IsPrimitive) {
            JToken.FromObject(obj).WriteJSON(path);
            return;
        }

        using (JsonHelperWriter json = WriteJSON(path)) {
            json.Write(obj);
        }
    }

    public static void Write(this JsonHelperWriter json, object obj) {
        if (obj == null) {
            json.WriteNull();
            return;
        }
        if (obj is JToken) {
            json.WriteRawValue(obj.ToString());
            return;
        }

        Type type = obj.GetType();
        if (obj is Enum || obj is string || obj is byte[] || type.IsPrimitive) {
            json.WriteValue(obj);
            return;
        }

        if (obj is Type) {
            json.WriteMetaType((Type) obj);
            return;
        }

        if (json.TryWriteMetaReference(obj, true)) {
            return;
        }

        JSONConfig config = type.GetJSONConfig();
        if (config.GetType() == t_JSONConfig) {
            if (obj is IEnumerable) {
                json.WriteStartArray();
                IEnumerable enumerable = (IEnumerable) obj;
                foreach (object o in enumerable) {
                    json.Write(o);
                }
                json.WriteEndArray();
                return;
            }
            if (obj is IDictionary) {
                json.WriteStartArray();
                IDictionary dict = (IDictionary) obj;
                foreach (DictionaryEntry e in dict) {
                    json.Write(e);
                }
                json.WriteEndArray();
                return;
            }
        }

        UnityEngine.Object so = (UnityEngine.Object) (
                       ((object) (obj as GameObject)) ??
                       ((object) (obj as ScriptableObject)) ?? 
                       ((object) (obj as Component))
        );
        string name = so?.name;
        if (json.RootWritten && (json.DumpDir != null || DumpDir != null) && !string.IsNullOrEmpty(name) && !(obj is Transform)) {
            if (DumpDir == null) {
                Directory.CreateDirectory(json.DumpDir);
                string dumppath = Path.Combine(json.DumpDir, name + ".json");
                if (!File.Exists(dumppath)) {
                    using (JsonHelperWriter jsonSub = WriteJSON(dumppath)) {
                        jsonSub.DumpDir = Path.Combine(json.DumpDir, name);
                        jsonSub.Write(obj);
                    }
                }
                json.WriteMetaAssetReference(name, META.EXTERNAL_IN_RELATIVE);

            } else {
                string path;
                if (_DumpObjPathMap.TryGetValue(so, out path)) {
                    json.WriteMetaAssetReference(path, META.EXTERNAL_IN_SHARED);
                    return;
                }
                path = type.Name + "s/" + name;

                int id;
                if (!_DumpNameIdMap.TryGetValue(path, out id)) {
                    id = -1;
                }
                _DumpNameIdMap[name] = ++id;

                if (id != 0) {
                    path += "." + id;
                }
                _DumpObjPathMap[so] = path;

                string dumppath = Path.Combine(DumpDir, path.Replace('/', Path.DirectorySeparatorChar) + ".json");
                Directory.GetParent(dumppath).Create();
                if (!File.Exists(dumppath)) {
                    using (JsonHelperWriter jsonSub = WriteJSON(dumppath)) {
                        jsonSub.Write(obj);
                    }
                }
                json.WriteMetaAssetReference(path, META.EXTERNAL_IN_SHARED);
            }
            return;
        }

        json.RootWritten = true;
        config.Serialize(json, obj);
    }

    public static void WriteStartMetadata(this JsonHelperWriter json, string metatype) {
        json.WriteStartObject();
        json.WriteProperty(META.MARKER, metatype);
    }
    public static void WriteEndMetadata(this JsonHelperWriter json) {
        json.WriteEndObject();
    }

    public static bool TryWriteMetaReference(this JsonHelperWriter json, object obj, bool register = false) {
        int id = json.GetReferenceID(obj);

        if (id != META.REF_NONE) {
            json.WriteStartMetadata(META.REF);

            json.WritePropertyName(META.REF_ID);
            json.WriteValue(id);
            json.WritePropertyName(META.REF_TYPE);
            json.WriteValue(json.GetReferenceType(id, obj));

            json.WriteEndMetadata();
            return true;
        }

        if (register) {
            json.RegisterReference(obj);
        }
        return false;
    }

    public static void WriteMetaObjectType(this JsonHelperWriter json, object obj) {
        json.WriteMetaType_(obj.GetType(), META.OBJTYPE);
    }
    public static void WriteMetaType(this JsonHelperWriter json, Type type) {
        json.WriteMetaType_(type, META.TYPE);
    }
    private static void WriteMetaType_(this JsonHelperWriter json, Type type, string metatype) {
        json.WriteStartMetadata(metatype);

        json.WriteProperty(META.TYPE_FULLNAME, type.FullName);
        string ns = type.Namespace;
        if (ns != null) {
            json.WritePropertyName(META.TYPE_SPLIT);
            json.WriteStartArray();
            json.Write(ns);
            json.Write(type.Name);
            json.WriteEndArray();
        }
        Type[] genparams = type.GetGenericArguments();
        if (genparams.Length != 0) {
            json.WriteProperty(META.TYPE_GENPARAMS, genparams);
        }

        json.WriteEndMetadata();
    }

    public static void WriteMetaAssetReference(this JsonHelperWriter json, string path, string @in = META.EXTERNAL_IN_RESOURCES) {
        json.WriteStartMetadata(META.EXTERNAL);

        json.WriteProperty(META.EXTERNAL_PATH, path);
        if (@in != META.EXTERNAL_IN_RESOURCES) {
            json.WriteProperty(META.EXTERNAL_IN, @in);
        }

        json.WriteEndMetadata();
    }

    public static void WriteProperty(this JsonHelperWriter json, string name, object obj) {
        json.WritePropertyName(name);
        json.Write(obj);
    }

    public static void Write(this JsonHelperWriter json, JSONConfig config, object obj, MemberInfo info) {
        config.Serialize(json, obj, info);
    }

    public static void WriteAll(this JsonHelperWriter json, JSONConfig config, object obj, MemberInfo[] infos) {
        for (int i = 0; i < infos.Length; i++) {
            config.Serialize(json, obj, infos[i]);
        }
    }

}

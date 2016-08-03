using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public static class JSONHelper {

    public const int NOREF = -1;

    public const string META_PROP = ".";
    public const int META_VAL = 1337;
    public const string META_REFID = "r#";
    public const string META_REFTYPE = "r=";
    public const int META_REFTYPE_SAMEREF = 0;
    public const int META_REFTYPE_EQUAL = 1;

    private static Assembly _Asm = Assembly.GetCallingAssembly();
    private static Type[] _Types = _Asm.GetExportedTypes();
    private static Type t_JSONConfig = typeof(JSONConfig);
    private static object[] a_object_0 = new object[0];

    public static Dictionary<Type, JSONConfig> Configs = new Dictionary<Type, JSONConfig>();

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
        if (Configs.TryGetValue(type, out config)) {
            return config;
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
                        return Configs[type_] = (JSONConfig) t.GetConstructor(Type.EmptyTypes).Invoke(a_object_0);
                    }
                }
            }
            type = type.BaseType;
        }

        return Configs[type_] = new JSONConfig();
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

        if (obj is Type) {
            // Json.NET claims to support Type, yet fails on MonoType : RuntimeType : Type..?!
            JToken.FromObject(((Type) obj).FullName).WriteJSON(path);
            return;
        }

        File.Delete(path);
        using (Stream stream = File.OpenWrite(path))
        using (StreamWriter text = new StreamWriter(stream))
        using (JsonHelperWriter json = new JsonHelperWriter(text)) {
            json.Formatting = Formatting.Indented;
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
            // Json.NET claims to support Type, yet fails on MonoType : RuntimeType : Type..?!
            json.WriteValue(((Type) obj).FullName);
            return;
        }

        int id = json.GetReferenceID(obj);
        if (id != NOREF) {
            json.WriteStartMetadata();

            json.WritePropertyName(META_REFID);
            json.WriteValue(id);
            json.WritePropertyName(META_REFTYPE);
            json.WriteValue(json.GetReferenceType(id, obj));

            json.WriteEndMetadata();
            return;
        }
        json.RegisterReference(obj);

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

        config.Serialize(json, obj);
    }

    public static void WriteStartMetadata(this JsonHelperWriter json) {
        json.WriteStartObject();
        json.WritePropertyName(META_PROP);
        json.WriteValue(META_VAL);
    }
    public static void WriteEndMetadata(this JsonHelperWriter json) {
        json.WriteEndObject();
    }

    public static void WriteProperty(this JsonHelperWriter json, string name, object obj) {
        json.WritePropertyName(name);
        json.Write(obj);
    }

    public static void Write(this JsonHelperWriter json, JSONConfig config, object obj, MemberInfo info, bool isPrivate = false) {
        config.Serialize(json, obj, info, isPrivate);
    }

    public static void WriteAll(this JsonHelperWriter json, JSONConfig config, object obj, MemberInfo[] infos, bool isPrivate = false) {
        for (int i = 0; i < infos.Length; i++) {
            json.Write(config, obj, infos[i], isPrivate);
        }
    }

}

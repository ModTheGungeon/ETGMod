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

    public const string METAPROP = ".META";
    public const int METAVAL = 0;
    public const string META_REFID = "REFID";
    public const string META_REFTYPE = "REFTYPE";
    public const int META_REFTYPE_SAMEREF = 0;
    public const int META_REFTYPE_EQUAL = 1;

    private static Assembly _Asm = Assembly.GetCallingAssembly();
    private static Type[] _Types = _Asm.GetExportedTypes();
    private static Type t_JSONConfig = typeof(JSONConfig);
    private static object[] a_object_0 = new object[0];

    public static Func<JSONConfig> CreateDefaultConfig = () => new JSONConfig();
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
    public static JSONConfig GetJSONConfig(this Type type) {
        JSONConfig config;
        if (Configs.TryGetValue(type, out config)) {
            return config;
        }

        for (int i = 0; i < _Types.Length; i++) {
            Type t = _Types[i];
            Type bi = t;
            while ((bi = bi.BaseType) != null) {
                if (!bi.IsGenericType || bi.GetGenericTypeDefinition() != typeof(JSONConfig<>)) {
                    continue;
                }
                if (type == bi.GetGenericArguments()[0]) {
                    return Configs[type] = (JSONConfig) t.GetConstructor(Type.EmptyTypes).Invoke(a_object_0);
                }
            }
        }

        return Configs[type] = CreateDefaultConfig();
    }

    public static JToken ToJSON(this object obj) {
        if (obj == null) {
            return null;
        }
        if (obj is JToken) {
            return (JToken) obj;
        }

        Type type = obj.GetType();
        if (obj is Enum) {
            return obj.ToString().ToJSON();
        }
        if (obj is string || obj is byte[] || obj is Type || type.IsPrimitive) {
            return JToken.FromObject(obj);
        }

        if (obj is IEnumerable) {
            JArray json = new JArray();
            IEnumerable enumerable = (IEnumerable) obj;
            foreach (object o in enumerable) {
                json.Add(o.ToJSON());
            }
            return json;
        }
        if (obj is IDictionary) {
            JArray json = new JArray();
            IDictionary dict = (IDictionary) obj;
            foreach (DictionaryEntry e in dict) {
                json.Add(e.ToJSON());
            }
            return json;
        }

        return type.GetJSONConfig().Serialize(obj);
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
        if (obj is Enum || obj is string || obj is byte[] || obj is Type || type.IsPrimitive) {
            obj.ToJSON().WriteJSON(path);
            return;
        }

        File.Delete(path);
        using (Stream stream = File.OpenWrite(path))
        using (StreamWriter text = new StreamWriter(stream))
        using (JsonHelperWriter json = new JsonHelperWriter(text)) {
            json.Formatting = Formatting.Indented;
            obj.WriteJSON(json);
        }
    }

    public static void WriteJSON(this object obj, JsonHelperWriter json) {
        if (obj == null) {
            json.WriteNull();
            return;
        }
        if (obj is JToken) {
            json.WriteRawValue(obj.ToString());
            return;
        }

        Type type = obj.GetType();
        if (obj is Enum || obj is string || obj is byte[] || obj is Type || type.IsPrimitive) {
            json.WriteValue(obj);
            return;
        }

        if (obj is IEnumerable) {
            json.WriteStartArray();
            IEnumerable enumerable = (IEnumerable) obj;
            foreach (object o in enumerable) {
                o.WriteJSON(json);
            }
            json.WriteEndArray();
            return;
        }
        if (obj is IDictionary) {
            json.WriteStartArray();
            IDictionary dict = (IDictionary) obj;
            foreach (DictionaryEntry e in dict) {
                e.WriteJSON(json);
            }
            json.WriteEndArray();
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
        type.GetJSONConfig().Serialize(obj, json);
    }

    public static void WriteStartMetadata(this JsonWriter json) {
        json.WriteStartObject();
        json.WritePropertyName(METAPROP);
        json.WriteValue(METAVAL);
    }
    public static void WriteEndMetadata(this JsonWriter json) {
        json.WriteEndObject();
    }

    public static JToken Add(this JObject json, JSONConfig config, object obj, MemberInfo info, bool isPrivate = false) {
        JToken token = config.Serialize(obj, info, isPrivate);
        if (token != null) {
            json[info.Name] = token;
        }
        return token;
    }
    public static void Add(this JsonHelperWriter json, JSONConfig config, object obj, MemberInfo info, bool isPrivate = false) {
        config.Serialize(obj, info, json, isPrivate);
    }

    public static void AddAll(this JObject json, JSONConfig config, object obj, MemberInfo[] props, bool isPrivate = false) {
        for (int i = 0; i < props.Length; i++) {
            json.Add(config, obj, props[i], isPrivate);
        }
    }
    public static void AddAll(this JsonHelperWriter json, JSONConfig config, object obj, MemberInfo[] props, bool isPrivate = false) {
        for (int i = 0; i < props.Length; i++) {
            json.Add(config, obj, props[i], isPrivate);
        }
    }

}

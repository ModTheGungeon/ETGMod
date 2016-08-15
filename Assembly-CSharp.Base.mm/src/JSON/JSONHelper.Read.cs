using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public static partial class JSONHelper {

    public static bool CheckOnRead =
#if DEBUG
        true;
#else
        false;
#endif

    public static JsonHelperReader OpenReadJSON(Stream stream) {
        StreamReader text = new StreamReader(stream);
        JsonHelperReader json = new JsonHelperReader(text);
        return json;
    }
    public static JsonHelperReader OpenReadJSON(string path) {
        JsonHelperReader json = OpenReadJSON(File.OpenRead(path));
        json.RelativeDir = path.Substring(0, path.Length - 5);
        return json;
    }

    public static object ReadJSON(Stream stream) {
        using (JsonHelperReader json = OpenReadJSON(stream)) {
            json.Read(); // Go to Start
            return json.ReadObject();
        }
    }
    public static object ReadJSON(string path) {
        using (JsonHelperReader json = OpenReadJSON(path)) {
            json.Read(); // Go to Start
            return json.ReadObject();
        }
    }
    public static T ReadJSON<T>(Stream stream) {
        using (JsonHelperReader json = OpenReadJSON(stream)) {
            json.Read(); // Go to Start
            return json.ReadObject<T>();
        }
    }
    public static T ReadJSON<T>(string path) {
        using (JsonHelperReader json = OpenReadJSON(path)) {
            json.Read(); // Go to Start
            return json.ReadObject<T>();
        }
    }

    public static object ReadObject(this JsonHelperReader json) {
        json.Read(); // Drop StartObject
        string metaProp = json.ReadPropertyName();

        if (metaProp == META.MARKER) {
            return json.ReadObjectFromMeta();
        }

        return json.ReadObject(json.ReadMetaObjectType(), true);
    }
    public static T ReadObject<T>(this JsonHelperReader json, bool skipHeader = false) {
        return (T) json.ReadObject(typeof(T), skipHeader);
    }
    public static object ReadObject(this JsonHelperReader json, Type type, bool skipHeader = false) {
        if (LOG) {
            Console.WriteLine("READING " + json.RelativeDir + ", ");
            Console.WriteLine(json.Path + " (" + json.LineNumber + ", " + json.LinePosition + "): " + type.FullName);
            Console.WriteLine("(" + json.TokenType + ", " + json.Value + ")");
        }

        if (type == t_string || type == t_byte_a) {
            object value = json.Value;
            json.Read(); // Drop value
            return value;
        }
        if (type.IsEnum) {
            object value = json.Value;
            json.Read(); // Drop value
            return Enum.ToObject(type, value);
        }
        if (
            type.IsPrimitive ||
            (json.TokenType != JsonToken.StartArray && json.TokenType != JsonToken.StartObject && json.TokenType != JsonToken.PropertyName)
           ) {
            object value = json.Value;
            json.Read(); // Drop value
            if (value == null) {
                return null;
            }
            return Convert.ChangeType(value, type);
        }

        if (json.TokenType == JsonToken.StartArray) {
            json.Read(); // Drop StartArray
            int size;
            string arrayType = json.ReadMetaArrayData(out size);

            if (arrayType == META.ARRAYTYPE_LIST || arrayType == META.ARRAYTYPE_ARRAY) {
                Type itemType = type.GetListType();
                IList tmplist = null;
                IList list = null;
                if (type.IsArray) {
                    tmplist = new ArrayList();
                    list = Array.CreateInstance(type.GetElementType(), size);
                } else {
                    tmplist = list = (IList) ReflectionHelper.Instantiate(type);
                }

                json.RegisterReference(list);

                if (itemType != null) {
                    while (json.TokenType != JsonToken.EndArray) {
                        tmplist.Add(json.ReadObject(itemType));
                    }
                } else {
                    while (json.TokenType != JsonToken.EndArray) {
                        tmplist.Add(json.ReadObject());
                    }
                }

                if (type.IsArray) {
                    ((ArrayList) tmplist).CopyTo((Array) list);
                }

                json.Read(); // Drop EndArray
                return list;
            }

            if (arrayType == META.ARRAYTYPE_MAP) {
                // TODO Get key / value types
                IDictionary map = (IDictionary) ReflectionHelper.Instantiate(type);

                json.RegisterReference(map);

                while (json.TokenType != JsonToken.EndArray) {
                    DictionaryEntry entry = json.ReadObject<DictionaryEntry>();
                    map[entry.Key] = entry.Value;
                }

                json.Read(); // Drop EndArray
                return map;
            }
        }

        if (type == t_Type) {
            Type value = json.ReadMetaType();
            return value;
        }

        if (!skipHeader) {
            json.Read(); // Drop Start
        }

        JSONRule rule = GetJSONRule(type);
        if (!skipHeader) {
            Type typeO = type;
            string metaProp = rule.ReadMetaHeader(json, ref type);
            if (metaProp != null) {
                if (metaProp != META.MARKER) {
                    throw new JsonReaderException("Invalid meta prop: Expected . or : , got " + metaProp);
                }
                return json.ReadObjectFromMeta();
            }
            if (typeO != type) {
                rule = GetJSONRule(type);
            }
        }
        return json.FillObject(rule.New(json, type), type, rule, true);
    }

    public static object FillObject(this JsonHelperReader json, object obj, Type type = null, JSONRule rule = null, bool skipHeader = false) {
        if (type == null) {
            type = obj.GetType();
        }
        if (!skipHeader) {
            json.Read(); // Drop Start
        }

        if (rule == null) {
            rule = GetJSONRule(type);
        }
        if (!skipHeader) {
            Type typeO = type;
            string metaProp = rule.ReadMetaHeader(json, ref type);
            if (metaProp != null) {
                if (metaProp != META.MARKER) {
                    throw new JsonReaderException("Invalid meta prop: Expected . or : , got " + metaProp);
                }
                return json.FillObjectFromMeta(obj);
            }
            if (typeO != type) {
                rule = GetJSONRule(type);
            }
        }
        int id = json.RegisterReference(obj);
        obj = rule.Deserialize(json, obj);
        _OnAfterDeserialize(obj);
        json.UpdateReference(obj, id);

        json.Read(); // Drop End
        return obj;
    }

    public static object ReadObjectFromMeta(this JsonHelperReader json) {
        string metaType = (string) json.Value;
        json.Read(); // Drop value

        if (metaType == META.EXTERNAL) {
            return json.ReadMetaExternal(true);
        }

        if (metaType == META.REF) {
            return json.ReadMetaReference(true);
        }

        throw new JsonReaderException("Invalid meta type: " + metaType + " invalid object replacement");
    }

    public static object FillObjectFromMeta(this JsonHelperReader json, object obj) {
        string metaType = (string) json.Value;
        json.Read(); // Drop value

        if (metaType == META.EXTERNAL) {
            using (JsonHelperReader ext = json.OpenMetaExternal(true)) {
                ext.Read(); // Go to Start
                obj = ext.FillObject(obj);
                json.RegisterReference(obj); // External references are still being counted
            }
        }

        if (metaType == META.REF) {
            // FIXME Implement filling from references
            throw new JsonReaderException("Filling objects from references not supported!");
        }

        throw new JsonReaderException("Invalid meta type: " + metaType + " invalid object replacement");
    }

    public static JsonHelperReader OpenMetaExternal(this JsonHelperReader json, bool skipHeader = false) {
        string currentProp;
        if (!skipHeader) {
            json.ReadStartMetadata(META.EXTERNAL);
        }

        currentProp = json.ReadPropertyName();
        if (currentProp == META.EXTERNAL_PATH) {
            // No "in" property, which means this is a Resources.Load call! Return null!
            return null;
        }
        string @in = (string) json.Value;
        json.Read(); // Drop value
        string path = (string) json.ReadRawProperty(META.EXTERNAL_PATH);

        string fullpath = "";

        if (@in == META.EXTERNAL_IN_RELATIVE) {
            if (json.RelativeDir == null) {
                throw new NullReferenceException("json.RelativeDir == null, but found relative external reference!");
            }
            fullpath = Path.Combine(json.RelativeDir, path + ".json");
        } else if (@in == META.EXTERNAL_IN_SHARED) {
            if (SharedDir == null) {
                throw new NullReferenceException("JSONHelper.SharedDir == null, but found shared external reference!");
            }
            fullpath = Path.Combine(SharedDir, path.Replace('/', Path.DirectorySeparatorChar) + ".json");
        }

        json.ReadEndMetadata();

        JsonHelperReader ext = OpenReadJSON(fullpath);
        ext.Global.AddRange(json.Global);
        return ext;
    }

    public static object ReadMetaExternal(this JsonHelperReader json, bool skipHeader = false) {
        using (JsonHelperReader ext = json.OpenMetaExternal(skipHeader)) {
            if (ext == null) {
                // Resources.Load call - path PropertyName dropped already.
                string path = (string) json.Value;
                json.Read(); // Drop value
                json.ReadEndMetadata();
                return Resources.Load(path);
            }

            ext.Read(); // Go to Start
            object obj = ext.ReadObject();
            json.RegisterReference(obj); // External references are still being counted
            return obj;
        }
    }

    public static object ReadMetaReference(this JsonHelperReader json, bool skipHeader = false) {
        if (!skipHeader) {
            json.ReadStartMetadata(META.REF);
        }
        int id = (int) (long) json.ReadRawProperty(META.REF_ID);
        int type = (int) (long) json.ReadRawProperty(META.REF_TYPE);
        json.ReadEndMetadata();

        return json.GetReference(id, type);
    }

    public static Type ReadMetaObjectType(this JsonHelperReader json) {
        return json.ReadMetaType_(META.OBJTYPE);
    }
    public static Type ReadMetaType(this JsonHelperReader json) {
        return json.ReadMetaType_(META.TYPE);
    }
    public static Type ReadMetaType_(this JsonHelperReader json, string metaType) {
        string currentProp;
        json.ReadStartMetadata(metaType);

        Type type;

        string fullname = (string) json.ReadRawProperty(META.TYPE_FULLNAME);
        /*currentProp = json.ReadPropertyName();
        if (currentProp == META.TYPE_SPLIT) {
            json.Read(); // Drop StartArray
            string ns = (string) json.Value; json.Read();
            string name = (string) json.Value; json.Read();
            json.Read(); // Drop EndArray

            type = FindType(fullname, ns, name);
        } else*/ {
            type = FindType(fullname);
        }

        if (type == null) {
            throw new JsonReaderException("Could not find type " + fullname);
        }

        currentProp = json.ReadPropertyName();
        if (currentProp == META.TYPE_GENPARAMS) {
            type = type.MakeGenericType(json.ReadProperty<Type[]>(META.TYPE_GENPARAMS));
        }

        json.ReadEndMetadata();
        return type;
    }

    public static void ReadStartMetadata(this JsonHelperReader json, string metaType) {
        json.Read(); // Drop StartObject
        if (!CheckOnRead) {
            json.Read(); // Drop PropertyName
            json.Read(); // Drop String
        } else {
            string metaTypeR = (string) json.ReadRawProperty(META.MARKER);
            if (metaType != metaTypeR) {
                throw new JsonReaderException("Invalid meta type: Expected " + metaType + ", got " + metaTypeR);
            }
        }
    }
    public static void ReadEndMetadata(this JsonHelperReader json) {
        if (json.TokenType != JsonToken.EndObject) {
            // FAILSAFE
            // Currently only got so far that this got called at ReadMetaType_
            // with split, without genparams. This Read() caused problems.
            json.Read(); // @ EndObject
        }
        json.Read(); // Drop EndObject
    }

    public static string ReadMetaArrayData(this JsonHelperReader json, out int size) {
        json.ReadStartMetadata(META.ARRAYTYPE);
        string type = (string) json.ReadRawProperty(META.TYPE_FULLNAME);
        if (type == META.ARRAYTYPE_ARRAY) {
            size = (int) (long) json.ReadRawProperty(META.ARRAYTYPE_ARRAY_SIZE);
        } else {
            size = -1;
        }
        json.ReadEndMetadata();
        return type;
    }

    public static void ReadPropertyName(this JsonHelperReader json, string name) {
        if (CheckOnRead) {
            if (json.TokenType != JsonToken.PropertyName) {
                throw new JsonReaderException("Invalid token: Expected PropertyName, got " + json.TokenType);
            }
            if (name != (string) json.Value) {
                throw new JsonReaderException("Invalid property: Expected " + name + ", got " + json.Value);
            }
        }

        json.Read(); // Drop PropertyName
    }

    public static string ReadPropertyName(this JsonHelperReader json) {
        if (json.TokenType != JsonToken.PropertyName) {
            return null;
        }

        string name = (string) json.Value;
        json.Read(); // Drop PropertyName
        return name;
    }

    public static object ReadRawProperty(this JsonHelperReader json, string name, JsonToken type = JsonToken.Undefined) {
        if (name != null) {
            if (!CheckOnRead) {
                json.Read(); // Drop PropertyName
            } else {
                json.ReadPropertyName(name);
                if (type != JsonToken.Undefined && json.TokenType != type) {
                    throw new JsonReaderException("Invalid token: Expected " + type + ", got " + json.TokenType);
                }
            }
        }

        object value = json.Value;
        json.Read(); // Drop value
        return value;
    }

    public static T ReadProperty<T>(this JsonHelperReader json, string name) {
        return (T) json.ReadProperty(name, typeof(T));
    }
    public static object ReadProperty(this JsonHelperReader json, string name, Type type) {
        if (name != null) {
            if (!CheckOnRead) {
                json.Read(); // Drop PropertyName
            } else {
                json.ReadPropertyName(name);
            }
        }

        return json.ReadObject(type);
    }

}

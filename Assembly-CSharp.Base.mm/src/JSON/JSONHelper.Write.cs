using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public static partial class JSONHelper {

    private readonly static Dictionary<UnityEngine.Object, string> _DumpObjPathMap = new Dictionary<UnityEngine.Object, string>();
    private readonly static Dictionary<string, int> _DumpNameIdMap = new Dictionary<string, int>();

    public static JsonHelperWriter OpenWriteJSON(string path) {
        File.Delete(path);
        Stream stream = File.OpenWrite(path);
        StreamWriter text = new StreamWriter(stream);
        JsonHelperWriter json = new JsonHelperWriter(text);
        json.RelativeDir = path.Substring(0, path.Length - 5);
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

        using (JsonHelperWriter json = OpenWriteJSON(path)) {
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

        json.Push(obj);

        JSONRule rule = type.GetJSONRule();
        if (rule.GetType() == t_JSONRule) {
            
            if (obj is IList) {
                IList list = (IList) obj;
                json.WriteStartArray();
                if (type.IsArray) {
                    json.WriteMetaArrayData(META.ARRAYTYPE_ARRAY, list.Count);
                } else {
                    json.WriteMetaArrayData(META.ARRAYTYPE_LIST);
                }

                foreach (object o in list) {
                    json.Write(o);
                }
                json.WriteEndArray();
                json.Pop();
                return;
            }

            if (obj is IDictionary) {
                IDictionary dict = (IDictionary) obj;
                json.WriteStartArray();
                json.WriteMetaArrayData(META.ARRAYTYPE_MAP);
                foreach (DictionaryEntry e in dict) {
                    json.Write(e);
                }
                json.WriteEndArray();
                json.Pop();
                return;
            }

        }

        UnityEngine.Object so = (UnityEngine.Object) (
            ((object) (obj as GameObject)) ??
            ((object) (obj as ScriptableObject)) ??
            ((object) (obj as Component))
        );
        string name = so?.name;
        if (json.RootWritten && (json.DumpRelatively || SharedDir != null) && !string.IsNullOrEmpty(name) && !(obj is Transform)) {
            if (SharedDir == null && json.DumpRelatively) {
                Directory.CreateDirectory(json.RelativeDir);
                string dumppath = Path.Combine(json.RelativeDir, name + ".json");
                if (!File.Exists(dumppath)) {
                    using (JsonHelperWriter ext = OpenWriteJSON(dumppath)) {
                        ext.AddPath(json);
                        ext.RelativeDir = Path.Combine(json.RelativeDir, name);
                        ext.Write(obj);
                    }
                }
                json.WriteMetaExternal(name, META.EXTERNAL_IN_RELATIVE);

            } else if (SharedDir != null) {
                string path;
                if (_DumpObjPathMap.TryGetValue(so, out path)) {
                    json.WriteMetaExternal(path, META.EXTERNAL_IN_SHARED);
                    json.Pop();
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

                string dumppath = Path.Combine(SharedDir, path.Replace('/', Path.DirectorySeparatorChar) + ".json");
                Directory.GetParent(dumppath).Create();
                if (!File.Exists(dumppath)) {
                    using (JsonHelperWriter ext = OpenWriteJSON(dumppath)) {
                        ext.AddPath(json);
                        ext.Write(obj);
                    }
                }
                json.WriteMetaExternal(path, META.EXTERNAL_IN_SHARED);
            }
            json.Pop();
            return;
        }

        json.RootWritten = true;
        json.WriteStartObject();
        rule.WriteMetaHeader(json, obj);
        _OnBeforeSerialize(obj);
        rule.Serialize(json, obj);
        json.WriteEndObject();
        json.Pop();
    }

    public static void WriteStartMetadata(this JsonHelperWriter json, string metaType) {
        json.WriteStartObject();
        json.WriteProperty(META.MARKER, metaType);
    }
    public static void WriteEndMetadata(this JsonHelperWriter json) {
        json.WriteEndObject();
    }

    public static bool TryWriteMetaReference(this JsonHelperWriter json, object obj, bool register = false) {
        int id = json.GetReferenceID(obj);

        if (id != META.REF_NONE) {
            json.WriteStartMetadata(META.REF);

            json.WriteProperty(META.REF_ID, id);
            json.WriteProperty(META.REF_TYPE, json.GetReferenceType(id, obj));

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
    private static void WriteMetaType_(this JsonHelperWriter json, Type type, string metaType) {
        json.WriteStartMetadata(metaType);

        json.WriteProperty(META.TYPE_FULLNAME, type.FullName);
        /*string ns = type.Namespace;
        if (ns != null) {
            json.WritePropertyName(META.TYPE_SPLIT);
            json.WriteStartArray();
            json.Write(ns);
            json.Write(type.Name);
            json.WriteEndArray();
        }*/
        Type[] genparams = type.GetGenericArguments();
        if (genparams.Length != 0) {
            json.WriteProperty(META.TYPE_GENPARAMS, genparams);
        }

        json.WriteEndMetadata();
    }

    public static void WriteMetaExternal(this JsonHelperWriter json, string path, string @in = META.EXTERNAL_IN_RESOURCES) {
        json.WriteStartMetadata(META.EXTERNAL);

        if (@in != META.EXTERNAL_IN_RESOURCES) {
            json.WriteProperty(META.EXTERNAL_IN, @in);
        }
        json.WriteProperty(META.EXTERNAL_PATH, path);

        json.WriteEndMetadata();
    }

    public static void WriteMetaArrayData(this JsonHelperWriter json, string type, int size = -1) {
        json.WriteStartMetadata(META.ARRAYTYPE);
        json.WriteProperty(META.TYPE_FULLNAME, type);
        if (type == META.ARRAYTYPE_ARRAY) {
            json.WriteProperty(META.ARRAYTYPE_ARRAY_SIZE, size);
        }
        json.WriteEndMetadata();
    }

    public static void WriteProperty(this JsonHelperWriter json, string name, object obj) {
        json.WritePropertyName(name);
        json.Write(obj);
    }

    public static void Write(this JsonHelperWriter json, JSONRule rule, object obj, MemberInfo info) {
        rule.Serialize(json, obj, info);
    }

    public static void WriteAll(this JsonHelperWriter json, JSONRule rule, object obj, MemberInfo[] infos) {
        for (int i = 0; i < infos.Length; i++) {
            rule.Serialize(json, obj, infos[i]);
        }
    }

}

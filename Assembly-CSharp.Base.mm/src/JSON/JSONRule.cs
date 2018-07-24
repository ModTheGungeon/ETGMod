using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JSONRule {

    public bool ForceSerializeProperties = false;

    protected MemberInfo[] _Properties;
    protected MemberInfo[] _Fields;
    protected Dictionary<string, MemberInfo> _MemberMap = new Dictionary<string, MemberInfo>();

    public virtual JSONRule Fill(Type type) {
        List<MemberInfo> infos;

        infos = new List<MemberInfo>();
        Fill_(infos, type.GetProperties(BindingFlags.Public | BindingFlags.Instance), false);
        Fill_(infos, type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance), true);
        _Properties = infos.ToArray();

        infos = new List<MemberInfo>();
        Fill_(infos, type.GetFields(BindingFlags.Public | BindingFlags.Instance), false);
        Fill_(infos, type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance), true);
        _Fields = infos.ToArray();

        return this;
    }
    protected virtual void Fill_(List<MemberInfo> to, MemberInfo[] from, bool isPrivate) {
        for (int i = 0; i < from.Length; i++) {
            MemberInfo info = from[i];
            if (!CanSerialize(info, isPrivate)) {
                continue;
            }
            to.Add(info);
            _MemberMap[info.Name] = info;
        }
    }

    public virtual bool CanSerialize(MemberInfo info, bool isPrivate) {
        if (info is PropertyInfo) {
            PropertyInfo pi = (PropertyInfo) info;
            if (!pi.CanRead || !pi.CanWrite) {
                return false;
            }
        }

        if (isPrivate && info.GetCustomAttributes(typeof(SerializeField), true).Length == 0) {
            return false;
        }
        if (!isPrivate && info.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length != 0) {
            return false;
        }

        return true;
    }

    public virtual void Serialize(JsonHelperWriter json, object obj, MemberInfo info) {
        json.WritePropertyName(info.Name);
        json.Write(ReflectionHelper.GetValue(info, obj));
    }

    public virtual void WriteMetaHeader(JsonHelperWriter json, object obj) {
        json.WritePropertyName(JSONHelper.META.PROP);
        json.WriteMetaObjectType(obj);
    }

    public virtual void Serialize(JsonHelperWriter json, object obj) {
        if (obj is UnityEngine.Object && !(obj is Component)) {
            json.WriteProperty("name", ((UnityEngine.Object) obj).name);
        }

        if (ForceSerializeProperties) {
            for (int i = 0; i < _Properties.Length; i++) {
                Serialize(json, obj, _Properties[i]);
            }
        }

        for (int i = 0; i < _Fields.Length; i++) {
            Serialize(json, obj, _Fields[i]);
        }
    }

    public virtual object New(JsonHelperReader json, Type type) {
        try {
            return ReflectionHelper.Instantiate(type);
        } catch (Exception e) {
            throw new JsonReaderException("Could not instantiate type " + type.FullName + "!", e);
        }
    }

    public virtual void Deserialize(JsonHelperReader json, object obj, string prop) {
        MemberInfo info;
        if (!_MemberMap.TryGetValue(prop, out info)) {
            // Forcibly throw here - can't parse the following data anymore
            throw new JsonReaderException("Invalid property " + prop + "!");
        }

        object value = json.ReadObject(info.GetValueType());
        if (obj == null) {
            // Just drop the value.
            return;
        }
        ReflectionHelper.SetValue(info, obj, value);
    }

    public virtual string ReadMetaHeader(JsonHelperReader json, ref Type type) {
        string metaProp = json.ReadPropertyName();
        if (metaProp != JSONHelper.META.PROP) {
            return metaProp;
        }

        Type typeR = json.ReadMetaObjectType();
        if (JSONHelper.CheckOnRead && !type.IsAssignableFrom(typeR)) {
            throw new JsonReaderException("Type mismatch! Expected " + type.FullName + ", got " + typeR.FullName);
        }
        type = typeR;

        return null;
    }

    public virtual object Deserialize(JsonHelperReader json, object obj) {
        if (obj is UnityEngine.Object && !(obj is Component)) {
            ((UnityEngine.Object) obj).name = (string) json.ReadRawProperty("name");
        }

        while (json.TokenType != JsonToken.EndObject) {
            Deserialize(json, obj, json.ReadPropertyName());
        }

        return obj;
    }

}

public class JSONRule<T> : JSONRule {

    protected Type _T = typeof(T);

    public override JSONRule Fill(Type type) {
        _T = type;
        return base.Fill(type);
    }

}

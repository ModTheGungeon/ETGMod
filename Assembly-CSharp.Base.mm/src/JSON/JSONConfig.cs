using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JSONConfig {

    public bool ForceSerializeProperties = false;

    protected MemberInfo[] _Properties;
    protected MemberInfo[] _Fields;

    public virtual JSONConfig Fill(Type type) {
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

    public virtual object Deserialize(JToken token, MemberInfo info, bool isPrivate) {
        return null; // TODO
    }

    public virtual void WriteMetaHeader(JsonHelperWriter json, object obj) {
        json.WritePropertyName(JSONHelper.META.PROP);
        json.WriteMetaObjectType(obj);
    }

    public virtual void Serialize(JsonHelperWriter json, object obj) {
        Type type = obj.GetType();
        json.WriteStartObject();
        WriteMetaHeader(json, obj);

        if (obj is UnityEngine.Object) {
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

        json.WriteEndObject();
    }

}

public class JSONConfig<T> : JSONConfig {

    protected Type _T = typeof(T);

}

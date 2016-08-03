using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JSONConfig {

    public bool ForceSerializeProperties = false;

    public virtual bool CanSerialize(object obj, MemberInfo info, bool isPrivate) {
        if (info is PropertyInfo) {
            PropertyInfo pi = (PropertyInfo) info;
            if (!pi.CanRead || !pi.CanWrite) {
                return false;
            }
            if (!ForceSerializeProperties) {
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

    public virtual void Serialize(JsonHelperWriter json, object obj, MemberInfo info, bool isPrivate) {
        if (!CanSerialize(obj, info, isPrivate)) {
            return;
        }

        json.WritePropertyName(info.Name);
        json.Write(ReflectionHelper.GetValue(info, obj));
    }

    public virtual object Deserialize(JToken token, MemberInfo info, bool isPrivate) {
        return null; // TODO
    }

    public virtual void Serialize(JsonHelperWriter json, object obj) {
        Type type = obj.GetType();
        json.WriteStartObject();

        if (obj is UnityEngine.Object) {
            json.WriteProperty("name", ((UnityEngine.Object) obj).name);
        }

        json.WriteAll(this, obj, type.GetProperties(BindingFlags.Public    | BindingFlags.Instance));
        json.WriteAll(this, obj, type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance), true);

        json.WriteAll(this, obj, type.GetFields(BindingFlags.Public    | BindingFlags.Instance));
        json.WriteAll(this, obj, type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance), true);

        json.WriteEndObject();
    }

}

public class JSONConfig<T> : JSONConfig {

    protected Type t = typeof(T);

}

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

    public virtual JToken Serialize(object obj, MemberInfo info, bool isPrivate) {
        if (!CanSerialize(obj, info, isPrivate)) {
            return null;
        }

        return JToken.FromObject(ReflectionHelper.GetValue(info, obj));
    }

    public virtual void Serialize(object obj, MemberInfo info, JsonHelperWriter json, bool isPrivate) {
        if (!CanSerialize(obj, info, isPrivate)) {
            return;
        }

        json.WritePropertyName(info.Name);
        ReflectionHelper.GetValue(info, obj).WriteJSON(json);
    }

    public virtual object Deserialize(JToken token, MemberInfo info, bool isPrivate) {
        return null; // TODO
    }

    public virtual JToken Serialize(object obj) {
        Type type = obj.GetType();
        JObject json = new JObject();

        json.AddAll(this, obj, type.GetProperties(BindingFlags.Public    | BindingFlags.Instance));
        json.AddAll(this, obj, type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance), true);

        json.AddAll(this, obj, type.GetFields(BindingFlags.Public    | BindingFlags.Instance));
        json.AddAll(this, obj, type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance), true);

        return json;
    }

    public virtual void Serialize(object obj, JsonHelperWriter json) {
        Type type = obj.GetType();
        json.WriteStartObject();

        json.AddAll(this, obj, type.GetProperties(BindingFlags.Public    | BindingFlags.Instance));
        json.AddAll(this, obj, type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance), true);

        json.AddAll(this, obj, type.GetFields(BindingFlags.Public    | BindingFlags.Instance));
        json.AddAll(this, obj, type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance), true);

        json.WriteEndObject();
    }

}

public class JSONConfig<T> : JSONConfig {

    protected Type t = typeof(T);

    public JToken SerializeAs(T obj, MemberInfo info, bool isPrivate) {
        return Serialize(obj, info, isPrivate);
    }

    public T DeserializeAs(JToken token, MemberInfo info, bool isPrivate) {
        return (T) Deserialize(token, info, isPrivate);
    }

    public JToken SerializeAs(T obj) {
        return Serialize(obj);
    }

}

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONComponentBaseRule<T> : JSONRule<T> {

    public override void Serialize(JsonHelperWriter json, object obj) {
        Component c = (Component) obj;

        if (json.At(-2) is GameObject) {
            json.WriteProperty(JSONHelper.META.TYPE, JSONHelper.META.COMPONENTTYPE_DEFINITION);
            Serialize_(json, obj);
        } else {
            json.WriteProperty(JSONHelper.META.TYPE, JSONHelper.META.COMPONENTTYPE_REFERENCE);
            json.WriteProperty("name", c.gameObject.name);
            json.WriteProperty("path", c.transform.GetPath());
        }
    }
    protected virtual void Serialize_(JsonHelperWriter json, object obj) {
        base.Serialize(json, obj);
    }

    public override object New(JsonHelperReader json, Type type) {
        return null;
    }

    public override object Deserialize(JsonHelperReader json, object obj) {
        Component c = (Component) obj;

        string componentType = (string) json.ReadRawProperty(JSONHelper.META.TYPE);

        if (componentType == JSONHelper.META.COMPONENTTYPE_DEFINITION) {
            return Deserialize_(json, c);
        }

        if (componentType == JSONHelper.META.COMPONENTTYPE_REFERENCE) {
            string name = (string) json.ReadRawProperty("name");
            string path = (string) json.ReadRawProperty("path");
            GameObject holding = null;
            object holdingObj;
            if (json.Global.TryGetValue(path, out holdingObj)) {
                holding = holdingObj as GameObject;
            } if (json.Global.TryGetValue(name, out holdingObj)) {
                holding = holdingObj as GameObject;
            } else {
                holding = GameObject.Find(path);
            }
            if (holding == null) {
                Console.WriteLine("WARNING: Could not find GameObject " + path + " holding " + _T.Name + "!");
                return null;
            }
            return holding.GetComponent(_T);
        }

        throw new JsonReaderException("Unknown component type: " + componentType);
    }
    protected virtual object Deserialize_(JsonHelperReader json, object obj) {
        return base.Deserialize(json, obj);
    }

}

public class JSONComponentRule : JSONComponentBaseRule<Component> {

}

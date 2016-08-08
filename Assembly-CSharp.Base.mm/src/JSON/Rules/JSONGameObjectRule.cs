using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONGameObjectRule : JSONRule<GameObject> {

    // Add components (types)
    // Add children
    // Fill components (from bottom to top)

    // Issue:
    // Child 1 refers to component A of 

    public override void Serialize(JsonHelperWriter json, object obj) {
        GameObject go = (GameObject) obj;
        Component[] components = go.GetComponents<Component>();

        json.WriteProperty("name", go.name);

        json.WriteProperty("tag", go.tag);
        json.WriteProperty("layer", go.layer);
        json.WriteProperty("activeSelf", go.activeSelf);

        json.WritePropertyName("componentTypes");
        json.WriteStartArray();
        for (int i = 0; i < components.Length; i++) {
            json.Write(components[i].GetType());
        }
        json.WriteEndArray();

        Transform transform = go.transform;
        int children = transform.childCount;
        json.WritePropertyName("children");
        json.WriteStartArray();
        for (int i = 0; i < children; i++) {
            json.Write(transform.GetChild(i).gameObject);
        }
        json.WriteEndArray();

        json.WritePropertyName("components");
        json.WriteStartArray();
        for (int i = 0; i < components.Length; i++) {
            json.Write(components[i]);
        }
        json.WriteEndArray();
    }

    public override object Deserialize(JsonHelperReader json, object obj) {
        GameObject go = (GameObject) obj;

        go.name = (string) json.ReadRawProperty("name");

        json.Global[go.name] = go;
        json.Global[go.transform.GetPath()] = go;
        
        go.tag = (string) json.ReadRawProperty("tag");
        go.layer = (int) (long) json.ReadRawProperty("layer");
        bool active = (bool) json.ReadRawProperty("activeSelf");
        go.SetActive(false);

        json.ReadPropertyName("componentTypes");
        json.Read(); // Drop StartArray
        while (json.TokenType != JsonToken.EndArray) {
            Type componentType = json.ReadObject<Type>();
            if (go.GetComponent(componentType) == null) {
                go.AddComponent(componentType);
            }
        }
        json.Read(); // Drop EndArray

        json.ReadPropertyName("children");
        Transform transform = go.transform;
        json.Read(); // Drop StartArray
        while (json.TokenType != JsonToken.EndArray) {
            GameObject child = new GameObject();
            child.transform.SetParent(transform);
            json.FillObject(child);
        }
        json.Read(); // Drop EndArray

        json.ReadPropertyName("components");
        json.Read(); // Drop StartArray
        while (json.TokenType != JsonToken.EndArray) {
            FillComponent(json, go);
        }
        json.Read(); // Drop EndArray

        go.SetActive(active);
        return go;
    }

    public void FillComponent(JsonHelperReader json, GameObject go) {
        json.Read(); // Drop StartObject
        string metaProp = json.ReadPropertyName();

        if (metaProp == JSONHelper.META.PROP) {
            Type componentType = json.ReadMetaObjectType();
            json.FillObject(go.GetComponent(componentType), componentType, null, true);
            return;
        } 

        if (metaProp == JSONHelper.META.MARKER) {
            // Only meta currently allowed here is an external reference.
            // References back aren't allowed as one would need to read the same .json
            // and read / skip until the ref ID required is reached, then fill the component.
            // But doing even that only creates a shallow clone...
            // TODO do dat if it should be a shallow clone anyway.
            json.Read(); // Drop value
            using (JsonHelperReader ext = json.OpenMetaExternal(true)) {
                ext.Read(); // Go to Start
                FillComponent(ext, go);
            }
        }
    }

}

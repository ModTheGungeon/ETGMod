using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONGameObjectRule : JSONRule<GameObject> {

    public override void Serialize(JsonHelperWriter json, object obj) {
        GameObject go = (GameObject) obj;

        SerializeMain(json, go);
        SerializeComponentTypes(json, go);
        SerializeHierarchy(json, go);

        SerializeComponentData(json, go);
    }

    public void SerializeMain(JsonHelperWriter json, GameObject go) {
        json.WriteProperty("name", go.name);

        json.WriteProperty("tag", go.tag);
        json.WriteProperty("layer", go.layer);
        json.WriteProperty("activeSelf", go.activeSelf);
    }

    public void SerializeComponentTypes(JsonHelperWriter json, GameObject go) {
        Component[] components = go.GetComponents<Component>();

        json.WritePropertyName("componentTypes");
        json.WriteStartArray();
        for (int i = 0; i < components.Length; i++) {
            json.Write(components[i].GetType());
        }
        json.WriteEndArray();
    }

    public void SerializeHierarchy(JsonHelperWriter json, GameObject go) {
        Transform transform = go.transform;
        int children = transform.childCount;

        json.WritePropertyName("hierarchy");
        json.WriteStartArray();
        for (int i = 0; i < children; i++) {
            GameObject child = transform.GetChild(i).gameObject;
            json.WriteStartObject();
            SerializeMain(json, child);
            SerializeComponentTypes(json, child);
            SerializeHierarchy(json, child);
            json.WriteEndObject();
        }
        json.WriteEndArray();
    }

    public void SerializeComponentData(JsonHelperWriter json, GameObject go, bool property = true) {
        Component[] components = go.GetComponents<Component>();

        Transform transform = go.transform;
        int children = transform.childCount;

        if (property) {
            json.WritePropertyName("componentData");
        }
        json.WriteStartArray();

        json.WriteValue(components.Length);
        for (int i = 0; i < components.Length; i++) {
            json.Write(components[i]);
        }

        json.WriteStartArray();
        for (int i = 0; i < children; i++) {
            GameObject child = transform.GetChild(i).gameObject;
            SerializeComponentData(json, child, false);
        }
        json.WriteEndArray();

        json.WriteEndArray();
    }

    public override object Deserialize(JsonHelperReader json, object obj) {
        GameObject go = (GameObject) obj;

        bool active = DeserializeMain(json, go);
        go.SetActive(false);
        DeserializeComponentTypes(json, go);
        DeserializeHierarchy(json, go);

        DeserializeComponentData(json, go);

        go.SetActive(active);
        return go;
    }

    public bool DeserializeMain(JsonHelperReader json, GameObject go) {
        go.name = (string) json.ReadRawProperty("name");

        json.Global[go.name] = go;
        json.Global[go.transform.GetPath()] = go;

        go.tag = (string) json.ReadRawProperty("tag");
        go.layer = (int) (long) json.ReadRawProperty("layer");
        return (bool) json.ReadRawProperty("activeSelf");
    }

    public void DeserializeComponentTypes(JsonHelperReader json, GameObject go) {
        json.ReadPropertyName("componentTypes");
        json.Read(); // Drop StartArray
        while (json.TokenType != JsonToken.EndArray) {
            Type componentType = json.ReadObject<Type>();
            if (go.GetComponent(componentType) == null) {
                go.AddComponent(componentType);
            }
        }
        json.Read(); // Drop EndArray
    }

    public void DeserializeHierarchy(JsonHelperReader json, GameObject go) {
        json.ReadPropertyName("hierarchy");
        Transform transform = go.transform;
        json.Read(); // Drop StartArray
        while (json.TokenType != JsonToken.EndArray) {
            GameObject child = new GameObject();
            child.transform.SetParent(transform);
            json.Read(); // Drop StartObject
            DeserializeMain(json, child);
            DeserializeComponentTypes(json, child);
            DeserializeHierarchy(json, child);
            json.Read(); // Drop EndObject
        }
        json.Read(); // Drop EndArray
    }

    public void DeserializeComponentData(JsonHelperReader json, GameObject go, bool property = true) {
        Transform transform = go.transform;
        int children = transform.childCount;

        if (property) {
            json.ReadPropertyName("componentData");
        }
        json.Read(); // Drop StartArray

        int components = (int) (long) json.Value; json.Read();
        for (int i = 0; i < components; i++) {
            FillComponent(json, go);
        }

        json.Read(); // Drop StartArray
        int ii = -1;
        while (json.TokenType != JsonToken.EndArray) {
            GameObject child = transform.GetChild(++ii).gameObject;
            DeserializeComponentData(json, child, false);
        }
        json.Read(); // Drop EndArray

        json.Read(); // Drop EndArray
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

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONGameObjectConfig : JSONConfig<GameObject> {

    public override void Serialize(JsonHelperWriter json, object obj) {
        GameObject go = (GameObject) obj;
        json.WriteStartObject();

        json.WriteProperty("name", go.name);
        json.WriteProperty("tag", go.tag);
        json.WriteProperty("layer", go.layer);
        json.WriteProperty("activeSelf", go.activeSelf);

        json.WritePropertyName("components");
        json.WriteStartArray();
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++) {
            Component component = components[i];
            json.WriteStartObject();
            json.WriteProperty("type", component.GetType());
            json.WritePropertyName("value");
            json.Write(component);
            json.WriteEndObject();
        }
        json.WriteEndArray();

        json.WritePropertyName("children");
        json.WriteStartArray();
        Transform transform = go.transform;
        int children = transform.childCount;
        for (int i = 0; i < children; i++) {
            json.Write(transform.GetChild(i).gameObject);
        }
        json.WriteEndArray();

        json.WriteEndObject();
    }

}

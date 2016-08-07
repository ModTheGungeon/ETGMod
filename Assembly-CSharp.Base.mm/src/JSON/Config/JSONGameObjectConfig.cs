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
        WriteMetaHeader(json, obj);

        json.WriteProperty("name", go.name);
        json.WriteProperty("tag", go.tag);
        json.WriteProperty("layer", go.layer);
        json.WriteProperty("activeSelf", go.activeSelf);

        json.WriteProperty("components", go.GetComponents<Component>());

        Transform transform = go.transform;
        int children = transform.childCount;
        if (children != 0) {
            json.WritePropertyName("children");
            json.WriteStartArray();
            for (int i = 0; i < children; i++) {
                json.Write(transform.GetChild(i).gameObject);
            }
            json.WriteEndArray();
        }

        json.WriteEndObject();
    }

}

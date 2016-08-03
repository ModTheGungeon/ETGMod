using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONTransformConfig : JSONConfig<Transform> {

    public override void Serialize(JsonHelperWriter json, object obj) {
        Transform t = (Transform) obj;
        json.WriteStartObject();
        json.WriteProperty("type", t.GetType());
        json.WritePropertyName("value");
        json.WriteStartObject();

        // TODO all the (2) various types of Transforms
        json.WriteProperty("position", t.position);
        json.WriteProperty("rotation", t.rotation);
        json.WriteProperty("localScale", t.localScale);

        json.WriteEndObject();
        json.WriteEndObject();
    }

}

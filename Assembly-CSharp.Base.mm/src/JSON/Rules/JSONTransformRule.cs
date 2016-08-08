using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONTransformRule : JSONComponentBaseRule<Transform> {

    protected override void Serialize_(JsonHelperWriter json, object obj) {
        Transform t = (Transform) obj;

        // TODO all the (2) various types of Transforms
        json.WriteProperty("position", t.position);
        json.WriteProperty("rotation", t.rotation);
        json.WriteProperty("localScale", t.localScale);
    }

    protected override object Deserialize_(JsonHelperReader json, object obj) {
        if (obj == null) {
            // TODO all the (2) various types of Transforms
            /*Drop */ json.ReadProperty<Vector3>("position");
            /*Drop */ json.ReadProperty<Quaternion>("rotation");
            /*Drop */ json.ReadProperty<Vector3>("localScale");
            return null;
        }

        Transform t = (Transform) obj;

        // TODO all the (2) various types of Transforms
        t.position = json.ReadProperty<Vector3>("position");
        t.rotation = json.ReadProperty<Quaternion>("rotation");
        t.localScale = json.ReadProperty<Vector3>("localScale");

        return t;
    }

}

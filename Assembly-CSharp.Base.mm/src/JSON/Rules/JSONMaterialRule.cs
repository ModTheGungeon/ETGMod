using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JSONMaterialRule : JSONRule<Material> {

    public override void Serialize(JsonHelperWriter json, object obj) {
        json.WriteProperty(JSONHelper.META.UNSUPPORTED, JSONHelper.META.UNSUPPORTED_USE_EXTERNAL);
    }

    public override object New(JsonHelperReader json, Type type) {
        return null;
    }

    public override object Deserialize(JsonHelperReader json, object obj) {
        json.Read(); // Drop PropertyName
        json.Read(); // Drop String
        return obj;
    }

}

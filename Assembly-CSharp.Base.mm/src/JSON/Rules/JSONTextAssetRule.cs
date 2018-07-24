using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONTextAssetRule : JSONRule<TextAsset> {

    public override void Serialize(JsonHelperWriter json, object obj) {
        try {
            json.WriteProperty("text", ((TextAsset) obj).text);
        } catch {
            json.WriteProperty("text", null);
        }
    }

    public override object Deserialize(JsonHelperReader json, object obj) {
        ((patch_TextAsset) obj).textOverride = (string) json.ReadRawProperty("text");
        return obj;
    }

}

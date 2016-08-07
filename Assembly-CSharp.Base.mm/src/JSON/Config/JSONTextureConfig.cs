using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JSONTextureBaseConfig<T> : JSONConfig<T> {

    public override void Serialize(JsonHelperWriter json, object obj) {
        json.WriteStartObject();
        WriteMetaHeader(json, obj);

        json.WriteProperty("name", ((Texture) obj).name);
        json.WriteProperty("type", typeof(T).Name);

        json.WriteEndObject();
    }

}

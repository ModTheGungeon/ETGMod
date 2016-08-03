using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JSONTextureBaseConfig<T> : JSONConfig<T> {

    public override JToken Serialize(object obj) {
        return new JObject() {
            {"name", ((Texture) obj).name.ToJSON()},
            {"type", typeof(T).Name.ToJSON()}
        };
    }

    public override void Serialize(object obj, JsonHelperWriter json) {
        json.WriteStartObject();

        json.WritePropertyName("name");
        ((Texture) obj).name.WriteJSON(json);

        json.WritePropertyName("type");
        typeof(T).Name.WriteJSON(json);

        json.WriteEndObject();
    }

}

public class JSONTextureConfig : JSONTextureBaseConfig<Texture> { }
public class JSONTexture2DConfig : JSONTextureBaseConfig<Texture2D> { }
public class JSONTexture3DConfig : JSONTextureBaseConfig<Texture3D> { }

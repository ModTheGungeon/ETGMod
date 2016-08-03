using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONComponentConfig : JSONConfig<Component> {

    public override void Serialize(JsonHelperWriter json, object obj) {
        Component component = (Component) obj;
        json.WriteStartObject();

        json.WriteProperty("type", component.GetType());
        json.WritePropertyName("value");
        base.Serialize(json, obj);

        json.WriteEndObject();
    }

}

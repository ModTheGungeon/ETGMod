using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONDictionaryEntryConfig : JSONConfig<DictionaryEntry> {

    public override JToken Serialize(object obj) {
        DictionaryEntry entry = (DictionaryEntry) obj;
        return new JObject() {
            {"key", entry.Key.ToJSON()},
            {"value", entry.Value.ToJSON()}
        };
    }

    public override void Serialize(object obj, JsonHelperWriter json) {
        DictionaryEntry entry = (DictionaryEntry) obj;
        json.WriteStartObject();

        json.WritePropertyName("key");
        entry.Key.WriteJSON(json);

        json.WritePropertyName("value");
        entry.Value.WriteJSON(json);

        json.WriteEndObject();
    }

}

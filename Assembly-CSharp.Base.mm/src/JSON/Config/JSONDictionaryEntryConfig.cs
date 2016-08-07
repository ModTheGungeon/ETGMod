using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONDictionaryEntryConfig : JSONConfig<DictionaryEntry> {

    public override void Serialize(JsonHelperWriter json, object obj) {
        DictionaryEntry entry = (DictionaryEntry) obj;
        json.WriteStartObject();

        json.WriteProperty("key", entry.Key);
        json.WriteProperty("value", entry.Value);

        json.WriteEndObject();
    }

}

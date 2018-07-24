using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONDictionaryEntryRule : JSONRule<DictionaryEntry> {

    public override void WriteMetaHeader(JsonHelperWriter json, object obj) {
    }

    public override void Serialize(JsonHelperWriter json, object obj) {
        DictionaryEntry entry = (DictionaryEntry) obj;
        json.WriteProperty("key", entry.Key);
        json.WriteProperty("value", entry.Value);
    }

    public override object New(JsonHelperReader json, Type type) {
        return null;
    }

    public override string ReadMetaHeader(JsonHelperReader json, ref Type type) {
        return null;
    }

    public override object Deserialize(JsonHelperReader json, object obj) {
        return new DictionaryEntry(
            json.ReadRawProperty("key"),
            json.ReadRawProperty("value")
        );
    }

}

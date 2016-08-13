using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONValueTypeBaseRule<T> : JSONRule<T> {

    public override void WriteMetaHeader(JsonHelperWriter json, object obj) {
        // json.WriteProperty(JSONHelper.META.PROP, JSONHelper.META.VALUETYPE);
    }

    public override string ReadMetaHeader(JsonHelperReader json, ref Type type) {
        /*
        string metaProp = json.ReadPropertyName();
        if (metaProp != JSONHelper.META.PROP) {
            return metaProp;
        }
        string valuetype = (string) json.Value;
        json.Read(); // Drop String
        if (JSONHelper.CheckOnRead && valuetype != JSONHelper.META.VALUETYPE) {
            throw new JsonReaderException("Type mismatch! Expected " + JSONHelper.META.VALUETYPE + ", got " + valuetype);
        }
        */
        return null;
    }

}

public class JSONValueTypeRule : JSONValueTypeBaseRule<ValueType> {

}

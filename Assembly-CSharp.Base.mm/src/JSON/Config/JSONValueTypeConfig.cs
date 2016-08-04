using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONValueTypeConfig : JSONConfig<ValueType> {

    public override void WriteMetaHeader(JsonHelperWriter json, object obj) {
        json.WriteProperty(JSONHelper.META.PROP, JSONHelper.META.VALUETYPE);
    }

}

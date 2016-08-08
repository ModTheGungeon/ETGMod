using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONAttachPointDataRule : JSONRule<AttachPointData> {

    public override object New(JsonHelperReader json, Type type) {
        return new AttachPointData(null);
    }

}

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public class JSONBagelColliderDataRule : JSONRule<BagelColliderData> {

    public override object New(JsonHelperReader json, Type type) {
        return new BagelColliderData(null);
    }

}

public class JSONBagelColliderRule : JSONRule<BagelCollider> {

    public override object New(JsonHelperReader json, Type type) {
        return new BagelCollider(0, 0, null);
    }

}

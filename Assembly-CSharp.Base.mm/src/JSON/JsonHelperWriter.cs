using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Runtime.InteropServices;

public class JsonHelperWriter : JsonTextWriter {

    private List<object> _RefList = new List<object>();
    private Dictionary<object, int> _RefIdMap = new Dictionary<object, int>(4096);

    public JsonHelperWriter(TextWriter writer)
        : base(writer) {
    }

    public int GetReferenceID(object obj) {
        int id;
        if (!_RefIdMap.TryGetValue(obj, out id)) {
            return JSONHelper.NOREF;
        }
        return id;
    }
    public int GetReferenceType(int id, object a) {
        object b = _RefList[id];
        if (ReferenceEquals(a, b)) {
            return JSONHelper.META_REFTYPE_SAMEREF;
        }
        return JSONHelper.META_REFTYPE_EQUAL;
    }

    public int RegisterReference(object obj) {
        int id = _RefList.Count;
        _RefList.Add(obj);
        _RefIdMap[obj] = id;
        return id;
    }

}
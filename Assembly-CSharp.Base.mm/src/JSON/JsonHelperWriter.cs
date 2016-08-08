using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

public class JsonHelperWriter : JsonTextWriter {

    public bool RootWritten = false;

    private readonly List<object> _RefList = new List<object>(512);
    private readonly Dictionary<object, int> _RefIdMap = new Dictionary<object, int>(512);

    private readonly List<object> _ObjPath = new List<object>(512);

    public string RelativeDir;
    public bool DumpRelatively = false;

    public JsonHelperWriter(TextWriter writer)
        : base(writer) {
    }

    public int GetReferenceID(object obj) {
        if (obj is Component) {
            return JSONHelper.META.REF_NONE;
        }
        int id;
        if (!_RefIdMap.TryGetValue(obj, out id)) {
            return JSONHelper.META.REF_NONE;
        }
        return id;
    }
    public int GetReferenceType(int id, object a) {
        if (id == JSONHelper.META.REF_NONE || a is Component) {
            return JSONHelper.META.REF_NONE;
        }
        object b = _RefList[id];
        if (ReferenceEquals(a, b)) {
            return JSONHelper.META.REF_TYPE_SAMEREF;
        }
        return JSONHelper.META.REF_TYPE_EQUAL;
    }

    public int RegisterReference(object obj) {
        // This breaks stuff. FIXME FIXME FIXME
        /*if (obj is Component) {
            return JSONHelper.META.REF_NONE;
        }*/
        int id = _RefList.Count;
        _RefList.Add(obj);
        _RefIdMap[obj] = id;
        return id;
    }

    public void AddPath(JsonHelperWriter json) {
        _ObjPath.AddRange(json._ObjPath);
    }
    public void Push(object obj) {
        _ObjPath.Add(obj);
    }
    public void Pop() {
        _ObjPath.RemoveAt(_ObjPath.Count - 1);
    }
    public object At(int pos, bool throwIfOOB = false) {
        if (pos < 0) {
            pos += _ObjPath.Count;
        }

        if (pos < 0 || _ObjPath.Count <= pos) {
            if (throwIfOOB) {
                StringBuilder message = new StringBuilder();
                message.Append("Requested object path position out of bounds!\nPosition: ").Append(pos)
                       .Append("\nObject path:");
                for (int i = 0; i < _ObjPath.Count; i++) {
                    message.AppendLine().Append(i).Append(": ").Append(_ObjPath[i]);
                }
                throw new JsonReaderException(message.ToString());
            }
            return null;
        }

        return _ObjPath[pos];
    }

}
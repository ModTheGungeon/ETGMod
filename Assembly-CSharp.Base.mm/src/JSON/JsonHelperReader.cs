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

public class JsonHelperReader : JsonTextReader {

    public readonly List<object> _RefList = new List<object>(512);

    public readonly Dictionary<string, object> Global = new Dictionary<string, object>();

    public string RelativeDir;

    public JsonHelperReader(TextReader reader)
        : base(reader) {
    }

    public object GetReference(int id, int type) {
        if (id < 0 || _RefList.Count <= id) {
            StringBuilder message = new StringBuilder();
            message.Append("Reference ID out of bounds!\nID: ").Append(id)
                   .Append("\nRegistered references:");
            for (int i = 0; i < _RefList.Count; i++) {
                message.AppendLine().Append(i).Append(": ").Append(_RefList[i]);
            }
            throw new JsonReaderException(message.ToString());
        }

        if (type == JSONHelper.META.REF_TYPE_SAMEREF) {
            return _RefList[id];
        }

        if (type == JSONHelper.META.REF_TYPE_EQUAL) {
            // TODO create shallow copy
            return _RefList[id];
        }

        return null;
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
        return id;
    }

    public void UpdateReference(object obj, int id) {
        if (id == JSONHelper.META.REF_NONE || obj is Component) {
            return;
        }
        _RefList[id] = obj;
    }

    /*
    public override bool Read() {
        bool value = base.Read();
        if (Value == null) {
            Console.WriteLine("JSON Read(): " + TokenType);
        } else {
            Console.WriteLine("JSON Read(): " + TokenType + ": " + Value);
        }
        Console.WriteLine(Environment.StackTrace);
        return value;
    }
    */

}
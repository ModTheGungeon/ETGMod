using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

// Commonly used; Instead of reflection, just use prepared rules to save some spare cycles.

public class JSONVector2Rule : JSONValueTypeBaseRule<Vector2> {
    public override void Serialize(JsonHelperWriter json, object obj) {
        Vector2 v = (Vector2) obj;
        json.WriteProperty("x", v.x);
        json.WriteProperty("y", v.y);
    }
    public override object New(JsonHelperReader json, Type type) {
        return new Vector2();
    }
    public override object Deserialize(JsonHelperReader json, object obj) {
        Vector2 v = (Vector2) obj;
        v.Set(
            (float) (double) json.ReadRawProperty("x"),
            (float) (double) json.ReadRawProperty("y")
        );
        return v;
    }
}

public class JSONVector3Rule : JSONValueTypeBaseRule<Vector3> {
    public override void Serialize(JsonHelperWriter json, object obj) {
        Vector3 v = (Vector3) obj;
        json.WriteProperty("x", v.x);
        json.WriteProperty("y", v.y);
        json.WriteProperty("z", v.z);
    }
    public override object New(JsonHelperReader json, Type type) {
        return new Vector3();
    }
    public override object Deserialize(JsonHelperReader json, object obj) {
        Vector3 v = (Vector3) obj;
        v.Set(
            (float) (double) json.ReadRawProperty("x"),
            (float) (double) json.ReadRawProperty("y"),
            (float) (double) json.ReadRawProperty("z")
        );
        return v;
    }
}

public class JSONVector4Rule : JSONValueTypeBaseRule<Vector4> {
    public override void Serialize(JsonHelperWriter json, object obj) {
        Vector4 v = (Vector4) obj;
        json.WriteProperty("x", v.x);
        json.WriteProperty("y", v.y);
        json.WriteProperty("z", v.z);
        json.WriteProperty("w", v.w);
    }
    public override object New(JsonHelperReader json, Type type) {
        return new Vector4();
    }
    public override object Deserialize(JsonHelperReader json, object obj) {
        Vector4 v = (Vector4) obj;
        v.Set(
            (float) (double) json.ReadRawProperty("x"),
            (float) (double) json.ReadRawProperty("y"),
            (float) (double) json.ReadRawProperty("z"),
            (float) (double) json.ReadRawProperty("w")
        );
        return v;
    }
}

public class JSONQuaternionRule : JSONValueTypeBaseRule<Quaternion> {
    public override void Serialize(JsonHelperWriter json, object obj) {
        Quaternion q = (Quaternion) obj;
        json.WriteProperty("x", q.x);
        json.WriteProperty("y", q.y);
        json.WriteProperty("z", q.z);
        json.WriteProperty("w", q.w);
    }
    public override object New(JsonHelperReader json, Type type) {
        return new Quaternion();
    }
    public override object Deserialize(JsonHelperReader json, object obj) {
        Quaternion q = (Quaternion) obj;
        q.Set(
            (float) (double) json.ReadRawProperty("x"),
            (float) (double) json.ReadRawProperty("y"),
            (float) (double) json.ReadRawProperty("z"),
            (float) (double) json.ReadRawProperty("w")
        );
        return q;
    }
}

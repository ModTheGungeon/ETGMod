using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

public static partial class JSONHelper {

    private readonly static Assembly _Asm;
    private readonly static Type[] _RuleTypes;
    private readonly static Type t_JSONRule = typeof(JSONRule);
    private readonly static Type t_string = typeof(string);
    private readonly static Type t_byte_a = typeof(byte[]);
    private readonly static Type t_Type = typeof(Type);
    private readonly static object[] a_object_0 = new object[0];

    private readonly static Dictionary<string, Type> _TypeCache = new Dictionary<string, Type>();

    public static bool LOG = false;

    public readonly static Dictionary<Type, JSONRule> Rules = new Dictionary<Type, JSONRule>();
    public readonly static JSONRule RuleValueType = new JSONValueTypeRule();

    public static string SharedDir;

    static JSONHelper() {
        _Asm = Assembly.GetExecutingAssembly();
        _RuleTypes = _Asm.GetExportedTypes();

        List<Type> _Types_Assignable = new List<Type>(_RuleTypes.Length);
        for (int i = 0; i < _RuleTypes.Length; i++) {
            Type t = _RuleTypes[i];
            if (!t_JSONRule.IsAssignableFrom(t)) {
                continue;
            }
            _Types_Assignable.Add(t);
        }
        _RuleTypes = _Types_Assignable.ToArray();
    }

    public static JSONRule GetJSONRule(this object obj) {
        return obj.GetType().GetJSONRule();
    }
    public static JSONRule GetJSONRule(this Type type_) {
        Type type = type_;
        JSONRule config;
        if (Rules.TryGetValue(type_, out config)) {
            return config;
        }

        while (type != null) {
            for (int i = 0; i < _RuleTypes.Length; i++) {
                Type t = _RuleTypes[i];
                Type bi = t;
                while ((bi = bi.BaseType) != null) {
                    if (!bi.IsGenericType || bi.GetGenericTypeDefinition() != typeof(JSONRule<>)) {
                        continue;
                    }
                    if (type == bi.GetGenericArguments()[0]) {
                        return Rules[type_] = ((JSONRule) t.GetConstructor(Type.EmptyTypes).Invoke(a_object_0)).Fill(type_);
                    }
                }
            }
            type = type.BaseType;
        }

        if (type_.IsValueType) {
            return Rules[type_] = new JSONValueTypeRule().Fill(type_);
        }

        return Rules[type_] = new JSONRule().Fill(type_);
    }

    public static Type FindType(string fullname, string ns = null, string name = null) {
        Type type;
        if (_TypeCache.TryGetValue(fullname, out type)) {
            return type;
        }

        Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < asms.Length; i++) {
            Assembly asm = asms[i];
            if ((type = asm.GetType(fullname, false)) != null) {
                return _TypeCache[fullname] = type;
            }
        }

        if (type == null && ns != null && name != null) {
            for (int i = 0; i < asms.Length; i++) {
                Assembly asm = asms[i];
                Type[] types = asm.GetTypes();
                for (int ti = 0; ti < types.Length; ti++) {
                    type = types[ti];
                    if (type.Namespace == ns && type.Name == name) {
                        return _TypeCache[fullname] = type;
                    }
                }
            }
        }

        return _TypeCache[fullname] = null;
    }

    private static void _OnBeforeSerialize(object obj) {
        (obj as ISerializationCallbackReceiver)?.OnBeforeSerialize();
    }

    private static void _OnAfterDeserialize(object obj) {
        (obj as ISerializationCallbackReceiver)?.OnAfterDeserialize();
    }

}

using System;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Mono.Cecil;

/// <summary>
/// "Cross" reflection "framework" configuration for Enter the Gungeon.
/// </summary>
public class CrossGungeonConfig : ICrossConfig {

    public int SearchFocusRadius = 8;

    private static Assembly _Asm = Assembly.GetCallingAssembly();
    public Assembly Asm {
        get {
            return _Asm;
        }
    }

    private static IEnumerable<Assembly> _Assemblies = new List<Assembly>() {
        _Asm
    }.AsReadOnly();
    public IEnumerable<Assembly> Assemblies {
        get {
            return _Assemblies;
        }
    }

    public Dictionary<long, Type> GClasses = new Dictionary<long, Type>();

    public CrossGungeonConfig() {
        Type[] types = Asm.GetTypes();
        for (int i = 0; i < types.Length; i++) {
            Type type = types[i];
            if (!type.Name.StartsWith("GClass")) {
                continue;
            }
            string id = type.Name.Substring(6);
            if (id.Contains("`")) {
                string[] ids = id.Split('`');
                GClasses[
                    int.Parse(ids[1]) << 32 |
                    int.Parse(ids[0])
                ] = type;
                GClasses[
                    int.Parse(ids[0]) // Fallback
                ] = type;
                continue;
            }

            GClasses[int.Parse(id)] = type;
        }
    }
    
    public string TypeName(string name, int from_, int to_) {
        if (!name.Contains("GClass")) {
            return name;
        }
        Platform from = (Platform) from_;
        Platform to = (Platform) to_;
        int split = name.LastIndexOf("GClass");
        string pre = name.Substring(0, split);
        int id = int.Parse(name.Substring(split + 6));

        // TODO where do the shifts begin?
        // TODO mac? (give seems to be magically working with following code)

               if (from.Has(Platform.Unix)    && to.Has(Platform.Windows)) {
            id += 2;
        } else if (from.Has(Platform.Windows) && to.Has(Platform.Unix)) {
            id -= 2;
        }

        return pre + "GClass" + id;
    }

    public object Find(CrossSearch search) {
        string prefix = "UNKNOWN_";
        if (search.Type == CrossSearch.t_FieldInfo) {
            CrossSearch<FieldInfo> search_ = (CrossSearch<FieldInfo>) search;
            prefix = search.Returns.Name + "_";
            prefix = prefix[0].ToString().ToLowerInvariant() + prefix.Substring(1);

        } else if (search.Type == CrossSearch.t_MethodInfo) {
            CrossSearch<MethodInfo> search_ = (CrossSearch<MethodInfo>) search;
            prefix = search.Static ? "smethod_" : "method_";


        } else if (search.Type == CrossSearch.t_PropertyInfo) {
            CrossSearch<PropertyInfo> search_ = (CrossSearch<PropertyInfo>) search;
            prefix = search.Returns.Name + "_";

        }

        int preID = -1;
        if (!string.IsNullOrEmpty(search.Name)) {
            preID = int.Parse(search.Name.Substring(search.Name.IndexOf('_') + 1));
        }

        int preTypeID = -1;
        long typeOr = 0;
        long typeAnd = long.MaxValue;
        if (!string.IsNullOrEmpty(search.In)) {
            if (search.In.StartsWith("GClass")) {
                if (search.In.Contains("`")) {
                    string[] ids = search.In.Substring(6).Split('`');
                    preTypeID = int.Parse(ids[0]);
                    typeOr |= int.Parse(ids[1]) << 32;
                } else {
                    preTypeID = int.Parse(search.In.Substring(6));
                }
            } else {
                object obj = FindInType(search, search.In.XType(), prefix, preID);
                if (obj != null) {
                    return obj;
                }
            }
        }

        if (preTypeID != -1) {
            for (int ti = Math.Max(0, preTypeID - SearchFocusRadius); ti <= preTypeID + SearchFocusRadius && ti < GClasses.Count; ti++) {
                Type gclass;
                if (!GClasses.TryGetValue((ti & typeAnd) | typeOr, out gclass)) {
                    continue;
                }
                object obj = FindInType(search, gclass, prefix, preID);
                if (obj != null) {
                    return obj;
                }
            }
        }

        for (int ti = 0; ti < GClasses.Count; ti++) {
            if (preTypeID != -1 && preTypeID - SearchFocusRadius <= ti && ti <= preTypeID + SearchFocusRadius) {
                continue;
            }
            Type gclass;
            if (!GClasses.TryGetValue((ti & typeAnd) | typeOr, out gclass)) {
                continue;
            }
            object obj = FindInType(search, gclass, prefix, preID);
            if (obj != null) {
                return obj;
            }
        }

        return null;
    }

    public object FindInType(CrossSearch search, Type @in, string prefix, int preID) {
        // Check the context here; return null prematurely if context doesn't match.
        if (search.Context != null && search.Context.Length != 0) {
            // TODO check context!
        }

        BindingFlags flags =
            (search.Private ? BindingFlags.NonPublic : search.Public ? BindingFlags.Public : BindingFlags.Default) |
            (search.Static ? BindingFlags.Static : BindingFlags.Instance)
            ;

        if (search.Type == CrossSearch.t_FieldInfo) {
            // We just return the field. There's currently a too high risk of false positives.
            return @in.GetField(search.Name, flags);


        } else if (search.Type == CrossSearch.t_PropertyInfo) {
            // We do the same with the properties as with the fields.
            return @in.GetProperty(search.Name, flags);
        }

        if (search.Type != CrossSearch.t_MethodInfo) {
            return null;
        }

        // With methods, we can actually search.
        Console.WriteLine("METHOD RADIUS SEARCH?");
        if (preID != -1) {
            Console.WriteLine("METHOD RADIUS SEARCH.");
            for (int mi = Math.Max(0, preID - SearchFocusRadius); mi <= preID + SearchFocusRadius; mi++) {
                Console.WriteLine(prefix + mi);
                MethodInfo method = @in.GetMethod(prefix + mi, flags, null, search.Args, null);
                if (method == null) {
                    continue;
                }
                return method;
            }
        }

        // No method match? Huh, now we need to crawl through all properly flagged methods...
        MethodInfo[] methods = @in.GetMethods(flags);
        for (int mi = 0; mi < methods.Length; mi++) {
            MethodInfo method = methods[mi];
            ParameterInfo[] argsInfo = method.GetParameters();
            if (!method.Name.StartsWith(prefix) ||
                argsInfo.Length != search.Args.Length ||
                method.ReturnType != (search.Returns ?? CrossSearch.t_void)) {
                continue;
            }
            bool match = true;
            for (int pi = 0; pi < argsInfo.Length; pi++) {
                if (argsInfo[pi].ParameterType != search.Args[pi]) {
                    match = false;
                    continue;
                }
            }
            if (!match) {
                continue;
            }
            return method;
        }

        return null;
    }

}

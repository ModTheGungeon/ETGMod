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

    private readonly static char[] c_GClass = { 'G', 'C', 'l', 'a', 's', 's' };

    private static List<Assembly> _Assemblies = new List<Assembly>() {
        Assembly.GetCallingAssembly()
    };
    public IEnumerable<Assembly> Assemblies {
        get {
            return _Assemblies;
        }
    }

    public string TypeName(string name, int from_, int to_) {
        if (!name.Contains("GClass")) {
            return name;
        }
        Platform from = (Platform) from_;
        Platform to = (Platform) to_;
        string[] split = name.Split(c_GClass);
        int id = int.Parse(split[1]);

        // TODO where do the shifts begin?
        // TODO mac?

               if (from == Platform.Linux && to == Platform.Windows) {
            id += 2;
        } else if (from == Platform.Windows && to == Platform.Linux) {
            id -= 2;
        }

        return split[0] + "GClass" + id;
    }

}

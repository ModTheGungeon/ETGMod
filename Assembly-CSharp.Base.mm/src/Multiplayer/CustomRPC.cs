using System;
using System.Reflection;
using ETGMultiplayer;

public class CustomRPC : Attribute{

    private readonly static Type t_CustomRPC = typeof(CustomRPC);

    string Name;

    public CustomRPC(string functionName) {
        Name = functionName;
    }

    public static void DocAllRPCAttributes() {
        Assembly curr = Assembly.GetExecutingAssembly();

        Type[] allTypes = curr.GetTypes();

        for (int ti = 0; ti < allTypes.Length; ti++) {
            Type type = allTypes[ti];
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

            for (int mi = 0; mi < methods.Length; mi++) {
                MethodInfo method = methods[mi];
                ParameterInfo[] args = method.GetParameters();
                object[] attributes = method.GetCustomAttributes(false);

                for (int ai = 0; ai < attributes.Length; ai++) {
                    object attribute = attributes[ai];

                    if (t_CustomRPC.IsAssignableFrom(attribute.GetType())) {
                        PacketHelper.allRPCs.Add(((CustomRPC) attribute).Name, method);
                    }
                }
            }
        }
    }

}


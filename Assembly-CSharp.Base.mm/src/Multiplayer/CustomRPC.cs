using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Steamworks;
using ETGMultiplayer;

class CustomRPC : Attribute{

    string name;

    public CustomRPC(string functionName) {
        name=functionName;
    }

    public static void DocAllRPCAttributes() {
        Assembly curr = Assembly.GetExecutingAssembly();

        Type[] allTypes = curr.GetTypes();

        for(int i = 0; i < allTypes.Length; i++) {
            Type iType = allTypes[i];

            MethodInfo[] pubMethods = iType.GetMethods();

            for (int j = 0; j < pubMethods.Length; j++) {
                MethodInfo jInf = pubMethods[j];

                if (!jInf.IsStatic)
                    continue;

                ParameterInfo[] param = jInf.GetParameters();

                object[] attributes = jInf.GetCustomAttributes(false);

                for (int k = 0; k < attributes.Length; k++) {
                    object kAt = attributes[k];

                    if (kAt.GetType()==typeof(CustomRPC)) {
                        PacketHelper.allRPCs.Add(((CustomRPC)kAt).name,jInf);
                    }
                }
            }

        }

    }

}


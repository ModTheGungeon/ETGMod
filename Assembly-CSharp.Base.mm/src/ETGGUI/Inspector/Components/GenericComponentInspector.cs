using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ETGGUI.Inspector {
    public class GenericComponentInspector {

        public static List<string> PropertyBlacklist = new List<string>() {
            "dfControl::InverseClipChildren"
        };

        public void OnGUI(object instance) {

            PropertyInfo[] allProperties = instance.GetType().GetProperties();

            foreach (PropertyInfo inf in allProperties) {
                string fullName = inf.DeclaringType.FullName + "::" + inf.Name;
                if (PropertyBlacklist.Contains(fullName) ||
                    inf.MemberType == MemberTypes.Method ||
                    inf.GetIndexParameters().Length != 0 ||
                    !inf.CanRead || !inf.CanWrite) {
                    continue;
                }

                try {
                    object getProperty = ReflectionHelper.GetValue(inf, instance);
                    object setProperty = ETGModInspector.DrawProperty(inf, getProperty);
                    if (getProperty != setProperty) {
                        ReflectionHelper.SetValue(inf, instance, setProperty);
                    }
                } catch (Exception e) {
                    Debug.LogWarning("GenericComponentInspector: Blacklisting " + fullName);
                    Debug.LogWarning(e);
                    PropertyBlacklist.Add(fullName);
                }
            }

        }

    }
}

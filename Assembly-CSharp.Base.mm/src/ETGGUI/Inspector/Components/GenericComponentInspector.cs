using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ETGGUI.Inspector {
    public class GenericComponentInspector {

        public static List<string> PropertyBlacklist = new List<string>() {
            "UnityEngine.Object::name",
            "UnityEngine.Component::tag",
            "dfControl::InverseClipChildren"
        };

        private static PropertyInfo p_Object_name = typeof(UnityEngine.Object).GetProperty("name");
        private static PropertyInfo p_Transform_tag = typeof(Transform).GetProperty("tag");
        private static PropertyInfo p_GameObject_activeSelf = typeof(GameObject).GetProperty("activeSelf");
        private static PropertyInfo p_Transform_position = typeof(Transform).GetProperty("position");

        private List<string> crawled = new List<string>();
        public void OnGUI(object instance) {
            crawled.Clear();
            crawled.Add("name");
            crawled.Add("tag");
            crawled.Add("position");
            PropertyInfo[] allProperties = instance.GetType().GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
            );

            if (instance is Transform) {
                object getProperty = ((Transform) instance).name;
                object setProperty = ETGModInspector.DrawProperty(p_Object_name, getProperty);
                if (getProperty != setProperty) 
                    ((Transform) instance).name = (string) setProperty;

                getProperty = ((Transform) instance).gameObject.activeSelf;
                setProperty = ETGModInspector.DrawProperty(p_GameObject_activeSelf, getProperty);
                if (getProperty != setProperty)
                    ((Transform) instance).gameObject.SetActive((bool) setProperty);

                getProperty = ((Transform) instance).tag;
                setProperty = ETGModInspector.DrawProperty(p_Transform_tag, getProperty);
                if (getProperty != setProperty) 
                    ((Transform) instance).tag = (string) setProperty;
            }

            GUILayout.Label("");
            GUILayout.Label("Component: " + instance.GetType().FullName);

            foreach (PropertyInfo inf in allProperties) {
                string fullName = inf.DeclaringType.FullName + "::" + inf.Name;
                if (PropertyBlacklist.Contains(fullName) ||
                    crawled.Contains(fullName) ||
                    inf.MemberType == MemberTypes.Method ||
                    inf.GetIndexParameters().Length != 0 ||
                    !inf.CanRead || !inf.CanWrite) {
                    continue;
                }
                crawled.Add(inf.Name);

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

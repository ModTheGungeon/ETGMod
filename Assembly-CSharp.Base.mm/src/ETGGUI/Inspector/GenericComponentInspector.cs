using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace src.ETGGUI.Inspector {
    public class GenericComponentInspector {

        public void OnGUI(object instance) {

            PropertyInfo[] allProperties = instance.GetType().GetProperties();

            foreach (PropertyInfo inf in allProperties) {
                if (inf.MemberType==MemberTypes.Method)
                    continue;
                //object getProperty = ReflectionHelper.GetValue(inf, instance);


                GUILayout.Label(inf.ToString());
            }

        }

    }
}

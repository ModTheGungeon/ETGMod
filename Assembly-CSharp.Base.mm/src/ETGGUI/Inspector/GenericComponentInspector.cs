using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ETGGUI.Inspector {
    public class GenericComponentInspector {

        public void OnGUI(object instance) {

            PropertyInfo[] allProperties = instance.GetType().GetProperties();

            foreach (PropertyInfo inf in allProperties) {
                if (inf.MemberType==MemberTypes.Method)
                    continue;
                //object getProperty = ReflectionHelper.GetValue(inf, instance);

                //object setProperty = ETGModInspector.DrawProperty(inf, getProperty);

                //ReflectionHelper.SetValue(inf,instance,setProperty);
            }

        }

    }
}

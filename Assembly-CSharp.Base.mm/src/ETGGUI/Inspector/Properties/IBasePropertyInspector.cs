using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ETGGUI.Inspector {
    public interface IBasePropertyInspector {

        object OnGUI(PropertyInfo info, object input);

    }
}

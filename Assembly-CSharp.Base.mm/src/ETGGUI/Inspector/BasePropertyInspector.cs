using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace src.ETGGUI.Inspector {
    public interface IBasePropertyInspector {

        object OnGUI(PropertyInfo info, object input);

    }
}

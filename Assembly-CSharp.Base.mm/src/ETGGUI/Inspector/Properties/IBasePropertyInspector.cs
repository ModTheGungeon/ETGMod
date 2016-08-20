using System.Reflection;

namespace ETGGUI.Inspector {
    public interface IBasePropertyInspector {

        object OnGUI(PropertyInfo info, object input);

    }
}

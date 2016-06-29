using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using src.ETGGUI.Hierarchy;

class ETGModInspector: IETGModMenu {

    public static Dictionary<System.Type, System.Type> InspectorRegistry = new Dictionary<Type, Type>();

    public void Start() {

        //Init the hierarchy.
        ETGHierarchy.Start();
    }

    public void Update() {

    }

    public void OnGUI() {
        ETGHierarchy.OnGUI();
    }

    public void OnDestroy() {

    }
}

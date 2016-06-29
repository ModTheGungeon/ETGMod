using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ETGGUI.Hierarchy {
    class ETGHierarchy {

        private static Dictionary<Transform, HierarchyComponent> fullHierarchy = new Dictionary<Transform, HierarchyComponent>();

        private static Vector2 scrollPos;
        private static Rect windowRect;

        public static void Start() {

            Debug.Log("Compiling transforms into a hierarchy.");
            

            windowRect=new Rect(0,0,450,900);
        }


        public static void OnGUI() {
            CompileExistingTransforms();
            windowRect=GUILayout.Window(15, windowRect, WindowFunction, "Hierarchy");
        }

        private static void WindowFunction(int windowID) {
            scrollPos=GUILayout.BeginScrollView(scrollPos);
            foreach (HierarchyComponent c in fullHierarchy.Values) {
                bool isButton = GUILayout.Button(c.reference.name);
                c.showChildren=isButton ? !c.showChildren : c.showChildren;
                if (isButton)
                    ETGModInspector.targetObject=c.reference;
                c.OnGUI();
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        //Compiles all transforms currently in the scene into the dictionary.
        public static void CompileExistingTransforms() {
            Transform[] allTransforms = GameObject.FindObjectsOfType<Transform>();

            foreach (Transform t in allTransforms) {
                //If this object is on the root of the scene, we iterate downward through all it's children and add them all.
                if (t.root==t) {
                    if(t==null)
                        continue;

                    if(!fullHierarchy.ContainsKey(t))
                        fullHierarchy[t]=new HierarchyComponent(t.gameObject, false);

                    CompileIntoTransform(fullHierarchy[t]);
                }
            }

        }

        private static void CompileIntoTransform(HierarchyComponent comp) {
            for(int i = 0; i < comp.reference.transform.childCount; i++) {
                if(!comp.children.ContainsKey(comp.reference.transform.GetChild(i)))
                    comp.children[comp.reference.transform.GetChild(i)]=new HierarchyComponent(comp.reference.transform.GetChild(i).gameObject,false);
                CompileIntoTransform(comp.children[comp.reference.transform.GetChild(i)]);
            }
        }

    }
}

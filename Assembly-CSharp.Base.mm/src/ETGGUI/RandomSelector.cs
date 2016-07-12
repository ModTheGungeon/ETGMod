using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ETGGUI {
    public class RandomSelector {

        public static void OnGUI() {
            if (Foyer.Foyer_0.foyerCharacterSelectFlag_0) {
                if (GUI.Button(new Rect(Screen.width-100, Screen.height-100, 50, 50), "Random")) {
                    FoyerCharacterSelectFlag[] array = acceptableFlags();
                    FoyerCharacterSelectFlag f = array[UnityEngine.Random.Range(0,array.Length-1)];

                    Foyer.Foyer_0.StartCoroutine(((patch_Foyer)(Foyer.Foyer_0)).method_8(0.25f,f));
                    GameObject.Destroy(GameObject.FindObjectOfType<FoyerInfoPanelController>().gameObject);
                }
            }
        }

        public static FoyerCharacterSelectFlag[] acceptableFlags() {
            List<FoyerCharacterSelectFlag> list = new List<FoyerCharacterSelectFlag>();

            foreach(FoyerCharacterSelectFlag f in GameObject.FindObjectsOfType<FoyerCharacterSelectFlag>()) {
                if (!f.bool_0)
                    list.Add(f);
            }

            return list.ToArray();
        }

    }
}

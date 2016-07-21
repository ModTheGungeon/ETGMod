using System.Collections.Generic;
using UnityEngine;

namespace ETGGUI {
    public class RandomSelector {

        public static void OnGUI() {
            if (Foyer.Instance.CurrentSelectedCharacterFlag) {
                if (GUI.Button(new Rect(Screen.width-100, Screen.height-100, 50, 50), "Random")) {
                    FoyerCharacterSelectFlag[] array = acceptableFlags();
                    FoyerCharacterSelectFlag f = array[Random.Range(0,array.Length-1)];

                    Foyer.Instance.StartCoroutine(((patch_Foyer)(Foyer.Instance)).OnSelectedCharacter(0.25f, f));
                    Object.Destroy(Object.FindObjectOfType<FoyerInfoPanelController>().gameObject);
                }
            }
        }

        public static FoyerCharacterSelectFlag[] acceptableFlags() {
            List<FoyerCharacterSelectFlag> list = new List<FoyerCharacterSelectFlag>();

            foreach(FoyerCharacterSelectFlag f in Object.FindObjectsOfType<FoyerCharacterSelectFlag>()) {
                if (!f.IsCoopCharacter)
                    list.Add(f);
            }

            return list.ToArray();
        }

    }
}

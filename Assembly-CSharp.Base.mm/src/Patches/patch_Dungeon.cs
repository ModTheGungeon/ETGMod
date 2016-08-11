#pragma warning disable 0626
#pragma warning disable 0649

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dungeonator {
    internal class patch_Dungeon : Dungeon {

        private extern IEnumerator orig_Start();
        private IEnumerator Start() {
            IEnumerator start = orig_Start();

            List<object> list = new List<object>();
            while (start.MoveNext()) {
                list.Add(start.Current);
            }

            // Set ETGMod.StartCoroutine to be sure it's not null.
            ETGMod.StartCoroutine = StartCoroutine;
            ETGMod.Assets.HandleAll();

            return list.GetEnumerator();
        }

    }
}

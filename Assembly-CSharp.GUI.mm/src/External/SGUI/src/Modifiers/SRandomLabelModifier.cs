using System.Text;
using UnityEngine;

namespace SGUI {
    public class SRandomLabelModifier : SModifier {

        public override void Update() {
            if (Random.value > 0.1f) {
                return;
            }
            SLabel label = (SLabel) Elem;
            string orig = label.Text;
            StringBuilder repl = new StringBuilder();
            while (orig.Length > 0) {
                int i = Random.Range(0, orig.Length);
                char c = orig[i];
                repl.Append(c);
                orig = orig.Remove(i, 1);
            }
            label.Text = repl.ToString();
        }

    }
}

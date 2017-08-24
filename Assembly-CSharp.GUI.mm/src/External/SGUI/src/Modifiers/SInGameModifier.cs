using System.Text;
using UnityEngine;

namespace SGUI {
#if SGUI_EtG
    public class SInGameModifier : SModifier {

        public bool ChangeVisible = false;
        public bool ChangeEnabled = false;

        public override void Init() {
            ChangeVisible |= Elem.Visible;
            ChangeEnabled |= Elem.Enabled;
        }

        public override void Update() {
            if (ChangeVisible) {
                Elem.Visible = GameManager.Instance.IsPaused;
            }
            if (ChangeEnabled) {
                Elem.Enabled = GameManager.Instance.IsPaused;
            }
        }

    }
#endif
}

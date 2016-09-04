#pragma warning disable RECS0018
using System;
using System.ComponentModel;
using UnityEngine;

namespace SGUI {
	public class SModifier {

        public virtual void UpdateStyle(SElement elem) {

        }

        public virtual void Update(SElement elem) {

        }

	}

    public class SDModifier : SModifier {

        public Action<SElement> OnUpdateStyle;
        public override void UpdateStyle(SElement elem) {
            OnUpdateStyle?.Invoke(elem);
        }

        public Action<SElement> OnUpdate;
        public override void Update(SElement elem) {
            OnUpdate?.Invoke(elem);
        }

    }
}

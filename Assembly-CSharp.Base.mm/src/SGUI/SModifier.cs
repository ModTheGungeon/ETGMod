#pragma warning disable RECS0018
using System;
using System.ComponentModel;
using UnityEngine;

namespace SGUI {
	public class SModifier {

        public SElement Elem;

        public virtual void UpdateStyle() {

        }

        public virtual void Update() {

        }

	}

    public class SDModifier : SModifier {

        public Action<SElement> OnUpdateStyle;
        public override void UpdateStyle() {
            OnUpdateStyle?.Invoke(Elem);
        }

        public Action<SElement> OnUpdate;
        public override void Update() {
            OnUpdate?.Invoke(Elem);
        }

    }
}

#pragma warning disable RECS0018
using System;
using System.ComponentModel;
using UnityEngine;

namespace SGUI {
	public class SModifier {

        public SElement Elem;

        public virtual void Init() {
            
        }

        public virtual void UpdateStyle() {

        }

        public virtual void Update() {

        }

	}

    public class SDModifier : SModifier {

        public Action<SElement> OnInit;
        public override void Init() {
            if (OnInit != null) OnInit(Elem);
        }

        public Action<SElement> OnUpdateStyle;
        public override void UpdateStyle() {
            if (OnUpdateStyle != null) OnUpdateStyle(Elem);
        }

        public Action<SElement> OnUpdate;
        public override void Update() {
            if (OnUpdate != null) OnUpdate(Elem);
        }

    }
}

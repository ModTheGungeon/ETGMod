using System.Collections.Generic;
using UnityEngine;

namespace SGUI {
    public class SAnimationSequence : SAnimation {
        public List<SAnimation> Sequence = new List<SAnimation>();

        protected float[] _Offsets;

        protected int _CurrentIndex;
        public int CurrentIndex {
            get {
                return _CurrentIndex;
            }
        }
        public SAnimation Current {
            get {
                return _CurrentIndex < 0 || Sequence.Count <= _CurrentIndex ? null : Sequence[_CurrentIndex];
            }
        }
        public float CurrentOffset {
            get {
                return _CurrentIndex < 0 || Sequence.Count <= _CurrentIndex ? 0f : _Offsets[_CurrentIndex];
            }
        }
        public EStatus CurrentStatus {
            get {
                return _CurrentIndex < 0 || Sequence.Count <= _CurrentIndex ? EStatus.Finished : Sequence[_CurrentIndex].Status;
            }
        }

        public SAnimationSequence()
            : base(0f) {
        }

        public override void OnStart() {
            Duration = 0f;
            _CurrentIndex = 0;

            _Offsets = new float[Sequence.Count];
            for (int i = 0; i < Sequence.Count; i++) {
                SAnimation anim = Sequence[i];
                if (anim == null) continue;
                _Offsets[i] = Duration;
                Duration += anim.Duration;
                anim.Loop = false;
                anim.AutoStart = false;
                anim.Elem = anim.Elem ?? Elem;
                anim.Init();
            }

            Current.Start();
        }

        public override void Animate(float t) {
            if (CurrentStatus == EStatus.Finished) {
                if (Current != null) Current.End();
                _CurrentIndex++;
                if (Current != null) Current.Start();
            }

            if (Current == null) return;

            t = ((t * Duration) - CurrentOffset) / Current.Duration;
            Current.Animate(Current.Easing(t));
            if (t >= 1f) Current.End();
        }

        public override void OnEnd() {
            base.OnEnd();
            if (Current != null) Current.End();
            _CurrentIndex = 0;
        }

    }
}

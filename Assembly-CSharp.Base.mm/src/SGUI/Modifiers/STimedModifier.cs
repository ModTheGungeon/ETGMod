using System;
using UnityEngine;

namespace SGUI {
    public abstract class STimedModifier : SModifier {

        public float TimeStart;
        public EStatus Status = EStatus.Start;

        public float Duration;

        public bool SpeedUnscaled = true;

        public STimedModifier()
            : this(0.3f) {
        }

        public STimedModifier(float duration) {
            Duration = duration;
        }

        public override void UpdateStyle() {
            if (Status == EStatus.Start) {
                Start();
                Animate(0f);

                TimeStart = SpeedUnscaled ? Time.unscaledTime : Time.time;
                Status = EStatus.Running;
            }
        }

        public override void Update() {
            if (Elem.Backend.RenderOnGUI && !Elem.Backend.IsOnGUIRepainting) {
                return;
            }

            if (Status != EStatus.Running) {
                return;
            }

            float t = ((SpeedUnscaled ? Time.unscaledTime : Time.time) - TimeStart) / Duration;
            if (t > 1f) {
                End();
                Status = EStatus.End;
                return;
            }

            Animate(t);
        }

        public abstract void Start();

        public abstract void Animate(float t);

        public virtual void End() {
            Animate(1f);
        }

        public enum EStatus {
            Start,
            Running,
            End
        }

    }

    public abstract class SDTimedModifier : STimedModifier {
        
        public Action OnStart;
        public override void Start() {
            OnStart?.Invoke();
        }

        public Action<float> OnAnimate;
        public override void Animate(float t) {
            OnAnimate?.Invoke(t);
        }

        public override void End() {
            Animate(1f);
        }

    }

}

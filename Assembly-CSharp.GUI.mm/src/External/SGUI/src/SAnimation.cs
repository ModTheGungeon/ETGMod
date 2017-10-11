using System;
using UnityEngine;

namespace SGUI {
    public abstract class SAnimation : SModifier {

        public float TimeStart;
        public EStatus Status = EStatus.Uninitialized;

        public float Duration;

        public bool SpeedUnscaled = true;
        public bool AutoStart = true;
        public bool Loop = false;
        public bool Paused = false;

        public Func<float, float> Easing = SEasings.SineEaseInOut;

        public SAnimation()
            : this(0.3f) {
        }

        public SAnimation(float duration) {
            Duration = duration;
        }

        public override void Init() {
            if (Status != EStatus.Uninitialized) {
                return;
            }

            OnInit();

            if (AutoStart) {
                Start();
                Animate(0f);
            } else {
                Status = EStatus.Finished;
            }
        }

        public virtual void OnInit() {
        }

        public override void Update() {
            if (Elem.Backend.RenderOnGUI && !Elem.Backend.IsOnGUIRepainting) {
                return;
            }

            if (Status != EStatus.Running || Paused) {
                return;
            }

        Loop:
            float t = ((SpeedUnscaled ? SGUIRoot.TimeUnscaled : SGUIRoot.Time) - TimeStart) / Duration;
            if (t >= 1f) {
                End();
                if (!Loop) return;
                Start();
                TimeStart += (t - 1f) * Duration;
                goto Loop;
            }

            Animate(Easing(t));
        }

        public void Start() {
            OnStart();

            TimeStart = (SpeedUnscaled ? SGUIRoot.TimeUnscaled : SGUIRoot.Time);
            Status = EStatus.Running;
        }

        public abstract void OnStart();

        public abstract void Animate(float t);

        public void End() {
            Status = EStatus.Finished;
            OnEnd();
        }

        public virtual void OnEnd() {
            Animate(1f);
        }

        public enum EStatus {
            Uninitialized,
            Running,
            Finished
        }

    }

    public abstract class SDAnimation : SAnimation {

        public Action<SAnimation> DOnInit;
        public override void OnInit() {
            DOnInit.Invoke(this);
        }

        public Action<SAnimation> DOnStart;
        public override void OnStart() {
            if (DOnStart != null) DOnStart(this);
        }

        public Action<SAnimation, float> DOnAnimate;
        public override void Animate(float t) {
            if (DOnAnimate != null) DOnAnimate(this, t);
        }

        public override void OnEnd() {
            Animate(1f);
        }

    }

}

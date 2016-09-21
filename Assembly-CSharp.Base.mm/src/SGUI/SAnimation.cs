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

        public SAnimation()
            : this(0.3f) {
        }

        public SAnimation(float duration) {
            Duration = duration;
        }

        public override void Init() {
            if (AutoStart) {
                Start();
                Animate(0f);
            } else {
                Status = EStatus.Finished;
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
                if (!Loop) {
                    return;
                }
                Start();
            }

            Animate(t);
        }

        public void Start() {
            OnStart();

            TimeStart = SpeedUnscaled ? Time.unscaledTime : Time.time;
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
        
        public Action DOnStart;
        public override void OnStart() {
            DOnStart?.Invoke();
        }

        public Action<float> DOnAnimate;
        public override void Animate(float t) {
            DOnAnimate?.Invoke(t);
        }

        public override void OnEnd() {
            Animate(1f);
        }

    }

}

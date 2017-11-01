using System;
using SGUI;
using UnityEngine;

namespace ETGMod.GUI {
    public class SNopAnimation : SAnimation {
        public SNopAnimation(float duration) : base(duration) {}

        public override void OnInit() {
            base.OnInit();
        }

        public override void OnStart() {}

        public override void Animate(float t) {}
    }

    public class SSlideInHorizontallyAnimation : SAnimation {
        public float HorizontalOffset;
        private float _XDiff;
        private float _XOrig;
        private float _XStart;

        public SSlideInHorizontallyAnimation(int x_offset, float duration) : base(duration) {
            HorizontalOffset = x_offset;
        }

        public override void OnInit() {
            base.OnInit();

            var target = Elem.Position.x + HorizontalOffset;
            var diff = target - Elem.Position.x;

            _XDiff = diff;
            _XOrig = Elem.Position.x;

            _XStart = target;
            Elem.Position.x = _XStart;
        }

        public override void OnStart() {}

        public override void Animate(float t) {
            Elem.Position.x = _XStart - t * _XDiff;
        }

        public override void OnEnd() {
            Elem.Position.x = _XOrig;
        }
    }

    public class SNotificationDecayAnimation : SAnimation {

        public SNotificationDecayAnimation(NotificationController n)
            : this(0.3f, n) {
        }
        public SNotificationDecayAnimation(float duration, NotificationController n)
            : base(duration) {
            NotificationController = n;
        }

        protected float _OriginalH;
        protected float _OriginalY;
        protected float _YDiff;
        public NotificationController NotificationController;


        public override void OnInit() {
            AutoStart = false;
            Elem.UpdateBounds = false;
        }

        public override void OnStart() {
            Loop = false;

            ((NotificationSGroup)Elem).Decaying = true;

            _OriginalY = Elem.Position.y;
            var current = Elem.Position.y;
            float target = Screen.height + NotificationController.NotificationPadding;
            if (Elem.Previous != null) target = Elem.Previous.Position.y;

            _YDiff = target - current;

            Elem.Children.Clear();
            Elem.Size.y = _OriginalH;
        }

        public override void Animate(float t) {
            Elem.Position.y = _OriginalY + _YDiff * t;
            if (Elem.Parent != null) {
                for (int i = 0; i < Elem.Parent.Children.Count; i++) {
                    var child = Elem.Parent.Children[i];
                    if (child == Elem) continue;
                    child.UpdateStyle();
                }
            }
        }

        public override void OnEnd() {
            Elem.Remove();
            base.OnEnd();
        }

    }

    public class SNotificationAnimationSequence : SAnimationSequence {
        public SFadeOutAnimation FadeOut;
        public SNotificationDecayAnimation Shrink;
        public NotificationController NotificationController;

        public float IdleDuration;
        public float FadeOutDuration;
        public float DecayDuration;
        
        public SNotificationAnimationSequence(float durationIdle, float durationFadeOut, float durationDecay, NotificationController controller) {
            IdleDuration = durationIdle;
            FadeOutDuration = durationFadeOut;
            DecayDuration = durationDecay;
            NotificationController = controller;
        }

        public override void OnStart() {
            Sequence.Add(new SNopAnimation(IdleDuration));
            
            if (FadeOut == null) {
                Sequence.Add(FadeOut = new SFadeOutAnimation(FadeOutDuration));
            }

            if (Shrink == null) {
                Sequence.Add(Shrink = new SNotificationDecayAnimation(DecayDuration, NotificationController));
            }

            base.OnStart();
        }

    }

    public class STest : SAnimation {
        public override void Animate(float t) {
            var color = Elem.Foreground;
            color.a = t - 1f;
            Elem.Foreground = color;
        }

        public override void OnStart() {
            Duration = 100;
        }
    }
}

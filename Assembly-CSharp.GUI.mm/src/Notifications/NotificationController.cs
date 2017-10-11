using System;
using System.Collections.Generic;
using UnityEngine;
using SGUI;

namespace ETGMod.GUI {
    internal class NotificationSGroup : SGroup {
        public bool Decaying = false;
        public bool Displayed = false;
    }

    public class NotificationController : MonoBehaviour {
        public float MaxNotificationDelay = 0.5f;
        public int NotificationSpaceWidth { get { return Screen.width / 4; } }
        public int NotificationHeight { get { return Screen.height / 8; } }
        public int NotificationPadding = 5;
        private float _CurrentNotificationDelay;
        private SGroup _NotificationGroup;

        public float IdleDuration = 2f;
        public float FadeoutDuration = 2f;
        public float DecayDuration = 1f;
        public int SlideOffset;
        public float SlideDuration = 1f;

        public void Awake() {
            SlideOffset = NotificationSpaceWidth;

            _CurrentNotificationDelay = MaxNotificationDelay;
            _NotificationGroup = new SGroup {
                Visible = true,
                Background = new Color(0, 0, 0, 0),

                OnUpdateStyle = (obj) => {
                    var width = NotificationSpaceWidth;
                    obj.Position = new Vector2(Screen.width - width, 0);
                    obj.Size = new Vector2(width, Screen.height);
                },

                Children = { }
            };
        }

        private SGroup _CreateNotificationSGroup(Notification notif) {
            var sgroup = new NotificationSGroup {
                Visible = true,
                Background = notif.BackgroundColor,

                OnUpdateStyle = (obj) => {
                    var group = (NotificationSGroup)obj;

                    if (group.Decaying) return;

                    if (obj.Previous == null && !group.Displayed) {
                        group.Displayed = true;
                        var seq = new SNotificationAnimationSequence(
                            IdleDuration,
                            FadeoutDuration,
                            DecayDuration,
                            this
                        );
                        group.Modifiers.Add(seq);
                        seq.Start();
                    }

                    float ysize = ((SGroup)obj).AutoLayoutPadding;
                    for (int i = 0; i < obj.Children.Count; i++) {
                        var child = obj.Children[i];
                        if (child is SGroup) {
                            child.UpdateStyle();
                            ysize += child.Size.y;
                        }
                    }

                    float y;
                    if (obj.Previous != null) y = obj.Previous.Position.y - NotificationPadding;
                    else y = Screen.height;

                    y -= obj.Size.y;

                    obj.Size = new Vector2(NotificationSpaceWidth, ysize);
                    obj.Position = new Vector2(obj.Position.x, y);
                },

                Children = { }
            };
            if (notif.Image != null) {
                sgroup.Children.Add(new SImage(notif.Image) {
                    OnUpdateStyle = (obj) => {
                        var image = (SImage)obj;
                        image.Size = new Vector2(image.Texture.width, image.Texture.height);
                    }
                });
            }

            var inner_group = new SGroup {
                Background = new Color(0f, 0f, 0f, 0f),
                AutoLayout = (self) => self.AutoLayoutVertical,
                Children = {
                    new SLabel(notif.Title) { Foreground = notif.TitleColor },
                    new SLabel(notif.Content) { Foreground = notif.ContentColor }
                },
                OnUpdateStyle = (obj) => {
                    float ysize = ((SGroup)obj).AutoLayoutPadding;
                    for (int i = 0; i < obj.Children.Count; i++) {
                        var child = obj.Children[i];
                        if (child is SLabel) {
                            ysize += obj.Backend.MeasureText(((SLabel)child).Text, obj.Size.x).y;
                            ysize += ((SGroup)obj).AutoLayoutPadding;
                        }
                    }

                    if (notif.Image != null) {
                        obj.Position = new Vector2(notif.Image.width, 0);
                        obj.Size = new Vector2(obj.Parent.Size.x - notif.Image.width, ysize);
                    } else {
                        obj.Position = new Vector2(0, 0);
                        obj.Size = new Vector2(obj.Parent.Size.x, ysize);
                    }
                },
                Size = new Vector2(NotificationSpaceWidth, NotificationHeight)
            };

            sgroup.Children.Add(inner_group);
            inner_group.UpdateStyle();
            sgroup.UpdateStyle();

            sgroup.Modifiers.Add(new SSlideInHorizontallyAnimation(
                SlideOffset,
                SlideDuration
            ));

            return sgroup;
        }

        public void Notify(Notification notif) {
            var sgroup = _CreateNotificationSGroup(notif);
            _NotificationGroup.Children.Add(sgroup);
        }
    }
}

using System;
using SGUI;
using UnityEngine;

namespace ETGMod.GUI {
    public class Notification {
        public Texture Image;
        public string Title;
        public string Content;
        public Color BackgroundColor = UnityUtil.NewColorRGB(0, 0, 0);
        public Color TitleColor = UnityUtil.NewColorRGB(255, 255, 255);
        public Color ContentColor = UnityUtil.NewColorRGB(200, 200, 200);
        internal float CurrentDecayTime;
        internal bool Decayed = false;

        public Notification(string title, string content) {
            Title = title;
            Content = content;
        }

        public Notification(Texture image, string title, string content) : this(title, content) {
            Image = image;
        }
    }
}

using System;
using System.Collections.Generic;
using SGUI;
using UnityEngine;

namespace ETGMod.GUI {
    public class MenuController : MonoBehaviour {
        internal Menu OpenMenu = null;
        private GameObject _GameObject = new GameObject("ETGMod Menu Controller");
        private Dictionary<KeyCode[], Menu> _Menus = new Dictionary<KeyCode[], Menu>();

        public void AddMenu<T>(KeyCode[] keys) where T : Menu {
            var menu = _Menus[keys] = _GameObject.AddComponent<T>();
            DontDestroyOnLoad(menu);
            menu.MenuController = this;
            menu.Keys = keys;
            menu.Window = menu.CreateWindow();
            menu.Window.Visible = false;
        }

        public void Update()     {
            foreach (var pair in _Menus) {
                for (int i = 0; i < pair.Key.Length; i++) {
                    var key = pair.Key[i];
                    if (Input.GetKeyDown(key)) {
                        if (OpenMenu == pair.Value) {
                            OpenMenu.Hide();
                            OpenMenu = null;
                            return;
                        }
                        if (OpenMenu != null) OpenMenu.Hide();
                        OpenMenu = pair.Value;
                        OpenMenu.Show();
                    }
                }
            }
        }
    }

    public abstract class Menu : MonoBehaviour {
        public SElement Window;
        public KeyCode[] Keys;
        internal MenuController MenuController;

        public abstract SElement CreateWindow();
        public virtual void OnShow() {}
        public virtual void OnHide() {}

        public void Show() {
            if (MenuController.OpenMenu != null) MenuController.OpenMenu.Hide();
            MenuController.OpenMenu = this;
            Window.Visible = true;
            OnShow();
        }

        public void Hide() {
            if (MenuController.OpenMenu == this) MenuController.OpenMenu = null;
            Window.Visible = false;
            OnHide();
        }
    }
}
using System;
using System.Collections.Generic;
using SGUI;
using UnityEngine;
using ETGMod.GUI;
using System.Text;

namespace ETGMod.Console {
    public partial class ConsoleMenu : Menu {
        public static ConsoleMenu Instance;

        internal SGroup OutputBox {
            get {
                return (SGroup)Window[(int)WindowChild.OutputBox];
            }
        }
        internal SGroup AutocompleteBox {
            get {
                return (SGroup)Window[(int)WindowChild.AutocompleteBox];
            }
        }
        internal STextField InputBox {
            get {
                return (STextField)Window[(int)WindowChild.InputBox];
            }
        }

        private enum WindowChild {
            OutputBox,
            AutocompleteBox,
            InputBox
        }

        public override SElement CreateWindow() {
            Instance = this;
            var etgmod = Backend.SearchForBackend("ETGMod");

            Console.Instance.AddDefaultCommands();

            return new SGroup {
                Background = new Color(0, 0f, 0f, 0.8f),

                OnUpdateStyle = elem => {
                    elem.Fill(0);
                },

                Children = {
                    new SGroup { // OutputBox
                        Background = new Color(0, 0, 0, 0),
                        AutoLayout = (self) => self.AutoLayoutVertical,
                        ScrollDirection = SGroup.EDirection.Vertical,
                        OnUpdateStyle = (elem) => {
                            elem.Fill(0);
                            elem.Size -= new Vector2(0, elem.Backend.LineHeight);
                        },
                        Children = {
                            new SLabel($"ETGMod v{etgmod.BestMatch?.Instance.StringVersion ?? "?"}") {
                                Foreground = UnityUtil.NewColorRGB(0, 161, 231),
                                Modifiers = {
                                    new STest()
                                }
                            }
                        }
                    },
                    new SGroup { // AutocompleteBox
                        Background = new Color(0.2f, 0.2f, 0.2f, 0.9f),
                        AutoLayout = (self) => self.AutoLayoutVertical,
                        ScrollDirection = SGroup.EDirection.Vertical,
                        OnUpdateStyle = (elem) => {
                            elem.Size.x = elem.Parent.InnerSize.x;
                            elem.Size.y = elem.Parent.InnerSize.y / 10; // 10%
                            elem.Position.y = elem.Parent.InnerSize.y - elem.Parent[(int)WindowChild.InputBox].Size.y - elem.Size.y;
                        },
                        Children = {},
                        Visible = false
                    },
                    new STextField { // InputBox
                        OverrideTab = true,

                        OnUpdateStyle = (elem) => {
                            elem.Size.x = elem.Parent.InnerSize.x;
                            elem.Position.x = 0;
                            elem.Position.y = elem.Parent.InnerSize.y - elem.Size.y;
                        },

                        OnKey = (self, is_down, key) => {
                            if (!is_down || key == KeyCode.Return || key == KeyCode.KeypadEnter) return;

                            switch(key) {
                            case KeyCode.Home:
                                self.MoveCursor(0);
                                break;
                            case KeyCode.Escape:
                            case KeyCode.F2:
                                Hide();
                                break;
                            case KeyCode.Tab:
                                Console.Instance.DoAutoComplete();
                                break;
                            case KeyCode.UpArrow:
                                Console.Instance.History.MoveUp();
                                self.MoveCursor(Console.Instance.History.Entry.Length);
                                break;
                            case KeyCode.DownArrow:
                                Console.Instance.History.MoveDown();
                                self.MoveCursor(Console.Instance.History.Entry.Length);
                                break;
                            default:
                                Console.Instance.History.LastEntry = self.Text;
                                Console.Instance.History.CurrentIndex = Console.Instance.History.LastIndex;
                                break;
                            }

                            self.Text = Console.Instance.History.Entry;
                        },

                        OnSubmit = (elem, text) => {
                            if (text.Trim().Length == 0) return;
                            Console.Instance.History.Push();
                            Console.Instance.ExecuteCommandAndPrintResult(text);
                        }
                    }
                }
            };
        }

        public override void OnShow() {
            Window[(int)WindowChild.InputBox].Focus();
        }

        public override void OnHide() {
            Console.Instance.DiscardAutocomplete(dont_reset_text: true);
            Console.Instance.History.LastEntry = Console.Instance.Text = "";
        }
    }
}

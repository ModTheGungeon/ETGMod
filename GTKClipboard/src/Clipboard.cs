using System;
namespace ETGMod.GUI {
    public class ClipboardFuckMono {
        public static String GetText() {
            Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            if (clipboard != null) {
                return clipboard.WaitForText();
            } else {
                return "";
            }
        }
    }
}

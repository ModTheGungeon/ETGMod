using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SGUI {
#if !SGUI_SHARED_EXT
    public static class SGUIExtensions {

        public static T AtOr<T>(this T[] a, int i, T or) {
            if (i < 0 || a.Length <= i) return or;
            return a[i];
        }

        public static void AddRange(this IDictionary to, IDictionary from) {
            foreach (DictionaryEntry entry in from) {
                to.Add(entry.Key, entry.Value);
            }
        }

        public static void ForEach<T>(this BindingList<T> list, Action<T> a) {
            for (int i = 0; i < list.Count; i++) {
                a(list[i]);
            }
        }
        public static void AddRange<T>(this BindingList<T> list, BindingList<T> other) {
            for (int i = 0; i < other.Count; i++) {
                list.Add(other[i]);
            }
        }

        public static int IndexOf(this object[] array, object elem) {
            for (int i = 0; i < array.Length; i++) {
                if (array[i] == elem) {
                    return i;
                }
            }
            return -1;
        }

    }
#endif
}

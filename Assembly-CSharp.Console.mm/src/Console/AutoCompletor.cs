using System;
using System.Collections.Generic;

namespace ETGMod.Console {
    public struct AutoCompletionEntry {
        public string Label { get; private set; }
        public string Content { get; private set; }

        public AutoCompletionEntry(string label, string content) {
            Label = label;
            Content = content;
        }
    }

    public class AutoCompletor {
        // full_str, current_arg, [return] list of entries
        private Func<string, string, List<AutoCompletionEntry>> _Func;

        public AutoCompletor(Func<string, string, List<AutoCompletionEntry>> f) {
            _Func = f;
        }
    }
}

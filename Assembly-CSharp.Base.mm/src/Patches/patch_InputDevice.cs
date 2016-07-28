using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InControl {
    class patch_InputDevice : InputDevice {

        public extern InputControl orig_GetControl(InputControlType inputControlType);
        public virtual InputControl GetControl(InputControlType inputControlType) {
            return orig_GetControl(inputControlType);
        }

        public patch_InputDevice(string name) : base(name) { }
    }
}


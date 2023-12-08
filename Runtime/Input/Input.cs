#if ENABLE_LEGACY_INPUT_MANAGER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modoium.Service {
    public static class Input {
        public static bool touchSupported {
            get {
                if (MDMInput.instance == null) { return UnityEngine.Input.touchSupported; }

                return MDMInput.instance.touchSupported;
            }
        }

        public static bool touchPressureSupported {
            get {
                if (MDMInput.instance == null) { return UnityEngine.Input.touchPressureSupported; }

                return MDMInput.instance.touchSupported;
            }
        }

        public static bool multiTouchEnabled {
            get {
                if (MDMInput.instance == null) { return UnityEngine.Input.multiTouchEnabled; }

                return MDMInput.instance.touchSupported;
            }
        }

        public static int touchCount {
            get {
                if (MDMInput.instance == null) { return UnityEngine.Input.touchCount; }

                return MDMInput.instance.touchCount;
            }
        }

        public static Touch GetTouch(int index) {
            if (MDMInput.instance == null) { return UnityEngine.Input.GetTouch(index); }

            return MDMInput.instance.GetTouch(index);
        }
    }
}

#endif

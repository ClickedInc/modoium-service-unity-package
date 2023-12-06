using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Modoium.Service {
    internal enum MDMInputDeviceID : byte {
        HeadTracker = 0,
        LeftHandTracker = 1,
        RightHandTracker = 2,
        XRController = 3,
        TouchScreen = 4
    }

    internal enum MDMTouchScreenControl : byte {
        TouchIndexStart = 0,
        ToucnIndexEnd = 9,
        TouchCount = 10
    }

    internal enum MDMTouchPhase : byte {
        Ended = 0,
        Cancelled,
        Stationary,
        Moved
    }

    internal class MDMInputProvider {
        private MDMService _owner;

        public MDMInputProvider(MDMService owner) {
            _owner = owner;
        }

        public void Update() {
            if (Application.isPlaying == false || ModoiumPlugin.isXR) { return; }

            updateLegacyInputManager();
            updateInputSystem();
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private const float TimeToWaitForEventSystem = 1.0f;

        private MDMInput _input;
        private float _remainingToCreateEventSystem = -1.0f;

        private void updateLegacyInputManager() {
            if (_input != null) { return; }

            var eventSystem = EventSystem.current;
            if (eventSystem == null) { 
                if (_remainingToCreateEventSystem < 0) {
                    _remainingToCreateEventSystem = TimeToWaitForEventSystem;
                }

                _remainingToCreateEventSystem -= Time.deltaTime;
                if (_remainingToCreateEventSystem >= 0) { return; }

                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            }
            _remainingToCreateEventSystem = TimeToWaitForEventSystem;
            
            _input = eventSystem.gameObject.GetComponent<MDMInput>();
            if (_input == null) {
                _input = eventSystem.gameObject.AddComponent<MDMInput>();
                _input.hideFlags = HideFlags.HideAndDontSave;

                _input.Configure(_owner);

                foreach (var inputModule in eventSystem.GetComponents<BaseInputModule>()) {
                    if ((inputModule is StandaloneInputModule) == false) { continue; }

                    inputModule.inputOverride = _input;
                }
            }


        }
#else
        private void updateLegacyInputManager() {}
#endif

#if ENABLE_INPUT_SYSTEM
        private void updateInputSystem() {

        }
#else
        private void updateInputSystem() {}
#endif
    }

///
#if ENABLE_LEGACY_INPUT_MANAGER
    public class MDMInput : BaseInput {
        private MDMService _service;
        private Touch[] _touchPool = new Touch[(int)MDMTouchScreenControl.TouchCount];
        private List<Touch> _touches = new List<Touch>();

        public override bool touchSupported => true;
        public override int touchCount => _touches.Count;

        public override Touch GetTouch(int index) {
            return _touches[index];
        }

        internal void Configure(MDMService service) {
            _service = service;

            foreach (var index in Enumerable.Range(0, _touchPool.Length)) {
                _touchPool[index] = new Touch();
            }
        }

        private void Update() {
            _touches.Clear();

            if ((_service?.remoteViewConnected) == false) { return; }

            ModoiumPlugin.UpdateInputFrame();

            for (byte control = 0; control < (byte)MDMTouchScreenControl.TouchCount; control++) {
                var touch = getTouch(control, _touches.Count);
                if (touch == null) { continue; }

                _touches.Add(touch.Value);
            }
        }

        private Touch? getTouch(byte control, int poolIndex) {
            var touchScreen = (byte)MDMInputDeviceID.TouchScreen;

            if (ModoiumPlugin.IsInputActive(touchScreen, control) == false &&
                ModoiumPlugin.GetInputDeactivated(touchScreen, control) == false) { return null; }

            var touch = _touchPool[poolIndex];
            touch.fingerId = control;
            touch.type = TouchType.Direct;
            touch.deltaTime = Time.deltaTime;

            ModoiumPlugin.GetInputTouch2D(touchScreen, control, out var position, out var state);
            touch.position = position;
            touch.rawPosition = position;

            if (ModoiumPlugin.GetInputActivated(touchScreen, control)) {
                touch.phase = TouchPhase.Began;
            }
            else {
                switch ((MDMTouchPhase)state) {
                    case MDMTouchPhase.Ended:
                        touch.phase = TouchPhase.Ended;
                        break;
                    case MDMTouchPhase.Cancelled:
                        touch.phase = TouchPhase.Canceled;
                        break;
                    case MDMTouchPhase.Stationary:
                        touch.phase = TouchPhase.Stationary;
                        break;
                    case MDMTouchPhase.Moved:
                        touch.phase = TouchPhase.Moved;
                        break;
                }
            }
            return touch;
        }
    }
#endif
}

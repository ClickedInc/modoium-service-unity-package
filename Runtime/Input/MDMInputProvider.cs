using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Modoium.Service {
    public class MDMInput : BaseInput {
        public static MDMInput instance { get; private set; }

        private MDMInputProvider _inputProvider;

        public override bool touchSupported => true;
        public override int touchCount => _inputProvider?.touchCount ?? 0;
        public override Touch GetTouch(int index) => _inputProvider.GetTouch(index).Value;

        internal void Configure(MDMInputProvider inputProvider) {
            _inputProvider = inputProvider;
            
            instance = this;
        }
    }

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
        private Touch[] _touchPool = new Touch[(int)MDMTouchScreenControl.TouchCount];
        private List<Touch> _touches = new List<Touch>();

        internal int touchCount => _touches.Count;

        public MDMInputProvider(MDMService owner) {
            _owner = owner;

            foreach (var index in Enumerable.Range(0, _touchPool.Length)) {
                _touchPool[index] = new Touch();
            }
        }

        public void Update() {
            if (Application.isPlaying == false || ModoiumPlugin.isXR) { return; }

            updateInputFrame();

            updateLegacyInputManager();
            updateInputSystem();
        }

        private void updateInputFrame() {
            ModoiumPlugin.UpdateInputFrame();
            updateTouches();
        }

        public Touch? GetTouch(int index) {
            if (index < 0 || index >= _touches.Count) { return null; }

            return _touches[index];
        }

        private void updateTouches() {
            _touches.Clear();
            if (_owner.remoteViewConnected == false) { return; }

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

                eventSystem = createEventSystem();
            }
            _remainingToCreateEventSystem = TimeToWaitForEventSystem;
            
            _input = eventSystem.gameObject.GetComponent<MDMInput>();
            if (_input == null) {
                _input = eventSystem.gameObject.AddComponent<MDMInput>();
                _input.hideFlags = HideFlags.HideAndDontSave;

                _input.Configure(this);

                foreach (var inputModule in eventSystem.GetComponents<BaseInputModule>()) {
                    inputModule.inputOverride = _input;
                }
            }
        }

        private EventSystem createEventSystem() {
            var go = new GameObject("EventSystem") {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
            };
            var eventSystem = go.AddComponent<Modoium.Service.EventSystem>();
            go.AddComponent<StandaloneInputModule>();

            return eventSystem;
        }

#if ENABLE_INPUT_SYSTEM
        private MDMTouchScreen _touchscreen;

        private void updateInputSystem() {
            if (_touchscreen == null) {
                _touchscreen = createTouchscreen();
            }
            _touchscreen.EnqueueInputEvents();
        }

        private MDMTouchScreen createTouchscreen() {
            var go = new GameObject("ModoiumTouchscreen") {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
            };
            var touchscreen = go.AddComponent<MDMTouchScreen>();
            Object.DontDestroyOnLoad(go);
    
            touchscreen.Configure(this);        
            return touchscreen;
        }
#else
        private void updateInputSystem() {}
#endif
    }
}

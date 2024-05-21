using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Modoium.Service.Editor {
    [InitializeOnLoad]
    internal class MDMEditorService : MDMService.IApplication {
        private static MDMEditorService _instance;
        private static bool _started;
        private static bool _playRequested;

        public static MDMService service => _instance?._service;
        public static MDMEditorStatusMonitor statusMonitor => _instance?._statusMonitor;

        static MDMEditorService() {
            EditorApplication.update += EditorUpdate;
            EditorApplication.playModeStateChanged += EditorPlayModeStateChanged;
            EditorApplication.quitting += EditorOnDestroy;

            _instance = new MDMEditorService();
        }

        static void EditorUpdate() {
            if (_started == false) {
                _instance.Start();
                _started = true;
            }

            _instance.Update();

            if (_playRequested) {
                _instance.play();
                _playRequested = false;
            }
        }

        static void EditorPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.EnteredPlayMode) {
                _playRequested = true;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode) {
                _instance.stop();
            }
        }

        static void EditorOnDestroy() {
            _instance.OnDestroy();
        }

        private MDMService _service;
        private MDMEditorStatusMonitor _statusMonitor;

        private MDMEditorService() {
            _service = new MDMService(this);
            _statusMonitor = new MDMEditorStatusMonitor(_service);
        }

        private void Start() {
            _service.Startup();
        }

        private void Update() {
            _service.Update();
            _statusMonitor.Update();
        }

        private void OnDestroy() {
            _service.Shutdown();
        }

        private void play() {
            _service.Play();
        }

        private void stop() {
            _service.Stop();
        }

        // implements MDMService.IApplication
        bool MDMService.IApplication.isPlaying => Application.isPlaying;
    }
}

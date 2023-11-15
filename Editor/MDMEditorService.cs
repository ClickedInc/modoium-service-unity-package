using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Modoium.Service.Editor {
    [InitializeOnLoad]
    internal class MDMEditorService {
        private static MDMEditorService _instance;
        private static bool _started;

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
        }

        static void EditorPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingEditMode) {
                _instance.play();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode) {
                _instance.stop();
            }
        }

        static void EditorOnDestroy() {
            _instance.OnDestroy();
        }

        private MDMService _service;

        private MDMEditorService() {
            _service = new MDMService();
        }

        private void Start() {
            _service.Startup();
        }

        private void Update() {
            _service.Update();
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
    }
}

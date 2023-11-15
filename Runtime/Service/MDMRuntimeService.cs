using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Modoium.Service {
    public class MDMRuntimeService : MonoBehaviour {
        public static MDMRuntimeService instance { get; private set; }

        internal static async void LoadOnce() {
            if (instance != null) { return; }

            // NOTE: wait until the first scene is loaded to avoid AXRServer from being destroyed.
            if (Application.isEditor == false && SceneManager.GetActiveScene().isLoaded == false) {
                await Task.Yield();
            }

            var go = new GameObject("ModoiumService");
            go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            DontDestroyOnLoad(go);

            instance = go.AddComponent<MDMRuntimeService>();
        }

        private MDMService _service;

        private void Awake() {
            _service = new MDMService();
        }

        private void Start() {
            _service.Startup();
        }

        private void Update() {
            _service.Update();
        }

        private void OnApplicationQuit() {
            if (instance != null) {
                Destroy(instance.gameObject);
            }
        }

        private void OnDestroy() {
            _service.Shutdown();
            
            instance = null;
        }
    }
}

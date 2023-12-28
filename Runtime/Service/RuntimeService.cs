using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Modoium.Service {
    public class RuntimeService : MonoBehaviour, MDMService.IApplication {
        public static RuntimeService instance { get; private set; }

        private MDMService _service;

        private void Awake() {
            if (instance != null) {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (Application.isEditor) { return; }
            
            _service = new MDMService(this);
        }

        private void Start() {
            if (Application.isEditor) { return; }

            _service.Startup();
        }

        private void Update() {
            if (Application.isEditor) { return; }

            _service.Update();
        }

        private void OnApplicationQuit() {
            if (Application.isEditor) { return; }
            
            if (instance != null) {
                Destroy(instance.gameObject);
            }
        }

        private void OnDestroy() {
            if (Application.isEditor) { return; }

            _service.Shutdown();
            
            instance = null;
        }

        // implements MDMService.IApplication
        bool MDMService.IApplication.isPlaying => true;
    }
}

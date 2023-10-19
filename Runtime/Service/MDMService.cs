using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Plastic.Newtonsoft.Json;

namespace Modoium.Service {
    public class MDMService : MonoBehaviour {
        private const float MaxFrameRate = 120f;

        public static MDMService instance { get; private set; }

        internal static async void LoadOnce() {
            if (instance != null) { return; }

            // NOTE: wait until the first scene is loaded to avoid AXRServer from being destroyed.
            if (Application.isEditor == false && SceneManager.GetActiveScene().isLoaded == false) {
                await Task.Yield();
            }

            var go = new GameObject("ModoiumService");
            go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            DontDestroyOnLoad(go);

            instance = go.AddComponent<MDMService>();
        }

        private MDMMessageDispatcher _messageDispatcher;
        private MDMAudioListener _audioListener;

        internal void Reconfigure(ModoiumSettings settings) {
            configure(settings);
        }

        private void Awake() {
            _messageDispatcher = new MDMMessageDispatcher();
            _messageDispatcher.onMessageReceived += onMDMMessageReceived;
        }

        private void Start() {
            var settings = ModoiumSettings.instance;

            configure(settings);

            ModoiumPlugin.StartupService(settings.serviceName, settings.serviceUserdata);
        }

        private void Update() {
            ensureAudioListenerConfigured();
            _messageDispatcher.Dispatch();
        }

        private void OnApplicationQuit() {
            if (instance != null) {
                Destroy(instance.gameObject);
            }
        }

        private void OnDestroy() {
            ModoiumPlugin.ShutdownService();
            _messageDispatcher.onMessageReceived -= onMDMMessageReceived;

            instance = null;
        }

        private void configure(ModoiumSettings settings) {
            ModoiumPlugin.Configure(settings.propIdleFrameRate,
                                    MaxFrameRate,
                                    AudioSettings.outputSampleRate,
                                    (int)settings.propDisplayTextureColorSpaceHint,
                                    settings.propCpuReadableEncodeBuffer,
                                    (int)settings.propCodecs,
                                    (int)settings.propEncodingPreset,
                                    (int)settings.propEncodingQuality);
        }

        private void ensureAudioListenerConfigured() {
            if (_audioListener != null) { return; }

            var audioListener = FindObjectOfType<AudioListener>();
            if (audioListener == null) { return; }

            _audioListener = audioListener.gameObject.GetComponent<MDMAudioListener>();
            if (_audioListener == null) {
                _audioListener = audioListener.gameObject.AddComponent<MDMAudioListener>();
            }
        }

        private void onMDMMessageReceived(MDMMessage message) {
            if (message is MDMMessageCoreConnected coreConnected) {
                onCoreConnected(coreConnected);
            }
            else if (message is MDMMessageCoreDisconnected coreDisconnected) {
                onCoreDisconnected(coreDisconnected);
            }
            else if (message is MDMMessageAxrOpenFailed axrOpenFailed) {
                onAxrOpenFailed(axrOpenFailed);
            }
            else if (message is MDMMessageAxrInitiated axrInitiated) {
                onAxrInitiated(axrInitiated);
            }
            else if (message is MDMMessageAxrEstablished axrEstablished) {
                onAxrEstablished(axrEstablished);
            }
            else if (message is MDMMessageAxrFinished axrFinished) {
                onAxrFinished(axrFinished);
            }
        }

        private void onCoreConnected(MDMMessageCoreConnected message) {
            Debug.Log("[modoium] core connected");
        }

        private void onCoreDisconnected(MDMMessageCoreDisconnected message) {
            Debug.Log($"[modoium] core disconnected: {message.statusCode}: {message.closeReason}");
        }

        private void onAxrOpenFailed(MDMMessageAxrOpenFailed message) {
            Debug.Log($"[modoium] axr open failed: {message.code}");
        }

        private void onAxrInitiated(MDMMessageAxrInitiated message) {
            Debug.Log($"[modoium] axr initiated: {JsonConvert.SerializeObject(message.appData)}");

            ModoiumPlugin.AcceptInitiation(message.appData);
        }

        private void onAxrEstablished(MDMMessageAxrEstablished message) {
            Debug.Log($"[modoium] axr established");
        }

        private void onAxrFinished(MDMMessageAxrFinished message) {
            Debug.Log($"[modoium] axr finished: {message.code}: {message.reason}");
        }
    }
}

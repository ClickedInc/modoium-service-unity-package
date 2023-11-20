using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;

namespace Modoium.Service {
    internal class MDMService {
        public interface IApplication {
            bool isPlaying { get; }
        }

        private const float MaxFrameRate = 120f;

        private IApplication _app;
        private MDMMessageDispatcher _messageDispatcher;
        private MDMAudioListener _audioListener;
        private MDMAppData _clientAppData;
        private MDMVideoOffer _remoteViewConfig;
        private MDMDisplayRenderer _displayRenderer;

        private bool remoteViewConnected => _remoteViewConfig != null;

        public MDMService(IApplication app) {
            _app = app;
            _messageDispatcher = new MDMMessageDispatcher();
            _displayRenderer = new MDMDisplayRenderer(app as MonoBehaviour);

            _messageDispatcher.onMessageReceived += onMDMMessageReceived;
        }

        public void Startup() {
            Debug.Log($"[modoium] startup");
            var settings = ModoiumSettings.instance;

            ModoiumPlugin.StartupService(settings.serviceName, settings.serviceUserdata);
        }

        public void Shutdown() {
            Debug.Log($"[modoium] shutdown");
            ModoiumPlugin.ShutdownService();

            _messageDispatcher.onMessageReceived -= onMDMMessageReceived;
        }

        public void Play() {
            Debug.Log($"[modoium] play: removeViewConnected = {remoteViewConnected}");
            if (remoteViewConnected == false) { return; } 

            _displayRenderer.Start(_remoteViewConfig);
            requestPlay();
        }

        public void Stop() {
            Debug.Log($"[modoium] stop: removeViewConnected = {remoteViewConnected}");
            if (remoteViewConnected == false) { return; } 

            requestStop();
            _displayRenderer.Stop();
        }

        public void Update() {
            _messageDispatcher.Dispatch();

            updateGameView();

            if (Application.isPlaying) { 
                ensureAudioListenerConfigured();
            }
        }

        private void updateGameView() {

        }

        private void ensureAudioListenerConfigured() {
            if (_audioListener != null) { return; }

            var audioListener = Object.FindObjectOfType<AudioListener>();
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
            else if (message is MDMMessageAxrClientAppData axrClientAppData) {
                onAxrClientAppData(axrClientAppData);
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
            _clientAppData = message.appData;
            _remoteViewConfig = message.appData.videoOffer;
        }

        private void onAxrEstablished(MDMMessageAxrEstablished message) {
            // TODO adjust game view size wrt remote view size

            if (_app.isPlaying) { 
                Play();
            }
        }

        private void onAxrFinished(MDMMessageAxrFinished message) {
            _displayRenderer.Stop();

            _clientAppData = null;
            _remoteViewConfig = null;
        }

        private void onAxrClientAppData(MDMMessageAxrClientAppData message) {
            _clientAppData = message.appData;
            _remoteViewConfig = message.appData.videoOffer;
        }

        private void requestPlay() {
            Debug.Assert(_clientAppData != null);
            var settings = ModoiumSettings.instance;

            ModoiumPlugin.Play(JsonConvert.SerializeObject(_clientAppData),
                               settings.idleFrameRate,
                               MaxFrameRate,
                               AudioSettings.outputSampleRate,
                               (int)settings.displayTextureColorSpaceHint,
                               (int)settings.codecs,
                               (int)settings.encodingPreset,
                               (int)settings.encodingQuality);
        }

        private void requestStop() {
            ModoiumPlugin.Stop();
        }
    }
}

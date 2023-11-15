using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;

namespace Modoium.Service {
    internal class MDMService {
        private const float MaxFrameRate = 120f;

        private MDMMessageDispatcher _messageDispatcher;
        private MDMAudioListener _audioListener;
        private MDMAppData _clientAppData;
        private MDMVideoOffer _remoteViewConfig;
        private bool _playing;

        private bool remoteViewConnected => _remoteViewConfig != null;

        public MDMService() {
            _messageDispatcher = new MDMMessageDispatcher();
            _messageDispatcher.onMessageReceived += onMDMMessageReceived;
        }

        public void Startup() {
            var settings = ModoiumSettings.instance;

            ModoiumPlugin.StartupService(settings.serviceName, settings.serviceUserdata);
        }

        public void Shutdown() {
            ModoiumPlugin.ShutdownService();

            _messageDispatcher.onMessageReceived -= onMDMMessageReceived;
        }

        public void Play() {
            if (_playing) { return; }
            _playing = true;

            if (remoteViewConnected) {
                requestPlay();
            }
        }

        public void Stop() {
            if (_playing == false) { return; }
            _playing = false;

            if (remoteViewConnected) {
                requestStop();
            }
        }

        public void Update() {
            _messageDispatcher.Dispatch();

            if (Application.isPlaying) { 
                ensureAudioListenerConfigured();
            }
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

            if (_playing) { 
                requestPlay();
            }
        }

        private void onAxrFinished(MDMMessageAxrFinished message) {
            Debug.Log($"[modoium] axr finished: {message.code}: {message.reason}");

            _clientAppData = null;
            _remoteViewConfig = null;
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

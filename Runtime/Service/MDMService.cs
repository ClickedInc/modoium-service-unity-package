using System;
using System.Collections;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        private MDMDisplayRenderer _displayRenderer;
        private MDMGameViewConfigurator _gameViewConfigurator;
        private MDMInputProvider _inputProvider;

        private bool coreConnected { get; set; } = false;

        internal MDMVideoDesc remoteViewDesc { get; private set; }
        internal bool remoteViewConnected => coreConnected && remoteViewDesc != null;

        public MDMService(IApplication app) {
            _app = app;
            _messageDispatcher = new MDMMessageDispatcher();
            _displayRenderer = new MDMDisplayRenderer(app as MonoBehaviour);
            _gameViewConfigurator = new MDMGameViewConfigurator(this);
            _inputProvider = new MDMInputProvider(this);
 
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
            if (remoteViewConnected == false) { return; } 

            _displayRenderer.Start(remoteViewDesc);
            requestPlay();
        }

        public void Stop() {
            if (remoteViewConnected == false) { return; } 

            requestStop();
            _displayRenderer.Stop();
        }

        public void Update() {
            if (Application.isPlaying) { 
                ensureAudioListenerConfigured();
            }

            _messageDispatcher.Dispatch();
            _inputProvider.Update();

            if (coreConnected) {
                _gameViewConfigurator.Update();
            }
        }

        private void ensureAudioListenerConfigured() {
            if (_audioListener != null) { return; }

            var audioListener = UnityEngine.Object.FindObjectOfType<AudioListener>();
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
            else if (message is MDMMessageCoreConnectionFailed coreConnectionFailed) {
                onCoreConnectionFailed(coreConnectionFailed);
            }
            else if (message is MDMMessageCoreDisconnected coreDisconnected) {
                onCoreDisconnected(coreDisconnected);
            }
            else if (message is MDMMessageSessionInitiated sessionInitiated) {
                onSessionInitiated(sessionInitiated);
            }
            else if (message is MDMMessageSessionCancelled sessionCancelled) {
                onSessionCancelled(sessionCancelled);
            }
            else if (message is MDMMessageAmpOpened ampOpened) {
                onAmpOpened(ampOpened);
            }
            else if (message is MDMMessageAmpClosed ampClosed) {
                onAmpClosed(ampClosed);
            }
            else if (message is MDMMessageClientAppData clientAppData) {
                onClientAppData(clientAppData);
            }
        }

        private void onCoreConnected(MDMMessageCoreConnected message) {
            coreConnected = true;
        }

        private void onCoreConnectionFailed(MDMMessageCoreConnectionFailed message) {
            var failureCode = (MDMFailureCode)message.code;
            if (failureCode != MDMFailureCode.CoreNotFound) {
                Debug.LogWarning($"[modoium] core connection failed: {failureCode} (status code {message.statusCode}): {message.reason}");
            }

            tryReopenService();
        }

        private void onCoreDisconnected(MDMMessageCoreDisconnected message) {
            coreConnected = false;
            clearClientAppData();

            tryReopenService();
        }

        private void onSessionInitiated(MDMMessageSessionInitiated message) {
            setClientAppData(message.appData);
        }
        
        private void onSessionCancelled(MDMMessageSessionCancelled message) {
            Debug.LogWarning($"[modoium] session cancelled: reason = {message.reason}");

            _displayRenderer.Stop(); 
            clearClientAppData();
        }

        private void onAmpOpened(MDMMessageAmpOpened message) {
            if (_app.isPlaying) { 
                Play();
            }
        }

        private void onAmpClosed(MDMMessageAmpClosed message) {
            _displayRenderer.Stop(); 

            clearClientAppData();
        }

        private void onClientAppData(MDMMessageClientAppData message) {
            setClientAppData(message.appData);
        }
        
        private async void tryReopenService() {
            var settings = ModoiumSettings.instance;

            await Task.Delay(TimeSpan.FromSeconds(0.2));

            ModoiumPlugin.ReopenService(settings.serviceName, settings.serviceUserdata);
        }

        private void setClientAppData(MDMAppData appData) {
            _clientAppData = appData;
            remoteViewDesc = appData.videoDesc;
        }

        private void clearClientAppData() {
            _clientAppData = null;
            remoteViewDesc = null;
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

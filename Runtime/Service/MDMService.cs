using System;
using System.Collections;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using Unity.Plastic.Newtonsoft.Json;
#else
using Newtonsoft.Json;
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
        private MDMServiceConfigurator _serviceConfigurator;
        private MDMDisplayRenderer _displayRenderer;
        private MDMDisplayConfigurator _displayConfigurator;
        private MDMInputProvider _inputProvider;
        private MDMServiceAvailability _availability = MDMServiceAvailability.Unspecified;
        private float _timeToReopenService = -1f;

        private bool coreConnected => ModoiumPlugin.GetServiceState() == MDMServiceState.Ready;

        internal MDMVideoDesc remoteViewDesc { get; private set; }
        internal MDMInputDesc remoteInputDesc { get; private set; }
        internal bool remoteViewConnected => coreConnected && remoteViewDesc != null;
        internal bool isAppPlaying => _app.isPlaying;

        public MDMService(IApplication app) {
            _app = app;
            _messageDispatcher = new MDMMessageDispatcher();
            _serviceConfigurator = new MDMServiceConfigurator();
            _displayRenderer = new MDMDisplayRenderer(app as MonoBehaviour);
            _displayConfigurator = new MDMDisplayConfigurator(this);
            _inputProvider = new MDMInputProvider(this);
 
            _messageDispatcher.onMessageReceived += onMDMMessageReceived;
        }

        public async void Startup() {
            _availability = await ModoiumPlugin.CheckServiceAvailability();
            if (_availability != MDMServiceAvailability.Available) { return; }

            var settings = ModoiumSettings.instance;

            ModoiumPlugin.SetBitrateInMbps(settings.bitrate);
            ModoiumPlugin.StartupService(settings.serviceName, settings.serviceUserdata);

            _displayConfigurator.OnPostFirstMessageDispatch(_messageDispatcher.Dispatch());
        }

        public void Shutdown() {
            if (_availability != MDMServiceAvailability.Available) { return; }

            ModoiumPlugin.ShutdownService();

            _messageDispatcher.onMessageReceived -= onMDMMessageReceived;
        }

        public void Play() {
            if (remoteViewConnected == false) { return; } 

            _displayRenderer.Start(_inputProvider, remoteViewDesc);
            requestPlay();
        }

        public void Stop() {
            if (remoteViewConnected == false) { return; } 

            requestStop();
            _displayRenderer.Stop();
        }

        public void Update() {
            if (_availability != MDMServiceAvailability.Available) { return; }

            if (Application.isPlaying) { 
                ensureAudioListenerConfigured();
            }

            _messageDispatcher.Dispatch();

            updateReopenService();

            if (coreConnected) {
                _displayConfigurator.Update();
            }

            ModoiumPlugin.UpdateService();
            _serviceConfigurator.Update();
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

        private void updateReopenService() {
            if (ModoiumPlugin.GetServiceState() != MDMServiceState.Disconnected) {
                _timeToReopenService = -1f;
                return;
            }

            if (_timeToReopenService < 0f) { 
                _timeToReopenService = Time.realtimeSinceStartup + 0.5f;
            }
            if (Time.realtimeSinceStartup < _timeToReopenService) { return; }

            var settings = ModoiumSettings.instance;
            ModoiumPlugin.ReopenService(settings.serviceName, settings.serviceUserdata);

            _timeToReopenService = -1f;
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
            // do nothing
        }

        private void onCoreConnectionFailed(MDMMessageCoreConnectionFailed message) {
            var failureCode = (MDMFailureCode)message.code;
            if (failureCode != MDMFailureCode.CoreNotFound) {
                Debug.LogWarning($"[modoium] core connection failed: {failureCode} (status code {message.statusCode}): {message.reason}");
            }
        }

        private void onCoreDisconnected(MDMMessageCoreDisconnected message) {
            clearClientAppData();
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

        private void setClientAppData(MDMAppData appData) {
            _clientAppData = appData;
            remoteViewDesc = appData.videoDesc;
            remoteInputDesc = appData.inputDesc;
        }

        private void clearClientAppData() {
            _clientAppData = null;
            remoteViewDesc = null;
            remoteInputDesc = null;
        }

        private void requestPlay() {
            Debug.Assert(_clientAppData != null);
            var settings = ModoiumSettings.instance;

            var isXR = ModoiumPlugin.isXR;
            if ((_clientAppData.videoDesc is MDMStereoVideoDesc) != isXR) { 
                Debug.LogWarning($"[Modoium Remote] the connected client does not support {(isXR ? "XR" : "non-XR")} content.");
                return; 
            }

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

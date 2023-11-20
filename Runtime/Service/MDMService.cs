using System;
using System.Collections;
using System.Collections.Generic;
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
        private MDMVideoOffer _remoteViewConfig;
        private MDMDisplayRenderer _displayRenderer;
        private DisplayConfigurator _displayConfigurator;

        private bool remoteViewConnected => _remoteViewConfig != null;

        public MDMService(IApplication app) {
            _app = app;
            _messageDispatcher = new MDMMessageDispatcher();
            _displayRenderer = new MDMDisplayRenderer(app as MonoBehaviour);
            _displayConfigurator = new DisplayConfigurator(this);

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

            _displayRenderer.Start(_remoteViewConfig);
            requestPlay();
        }

        public void Stop() {
            if (remoteViewConnected == false) { return; } 

            requestStop();
            _displayRenderer.Stop();
        }

        public void Update() {
            _messageDispatcher.Dispatch();

            _displayConfigurator.Update();

            if (Application.isPlaying) { 
                ensureAudioListenerConfigured();
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

        private class DisplayConfigurator {
            private MDMService _owner;
            private bool _syncedToRemote;
            private object _originalSize;

            public DisplayConfigurator(MDMService owner) {
                _owner = owner;

                init();
            }

#if UNITY_EDITOR
            private const string RemoteSizeName = "Modoium Remote";

            private Type _typeGameView;
            private MethodInfo _methodGetMainPlayModeView;

            private EditorWindow mainGameView {
                get {
                    var res = _methodGetMainPlayModeView.Invoke(null, null);
                    return (EditorWindow)res;
                }   
            }

            private object currentGroup {
                get {
                    var T = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
                    var sizes = T.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                    var instance = sizes.GetValue(null, new object[] { });

                    var prop = instance.GetType().GetProperty("currentGroup", BindingFlags.Public | BindingFlags.Instance);
                    return prop.GetValue(instance, new object[] { });
                }
            }

            private object currentSize {
                get {
                    var gameView = mainGameView;
                    if (gameView == null) { return new [] { 0, 0 }; }

                    var prop = gameView.GetType().GetProperty("currentGameViewSize", BindingFlags.NonPublic | BindingFlags.Instance);
                    return prop.GetValue(gameView, new object[] { });
                }
            }

            public void Update() {
                if (ModoiumPlugin.isXR ||
                    _owner.remoteViewConnected == _syncedToRemote) { return; }

                if (_owner.remoteViewConnected) {
                    _originalSize = currentSize;

                    syncGameViewToRemote(_owner._remoteViewConfig.width, _owner._remoteViewConfig.height);
                }
                else {
                    selectSize(_originalSize);
                }
                _syncedToRemote = _owner.remoteViewConnected;
            }

            private void init() {
                if (ModoiumPlugin.isXR) { return; }

                _typeGameView = Type.GetType("UnityEditor.PlayModeView,UnityEditor");
                _methodGetMainPlayModeView = _typeGameView.GetMethod("GetMainPlayModeView", BindingFlags.NonPublic | BindingFlags.Static);
            }

            private void syncGameViewToRemote(int width, int height) {
                var size = findRemoteSize();
                if (size != null) {
                    size.GetType().GetField("m_Width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(size, width);
                    size.GetType().GetField("m_Height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(size, height);

                    size.GetType().GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(size, new object[] { });
                }
                else {
                    size = makeRemoteSize(width, height);

                    var group = currentGroup;
                    var method = group.GetType().GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
                    method.Invoke(group, new[] { size });
                }

                selectSize(size);
            }

            private void selectSize(object size) {
                var index = indexOfSize(size); 
                var gameView = mainGameView; 
                if (gameView == null) { return; }

                var method = gameView.GetType().GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance);
                method.Invoke(gameView, new[] { index, size });
            }

            private int indexOfSize(object size) {
                var group = currentGroup;
                var method = group.GetType().GetMethod("IndexOf", BindingFlags.Public | BindingFlags.Instance);
                var index = (int)method.Invoke(group, new object[] { size });

                var builtinList = group.GetType().GetField("m_Builtin", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);
                method = builtinList.GetType().GetMethod("Contains");
                if ((bool)method.Invoke(builtinList, new[] { size })) { return index; }

                method = group.GetType().GetMethod("GetBuiltinCount");
                index += (int)method.Invoke(group, new object[] { });

                return index;
            }

            private object findRemoteSize() {
                var group = currentGroup;
                var customs = group.GetType().GetField("m_Custom", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);

                var iter = (IEnumerator)customs.GetType().GetMethod("GetEnumerator").Invoke(customs, new object[] {});
                while (iter.MoveNext()) {
                    var label = (string)iter.Current.GetType().GetField("m_BaseText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(iter.Current);
                    if (label == RemoteSizeName) { 
                        return iter.Current; 
                    }
                }
                return null;
            }

            private object makeRemoteSize(int width, int height) {
                var T = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
                var tt = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

                var c = T.GetConstructor(new[] { tt, typeof(int), typeof(int), typeof(string) });
                var size = c.Invoke(new object[] { 1, width, height, RemoteSizeName });
                return size;
            }
#else
            public void Update() {}
            private void init() {}
#endif
        }
    }
}

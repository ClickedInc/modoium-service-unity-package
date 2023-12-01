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
        private MDMVideoDesc _remoteViewDesc;
        private MDMDisplayRenderer _displayRenderer;
        private DisplayConfigurator _displayConfigurator;

        private bool coreConnected { get; set; } = false;
        private bool remoteViewConnected => coreConnected && _remoteViewDesc != null;

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

            _displayRenderer.Start(_remoteViewDesc);
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

            if (coreConnected) {
                _displayConfigurator.Update();
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
            Debug.Log($"[modoium] session cancelled: reason = {message.reason}");

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
            _remoteViewDesc = appData.videoDesc;
        }

        private void clearClientAppData() {
            _clientAppData = null;
            _remoteViewDesc = null;
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
            private const string KeyOriginalSizeLabel = "com.modoium.remote.originalSizeLabel";

            private MDMService _owner;
            private bool _syncedToRemote;
            private string _originalSizeLabelCached;

            private string originalSizeLabel {
                get {
                    _originalSizeLabelCached = PlayerPrefs.GetString(KeyOriginalSizeLabel, "");
                    return _originalSizeLabelCached;
                }
                set {
                    if (value == _originalSizeLabelCached) { return; }

                    _originalSizeLabelCached = value;

                    PlayerPrefs.SetString(KeyOriginalSizeLabel, value);
                    PlayerPrefs.Save();
                }
            }

            public DisplayConfigurator(MDMService owner) {
                _owner = owner;

                init();
            }

#if UNITY_EDITOR
            private const string RemoteSizeLabel = "Modoium Remote";

            private Type _typeGameView;
            private MethodInfo _methodGetMainPlayModeView;
            private Vector2Int _lastRemoteViewSize;

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
                if (ModoiumPlugin.isXR) { return; }

                if (_owner.remoteViewConnected) {
                    var remoteView = _owner._remoteViewDesc;

                    if (_syncedToRemote) {
                        if (remoteView.width == _lastRemoteViewSize.x &&
                            remoteView.height == _lastRemoteViewSize.y) { return; }

                        syncGameViewToRemote(remoteView.width, remoteView.height);

                        _lastRemoteViewSize.x = remoteView.width;
                        _lastRemoteViewSize.y = remoteView.height;
                    }
                    else {
                        var currentSizeLabel = sizeLabel(currentSize);
                        
                        if (currentSizeLabel != RemoteSizeLabel) { 
                            originalSizeLabel = currentSizeLabel;
                        }

                        syncGameViewToRemote(remoteView.width, remoteView.height);

                        _lastRemoteViewSize.x = remoteView.width;
                        _lastRemoteViewSize.y = remoteView.height;
                    }
                }
                else if (_syncedToRemote) {
                    selectSize(findSizeFromLabel(originalSizeLabel));

                    _lastRemoteViewSize.x = 0;
                    _lastRemoteViewSize.y = 0;
                }

                _syncedToRemote = _owner.remoteViewConnected;
            }

            private void init() {
                if (ModoiumPlugin.isXR) { return; }

                _typeGameView = Type.GetType("UnityEditor.PlayModeView,UnityEditor");
                _methodGetMainPlayModeView = _typeGameView.GetMethod("GetMainPlayModeView", BindingFlags.NonPublic | BindingFlags.Static);
            }

            private void syncGameViewToRemote(int width, int height) {
                if (width <= 0 || height <= 0) { return; }

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
                if (size == null) { return; }

                var index = sizeIndex(size); 
                var gameView = mainGameView; 
                if (gameView == null) { return; }

                var method = gameView.GetType().GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance);
                method.Invoke(gameView, new[] { index, size });
            }

            private int sizeIndex(object size) {
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

            private object findRemoteSize() => findSizeFromLabel(RemoteSizeLabel, false);

            private object findSizeFromLabel(string label, bool includeBuiltin = true) {
                var group = currentGroup;
                var customs = group.GetType().GetField("m_Custom", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);

                var found = findSizeInListFromLabel(customs, label);
                if (found != null || includeBuiltin == false) { return found; }

                var builtins = group.GetType().GetField("m_Builtin", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(group);
                return findSizeInListFromLabel(builtins, label);
            }

            private object findSizeInListFromLabel(object list, string label) {
                if (list == null) { return null; }

                var iter = (IEnumerator)list.GetType().GetMethod("GetEnumerator").Invoke(list, new object[] {});
                while (iter.MoveNext()) {
                    if (sizeLabel(iter.Current) == label) { 
                        return iter.Current; 
                    }
                }
                return null;
            }

            private string sizeLabel(object size) => (string)size.GetType().GetField("m_BaseText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(size);

            private object makeRemoteSize(int width, int height) {
                var T = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
                var tt = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

                var c = T.GetConstructor(new[] { tt, typeof(int), typeof(int), typeof(string) });
                var size = c.Invoke(new object[] { 1, width, height, RemoteSizeLabel });
                return size;
            }
#else
            public void Update() {}
            private void init() {}
#endif
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Threading.Tasks;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Modoium.Service {
    internal class MDMDisplayConfigurator {
        private const string KeyOriginalSizeLabel = "com.modoium.remote.originalSizeLabel";

        private MDMService _owner;
        private bool _remoteConnected;
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

        public MDMDisplayConfigurator(MDMService owner) {
            _owner = owner;

            init();
        }

#if UNITY_EDITOR
        private const string RemoteSizeLabel = "Modoium Remote";

        private Type _typeGameView;
        private MethodInfo _methodGetMainPlayModeView;
        private Vector2Int _lastRemoteViewSize;
        private bool _isFirstUpdate = true;

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

        public void OnPostFirstMessageDispatch(MDMMessageDispatcher.Event evt) {
            // NOTE: must handle the first update due to reconstruction on editor play/stop

            switch (evt) {
                case MDMMessageDispatcher.Event.None:
                    //restore states on reconstruction
                    _remoteConnected = _owner.remoteViewConnected;
                    if (_remoteConnected) {
                        var remoteView = _owner.remoteViewDesc;
                        _lastRemoteViewSize.x = remoteView.contentWidth;
                        _lastRemoteViewSize.y = remoteView.contentHeight;
                    }
                    break;
                case MDMMessageDispatcher.Event.AmpClosed:
                    _remoteConnected = true;
                    break;
            }
        }

        public void Update() {
            if (ModoiumPlugin.isXR) { return; }

            if (_owner.remoteViewConnected) {
                if (_remoteConnected) {
                    handleRemoteViewSizeChange();
                }
                else {
                    setToRemoteViewSize();
                }
            }
            else if (_remoteConnected) {
                returnToOriginalSize();
            }

            _remoteConnected = _owner.remoteViewConnected;
        }

        private void handleRemoteViewSizeChange() {
            if (_owner.isAppPlaying) { return; }

            var remoteView = _owner.remoteViewDesc;
            if (remoteView.contentWidth == _lastRemoteViewSize.x &&
                remoteView.contentHeight == _lastRemoteViewSize.y) { return; }

            var currentSizeLabel = sizeLabel(currentSize);
            syncGameViewToRemote(remoteView.contentWidth, remoteView.contentHeight, currentSizeLabel == RemoteSizeLabel);

            _lastRemoteViewSize.x = remoteView.contentWidth;
            _lastRemoteViewSize.y = remoteView.contentHeight;
        }

        private void setToRemoteViewSize() {
            var remoteView = _owner.remoteViewDesc;

            var currentSizeLabel = sizeLabel(currentSize);
            if (currentSizeLabel != RemoteSizeLabel) { 
                originalSizeLabel = currentSizeLabel;
            }

            syncGameViewToRemote(remoteView.contentWidth, remoteView.contentHeight, true);

            _lastRemoteViewSize.x = remoteView.contentWidth;
            _lastRemoteViewSize.y = remoteView.contentHeight;
        }

        private void returnToOriginalSize() {
            var currentSizeLabel = sizeLabel(currentSize);
            if (currentSizeLabel == RemoteSizeLabel) { 
                selectSize(findSizeFromLabel(originalSizeLabel));
            }

            _lastRemoteViewSize.x = 0;
            _lastRemoteViewSize.y = 0;
        }

        private void init() {
            if (ModoiumPlugin.isXR) { return; }

            _typeGameView = Type.GetType("UnityEditor.PlayModeView,UnityEditor");
            _methodGetMainPlayModeView = _typeGameView.GetMethod("GetMainPlayModeView", BindingFlags.NonPublic | BindingFlags.Static);
        }

        private void syncGameViewToRemote(int width, int height, bool select) {
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

            if (select) {
                selectSize(size);
            }
        }

        private async void selectSize(object size) {
            if (size == null) { return; }

            var index = sizeIndex(size); 
            var gameView = mainGameView; 
            if (gameView == null) { return; }

            var method = gameView.GetType().GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance);
            method.Invoke(gameView, new[] { index, size });

            await Task.Yield();

            method = gameView.GetType().GetMethod("UpdateZoomAreaAndParent", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(gameView, new object[] { });

            method = gameView.GetType().GetMethod("Repaint", BindingFlags.Public | BindingFlags.Instance);
            method.Invoke(gameView, new object[] { });
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
        private Vector2Int _lastRemoteViewSize;

        private void init() {}
        public void OnPostFirstMessageDispatch(MDMMessageDispatcher.Event evt) {}

        public void Update() {
            if (ModoiumPlugin.isXR) { return; }

            if (_owner.remoteViewConnected) {
                if (_remoteConnected) {
                    handleRemoteViewSizeChange();
                }
                else {
                    setToRemoteViewSize();
                }
            }

            _remoteConnected = _owner.remoteViewConnected;
        }

        private void handleRemoteViewSizeChange() {
            if (_owner.isAppPlaying) { return; }

            var remoteView = _owner.remoteViewDesc;
            if (remoteView.contentWidth == _lastRemoteViewSize.x &&
                remoteView.contentHeight == _lastRemoteViewSize.y) { return; }

            setScreenResolution(remoteView.contentWidth, remoteView.contentHeight);

            _lastRemoteViewSize.x = remoteView.contentWidth;
            _lastRemoteViewSize.y = remoteView.contentHeight;
        }

        private void setToRemoteViewSize() {
            var remoteView = _owner.remoteViewDesc;

            setScreenResolution(remoteView.contentWidth, remoteView.contentHeight);

            _lastRemoteViewSize.x = remoteView.contentWidth;
            _lastRemoteViewSize.y = remoteView.contentHeight;
        }

        private void setScreenResolution(int width, int height) {
            Screen.SetResolution(width, height, Screen.fullScreen);
        }
#endif        
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Modoium.Service {
    internal class MDMGameViewConfigurator {
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

        public MDMGameViewConfigurator(MDMService owner) {
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
                var remoteView = _owner.remoteViewDesc;

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

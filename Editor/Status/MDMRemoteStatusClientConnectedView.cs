using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modoium.Service.Editor {
    internal class MDMRemoteStatusClientConnectedView : VisualElement {
        private TextElement _bodyDevice;
        private MDMRemoteStatusVideoBitrate _videoBitrate;

        public MDMRemoteStatusClientConnectedView(VisualElement parent) {
            this.FillParent().Padding(10);

            Add(createStatus());
            Add(createInfo());

            parent.Add(this);
        }

        public void UpdateView(MDMRemoteStatusWindow.State state, bool embeddedCoreRunning, float videoBitrate, string deviceName) {
            style.display = state == MDMRemoteStatusWindow.State.ClientConnected ?
                DisplayStyle.Flex : DisplayStyle.None;

            updateInfo(embeddedCoreRunning, videoBitrate, deviceName);
        }

        private VisualElement createStatus() {
            var body = new TextElement { text = Styles.bodyStatus };
            body.style.color = Color.green;
            body.style.unityFontStyleAndWeight = FontStyle.Bold;
            return body;
        }

        private VisualElement createInfo() {
            var box = new Box().Padding(4);
            box.style.marginTop = 4;

            _bodyDevice = new TextElement { text = string.Format(Styles.bodyDevice, string.Empty) };
            box.Add(_bodyDevice);

            _videoBitrate = new MDMRemoteStatusVideoBitrate(box);
            _videoBitrate.style.marginTop = 4;

            return box;
        }

        private void updateInfo(bool embeddedCoreRunning, float videoBitrate, string deviceName) {
            _bodyDevice.text = string.Format(Styles.bodyDevice, deviceName);
            _videoBitrate.UpdateView(videoBitrate, embeddedCoreRunning == false);
        }

        private class Styles {
            public static string bodyStatus = $"Device connected";
            public static string bodyDevice = "Device : {0}";
        }
    }
}

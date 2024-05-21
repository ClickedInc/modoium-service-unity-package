using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modoium.Service.Editor {
    internal class MDMRemoteStatusClientConnectedView : VisualElement {
        private TextElement _bodyDevice;
        private TextElement _bodyVideoBitrate;

        public MDMRemoteStatusClientConnectedView(VisualElement parent) {
            this.FillParent().Padding(10);

            Add(createStatus());
            Add(createInfo());

            parent.Add(this);
        }

        public void UpdateView(MDMRemoteStatusWindow.State state, float videoBitrate, string clientUserAgent) {
            if (state != MDMRemoteStatusWindow.State.ClientConnected) {
                style.display = DisplayStyle.None;
                return;
            }
            style.display = DisplayStyle.Flex;

            updateInfo(videoBitrate, clientUserAgent);
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

            _bodyVideoBitrate = new TextElement { text = string.Format(Styles.bodyVideoBitrate, 0f) };
            _bodyVideoBitrate.style.marginTop = 4;
            box.Add(_bodyVideoBitrate);

            return box;
        }

        private void updateInfo(float videoBitrate, string clientUserAgent) {
            _bodyDevice.text = string.Format(Styles.bodyDevice, clientUserAgent);
            _bodyVideoBitrate.text = string.Format(Styles.bodyVideoBitrate, videoBitrate);
        }

        private class Styles {
            public static string bodyStatus = $"Device connected";
            public static string bodyDevice = "Device : {0}";
            public static string bodyVideoBitrate = "Video Bitrate : {0:0.0} Mbps";
        }
    }
}

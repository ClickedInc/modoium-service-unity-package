using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using Unity.Plastic.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif

namespace Modoium.Service {
    internal class MDMMessageDispatcher {
        public enum Event : byte {
            None = 0x01,
            AmpOpened = 0x02,
            AmpClosed = 0x04
        }

        private byte[] _buffer;

        public delegate void MessageReceiveHandler(MDMMessage message);
        public event MessageReceiveHandler onMessageReceived;

        public Event Dispatch() {
            var evt = Event.None;

            while (ModoiumPlugin.CheckMessageQueue(out var source, out var data, out var length)) {
                var buffer = bufferArray(length);
                Marshal.Copy(data, buffer, 0, length);
                ModoiumPlugin.RemoveFirstMessageFromQueue();

                var message = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(buffer, 0, length));
                if (message.ContainsKey("name") == false) { continue;}

                var name = message["name"] as string;
                Debug.Assert(string.IsNullOrEmpty(name) == false);

                var body = message.ContainsKey("body") ? message["body"] : null;

                switch (name) {
                    case MDMMessage.NameCoreConnected:
                        onMessageReceived?.Invoke(new MDMMessageCoreConnected());
                        break;
                    case MDMMessage.NameCoreConnectionFailed:
                        onMessageReceived?.Invoke(new MDMMessageCoreConnectionFailed(body));
                        break;
                    case MDMMessage.NameCoreDisconnected:
                        onMessageReceived?.Invoke(new MDMMessageCoreDisconnected(body));
                        break;
                    case MDMMessage.NameSessionInitiated:
                        onMessageReceived?.Invoke(new MDMMessageSessionInitiated(body));
                        break;
                    case MDMMessage.NameSessionCancelled:
                        onMessageReceived?.Invoke(new MDMMessageSessionCancelled(body));
                        break;
                    case MDMMessage.NameAmpOpened:
                        evt = Event.AmpOpened;
                        onMessageReceived?.Invoke(new MDMMessageAmpOpened());
                        break;
                    case MDMMessage.NameAmpClosed:
                        evt = Event.AmpClosed;
                        onMessageReceived?.Invoke(new MDMMessageAmpClosed(body));
                        break;
                    case MDMMessage.NameClientAppData:
                        onMessageReceived?.Invoke(new MDMMessageClientAppData(body));
                        break;
                }
            }
            return evt;
        }

        private byte[] bufferArray(int length) {
            if (_buffer != null && _buffer.Length >= length) { return _buffer; }

            _buffer = new byte[length];
            return _buffer;
        }
    }
}

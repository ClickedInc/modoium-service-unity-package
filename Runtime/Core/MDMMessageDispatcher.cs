using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;

namespace Modoium.Service {
    internal class MDMMessageDispatcher {
        private byte[] _buffer;

        public delegate void MessageReceiveHandler(MDMMessage message);
        public event MessageReceiveHandler onMessageReceived;

        public void Dispatch() {
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
                    case MDMMessage.NameCoreDisconnected:
                        onMessageReceived?.Invoke(new MDMMessageCoreDisconnected(body));
                        break;
                    case MDMMessage.NameAxrOpenFailed:
                        onMessageReceived?.Invoke(new MDMMessageAxrOpenFailed(body));
                        break;
                    case MDMMessage.NameAxrInitiated:
                        onMessageReceived?.Invoke(new MDMMessageAxrInitiated(body));
                        break;
                    case MDMMessage.NameAxrEstablished:
                        onMessageReceived?.Invoke(new MDMMessageAxrEstablished());
                        break;
                    case MDMMessage.NameAxrFinished:
                        onMessageReceived?.Invoke(new MDMMessageAxrFinished(body));
                        break;
                }
            }
        }

        private byte[] bufferArray(int length) {
            if (_buffer != null && _buffer.Length >= length) { return _buffer; }

            _buffer = new byte[length];
            return _buffer;
        }
    }
}

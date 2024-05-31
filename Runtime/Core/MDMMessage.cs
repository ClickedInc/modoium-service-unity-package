using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Newtonsoft.Json.Linq;

namespace Modoium.Service {
    internal enum MDMFailureCode : int {
        CoreNotFound = -1,
        SocketConnError = -2,
        WebsocketInvalidHTTP = -3,
        WebsocketHandshakeFailed = -4
    }

    internal enum MDMCloseCode : int {
        NormalClosure   = 1000,
        ProtocolError   = 1002,
        NoCode          = 1005,
        SocketError     = 1101,
        Timeout         = 1102,
        InvalidFrame    = 1103
    }

    internal class MDMMessage {
        public const string NameCoreConnected = "core-connected";
        public const string NameCoreConnectionFailed = "core-connection-failed";
        public const string NameCoreDisconnected = "core-disconnected";
        public const string NameSessionInitiated = "session-initiated";
        public const string NameSessionCancelled = "session-cancelled";
        public const string NameAmpOpened = "amp-opened";
        public const string NameAmpClosed = "amp-closed";
        public const string NameClientAppData = "client-app-data";
    }

    internal class MDMMessageCoreConnected : MDMMessage {}

    internal class MDMMessageCoreConnectionFailed : MDMMessage {
        public int code { get; private set; }
        public int statusCode { get; private set; }
        public string reason { get; private set; }

        public MDMMessageCoreConnectionFailed(object body) {
            Debug.Assert(body is JObject);
            var dict = body as JObject;

            code = dict.Value<int>("code");
            statusCode = dict.Value<int>("statusCode");
            reason = dict.Value<string>("reason");
        }
    }

    internal class MDMMessageCoreDisconnected : MDMMessage {
        public int statusCode { get; private set; }
        public string closeReason { get; private set; }

        public MDMMessageCoreDisconnected(object body) {
            Debug.Assert(body is JObject);
            var dict = body as JObject;

            statusCode = dict.Value<int>("statusCode");
            closeReason = dict.Value<string>("closeReason");
        }
    }

    internal class MDMMessageSessionInitiated : MDMMessage {
        public MDMAppData appData { get; private set; }

        public MDMMessageSessionInitiated(object body) {
            appData = new MDMAppData(body);
        }
    }

    internal class MDMMessageSessionCancelled : MDMMessage {
        public string reason { get; private set; }

        public MDMMessageSessionCancelled(object body) {
            Debug.Assert(body is JObject);
            var dict = body as JObject;

            reason = dict.Value<string>("reason");
        }
    }

    internal class MDMMessageAmpOpened : MDMMessage {}
    internal class MDMMessageAmpClosed : MDMMessage {
        public int code { get; private set; }
        public string reason { get; private set; }

        public MDMMessageAmpClosed(object body) {
            Debug.Assert(body is JObject);
            var dict = body as JObject;

            code = dict.Value<int>("code");
            reason = dict.Value<string>("reason");
        }
    }

    internal class MDMMessageClientAppData : MDMMessage {
        public MDMAppData appData { get; private set; }

        public MDMMessageClientAppData(object body) {
            appData = new MDMAppData(body);
        }
    }
}

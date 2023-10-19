using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine.Jobs;

namespace Modoium.Service {
    internal class MDMMessage {
        public const string NameCoreConnected = "core-connected";
        public const string NameCoreDisconnected = "core-disconnected";
        public const string NameAxrOpenFailed = "axr-open-failed";
        public const string NameAxrInitiated = "axr-initiated";
        public const string NameAxrEstablished = "axr-established";
        public const string NameAxrFinished = "axr-finished";
    }

    internal class MDMMessageCoreConnected : MDMMessage {}

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

    internal class MDMMessageAxrOpenFailed : MDMMessage {
        public int code { get; private set; }

        public MDMMessageAxrOpenFailed(object body) {
            Debug.Assert(body is JObject);
            var dict = body as JObject;

            code = dict.Value<int>("code");
        }
    }

    internal class MDMMessageAxrInitiated : MDMMessage {
        public MDMAppData appData { get; private set; }

        public MDMMessageAxrInitiated(object body) {
            appData = new MDMAppData(body);
        }
    }

    internal class MDMMessageAxrEstablished : MDMMessage {}

    internal class MDMMessageAxrFinished : MDMMessage {
        public int code { get; private set; }
        public string reason { get; private set; }

        public MDMMessageAxrFinished(object body) {
            Debug.Assert(body is JObject);
            var dict = body as JObject;

            code = dict.Value<int>("code");
            reason = dict.Value<string>("reason");
        }
    }
}

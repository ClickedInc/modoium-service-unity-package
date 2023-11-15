using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace Modoium.Service {
    [JsonObject(MemberSerialization.OptIn)]
    public class MDMAppData {
        [JsonProperty] private MDMOffer[] offer;
        [JsonProperty] private MDMUserData userData;

        public MDMVideoOffer videoOffer {
            get {
                foreach (var iter in offer) {
                    if (iter is MDMVideoOffer videoOffer) {
                        return videoOffer;
                    }
                }
                return null;
            }
        }

        public MDMAudioOffer audioOffer {
            get {
                foreach (var iter in offer) {
                    if (iter is MDMAudioOffer audioOffer) {
                        return audioOffer;
                    }
                }
                return null;
            }
        }

        public MDMAppData(MDMVideoOffer videoOffer) {
            offer = new MDMOffer[] {
                videoOffer,
                new MDMAudioOffer(),
                new MDMApplicationOffer()
            };
            userData = new MDMUserData();
        }

        public MDMAppData(object obj) {
            Debug.Assert(obj is JObject);
            var dict = obj as JObject;

            Debug.Assert(dict.ContainsKey("offer"));
            offer = dict.Value<JArray>("offer").Select((iter) => MDMOffer.Parse(iter)).Where((iter) => iter != null).ToArray();

            userData = new MDMUserData(obj);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MDMOffer {
        internal static MDMOffer Parse(object obj) {
            if (obj is JObject == false) { return null; }

            var dict = obj as JObject;
            if (dict.ContainsKey("type") == false || dict.ContainsKey("accept") == false) { return null; }

            switch (dict.Value<string>("type")) {
                case "video":
                    return MDMVideoOffer.Parse(obj);
                case "audio":
                    return new MDMAudioOffer(obj);
                case "application":
                    return new MDMApplicationOffer(obj);
                default:
                    return new MDMOffer(obj);
            }
        }

        [JsonProperty] private string type;
        [JsonProperty] protected string[] accept;

        internal MDMOffer(string type, string[] accept) {
            this.type = type;
            this.accept = accept;
        }

        internal MDMOffer(object obj) {
            Debug.Assert(obj is JObject);
            var dict = obj as JObject;

            type = dict.Value<string>("type");
            Debug.Assert(string.IsNullOrEmpty(type) == false);

            accept = dict.Value<JArray>("accept").Select((iter) => iter.ToObject<string>()).ToArray();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MDMVideoOffer : MDMOffer {
        internal static new MDMVideoOffer Parse(object obj) {
            var dict = obj as JObject;
            if (dict.ContainsKey("stereoscopy")) {
                return new MDMStereoVideoOffer(obj);
            }
            else if (dict.ContainsKey("monoscopy")) {
                return new MDMMonoVideoOffer(obj);
            }
            return null;
        }

        [JsonProperty] private ImageAttr[] imageattr;
        [JsonProperty] private float frameRate;
        [JsonProperty] private Bitrate bitRate;
        [JsonProperty] private string direction;
        [JsonProperty] private Xfmtp xfmtp;

        internal int width => imageattr[0].x;
        internal int height => imageattr[0].y;
        internal float framerate => frameRate;
        internal long bitrate => bitRate.max;
        internal string[] codecs => accept;
        internal bool useMPEG4BitstreamFormat => xfmtp.useSizePrefix;

        internal MDMVideoOffer(string[] codecs, 
                               int width, 
                               int height, 
                               float framerate,
                               long startBitrate,
                               long maxBitrate) : base("video", codecs) { 
            imageattr = new ImageAttr[] {
                new ImageAttr { x = width, y = height }
            };
            frameRate = framerate;
            bitRate = new Bitrate { start = startBitrate, max = maxBitrate };
            direction = "recvonly";
            xfmtp = new Xfmtp { useSizePrefix = false };
        }

        internal MDMVideoOffer(object obj) : base(obj) {
            Debug.Assert(obj is JObject);
            var dict = obj as JObject;

            Debug.Assert(dict.ContainsKey("imageattr"));
            imageattr = dict.Value<JArray>("imageattr").Select((iter) => { 
                var dict = iter as JObject;

                return new ImageAttr { 
                    x = iter.Value<int>("x"), 
                    y = iter.Value<int>("y")
                }; 
            }).ToArray();

            Debug.Assert(dict.ContainsKey("frameRate"));
            frameRate = dict.Value<float>("frameRate");

            Debug.Assert(dict.ContainsKey("bitRate"));
            var bitrate = dict.Value<JObject>("bitRate");
            bitRate = new Bitrate { 
                start = bitrate.Value<long>("start"),
                max = bitrate.Value<long>("max")
            };

            Debug.Assert(dict.ContainsKey("direction"));
            direction = dict.Value<string>("direction");

            Debug.Assert(dict.ContainsKey("xfmtp"));
            var xfmtp = dict.Value<JObject>("xfmtp");
            this.xfmtp = new Xfmtp { 
                useSizePrefix = dict.Value<bool>("useSizePrefix")
            };
        }

        [JsonObject(MemberSerialization.OptIn)]
        private struct ImageAttr {
            [JsonProperty] public int x;
            [JsonProperty] public int y;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private struct Bitrate {
            [JsonProperty] public long start;
            [JsonProperty] public long max;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private struct Xfmtp {
            [JsonProperty] public bool useSizePrefix;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MDMStereoVideoOffer : MDMVideoOffer {
        [JsonProperty] private Stereoscopy stereoscopy;

        public MDMStereoVideoOffer(string[] codecs, 
                                   int width, 
                                   int height, 
                                   float framerate,
                                   long startBitrate,
                                   long maxBitrate,
                                   Vector4 leftEyeProjection,
                                   float ipd) : base(codecs, width, height, framerate, startBitrate, maxBitrate) { 
            stereoscopy = new Stereoscopy { 
                leftEyeProjection = new float[] { leftEyeProjection.x, leftEyeProjection.y, leftEyeProjection.z, leftEyeProjection.w },
                ipd = ipd
            };
        }

        public MDMStereoVideoOffer(object obj) : base(obj) {
            Debug.Assert(obj is JObject);
            var dict = obj as JObject;

            Debug.Assert(dict.ContainsKey("stereoscopy"));
            var stereoscopy = dict.Value<JObject>("stereoscopy");
            this.stereoscopy = new Stereoscopy { 
                leftEyeProjection = stereoscopy.Value<JArray>("leftEyeProjection").Select((iter) => iter.ToObject<float>()).ToArray(),
                ipd = stereoscopy.Value<float>("ipd")
            };
        }

        [JsonObject(MemberSerialization.OptIn)]
        private struct Stereoscopy {
            [JsonProperty] public float[] leftEyeProjection;
            [JsonProperty] public float ipd;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MDMMonoVideoOffer : MDMVideoOffer {
        [JsonProperty] private Monoscopy monoscopy;

        public MDMMonoVideoOffer(string[] codecs, 
                                 int width, 
                                 int height, 
                                 float framerate,
                                 long startBitrate,
                                 long maxBitrate,
                                 Vector4 cameraProjection) : base(codecs, width, height, framerate, startBitrate, maxBitrate) {
            monoscopy = new Monoscopy {
                cameraProjection = new float[] { cameraProjection.x, cameraProjection.y, cameraProjection.z, cameraProjection.w }
            };
        }

        public MDMMonoVideoOffer(object obj) : base(obj) {
            Debug.Assert(obj is JObject);
            var dict = obj as JObject;

            Debug.Assert(dict.ContainsKey("monoscopy"));
            var monoscopy = dict.Value<JObject>("monoscopy");
            this.monoscopy = new Monoscopy { 
                cameraProjection = monoscopy.Value<JArray>("cameraProjection").Select((iter) => iter.ToObject<float>()).ToArray()
            };
        }

        [JsonObject(MemberSerialization.OptIn)]
        private struct Monoscopy {
            [JsonProperty] public float[] cameraProjection;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MDMAudioOffer : MDMOffer {
        public string[] codecs => accept;

        public MDMAudioOffer() : base("audio", new string[] { "opus" }) {}
        public MDMAudioOffer(object obj) : base(obj) {}
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal class MDMApplicationOffer : MDMOffer {
        public MDMApplicationOffer() : base("application", new string[] { "onairxr-input" }) {}
        public MDMApplicationOffer(object obj) : base(obj) {}
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal struct MDMUserData {
        public MDMUserData(object obj) {}
    }
}

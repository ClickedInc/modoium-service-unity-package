using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace Modoium.Service {
    [JsonObject(MemberSerialization.OptIn)]
    public class MDMAppData {
        [JsonProperty] private MDMMediaDesc[] medias;

        public MDMVideoDesc videoDesc {
            get {
                foreach (var iter in medias) {
                    if (iter is MDMVideoDesc videoDesc) {
                        return videoDesc;
                    }
                }
                return null;
            }
        }

        public MDMAudioDesc audioDesc {
            get {
                foreach (var iter in medias) {
                    if (iter is MDMAudioDesc audioDesc) {
                        return audioDesc;
                    }
                }
                return null;
            }
        }

        public MDMAppData(MDMVideoDesc videoDesc) {
            medias = new MDMMediaDesc[] {
                videoDesc,
                new MDMAudioDesc(),
                new MDMApplicationDesc()
            };
        }

        public MDMAppData(object obj) {
            Debug.Assert(obj is JObject);
            var dict = obj as JObject;

            Debug.Assert(dict.ContainsKey("medias"));
            medias = dict.Value<JArray>("medias").Select((iter) => MDMMediaDesc.Parse(iter)).Where((iter) => iter != null).ToArray();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MDMMediaDesc {
        internal static MDMMediaDesc Parse(object obj) {
            if (obj is JObject == false) { return null; }

            var dict = obj as JObject;
            if (dict.ContainsKey("type") == false || dict.ContainsKey("accept") == false) { return null; }

            switch (dict.Value<string>("type")) {
                case "video":
                    return MDMVideoDesc.Parse(obj);
                case "audio":
                    return new MDMAudioDesc(obj);
                case "application":
                    return new MDMApplicationDesc(obj);
                default:
                    return new MDMMediaDesc(obj);
            }
        }

        [JsonProperty] private string type;
        [JsonProperty] protected string[] accept;

        internal MDMMediaDesc(string type, string[] accept) {
            this.type = type;
            this.accept = accept;
        }

        internal MDMMediaDesc(object obj) {
            Debug.Assert(obj is JObject);
            var dict = obj as JObject;

            type = dict.Value<string>("type");
            Debug.Assert(string.IsNullOrEmpty(type) == false);

            accept = dict.Value<JArray>("accept").Select((iter) => iter.ToObject<string>()).ToArray();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MDMVideoDesc : MDMMediaDesc {
        internal static new MDMVideoDesc Parse(object obj) {
            var dict = obj as JObject;
            if (dict.ContainsKey("stereoscopy")) {
                return new MDMStereoVideoDesc(obj);
            }
            else if (dict.ContainsKey("monoscopy")) {
                return new MDMMonoVideoDesc(obj);
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

        internal MDMVideoDesc(string[] codecs, 
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

        internal MDMVideoDesc(object obj) : base(obj) {
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
                useSizePrefix = xfmtp.Value<bool>("useSizePrefix")
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
    public class MDMStereoVideoDesc : MDMVideoDesc {
        [JsonProperty] private Stereoscopy stereoscopy;

        public MDMStereoVideoDesc(string[] codecs, 
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

        public MDMStereoVideoDesc(object obj) : base(obj) {
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
    public class MDMMonoVideoDesc : MDMVideoDesc {
        [JsonProperty] private Monoscopy monoscopy;

        public MDMMonoVideoDesc(string[] codecs, 
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

        public MDMMonoVideoDesc(object obj) : base(obj) {
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
    public class MDMAudioDesc : MDMMediaDesc {
        public string[] codecs => accept;

        public MDMAudioDesc() : base("audio", new string[] { "opus" }) {}
        public MDMAudioDesc(object obj) : base(obj) {}
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal class MDMApplicationDesc : MDMMediaDesc {
        public MDMApplicationDesc() : base("application", new string[] { "onairxr-input" }) {}
        public MDMApplicationDesc(object obj) : base(obj) {}
    }
}

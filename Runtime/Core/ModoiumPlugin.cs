using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace Modoium.Service {
    internal static class ModoiumPlugin {
        private const string LibName = "modoium";

        [DllImport(LibName, EntryPoint = "mdm_configure")]
        public static extern void Configure(float idleFrameRate,
                                            float maxFrameRate,
                                            int audioSampleRate,
                                            int displayTextureColorSpaceHint,
                                            bool cpuReadableEncodeBuffer,
                                            int codecs,
                                            int encodingPreset,
                                            int encodingPerformance);

        [DllImport(LibName, EntryPoint = "mdm_startupService")]
        public static extern void StartupService(string serviceName, string userdata);

        [DllImport(LibName, EntryPoint = "mdm_shutdownService")]
        public static extern void ShutdownService();

        [DllImport(LibName, EntryPoint = "mdm_checkMessageQueue")]
        public static extern bool CheckMessageQueue(out IntPtr source, out IntPtr data, out int length);

        [DllImport(LibName, EntryPoint = "mdm_removeFirstMessageFromQueue")]
        public static extern void RemoveFirstMessageFromQueue();

        [DllImport(LibName, EntryPoint = "mdm_rejectInitiation")]
        public static extern void RejectInitiation(string reason);

        public static void AcceptInitiation(MDMAppData appData) {
            mdm_acceptInitiation(JsonConvert.SerializeObject(appData));
        }

        [DllImport(LibName, EntryPoint = "mdm_processMasterAudioOutput")]
        public static extern void ProcessMasterAudioOutput(float[] data, int sampleCount, int channels, double timestamp);

        [DllImport(LibName)] private static extern void mdm_acceptInitiation(string param);

        [Serializable]
        public struct InitiationParams {
            public string[] videoTypes;
            public int videoWidth;
            public int videoHeight;
            public float videoFramerate;
            public long videoBitrate;
            public string[] audioTypes;
            public bool useMPEG4BitstreamFormat;
        }
    }
}

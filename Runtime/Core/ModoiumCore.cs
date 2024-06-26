using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Modoium.Service {
    internal class ModoiumCore {
#if UNITY_EDITOR_OSX
        private const string LibName = "modoiumCore";
#else
        private const string LibName = "modoium-core";
#endif

        [DllImport(LibName)] private static extern int ModoiumPort();
        [DllImport(LibName)] private static extern int ModoiumStartUpAsEmbedded(string hostname);
        [DllImport(LibName)] private static extern void ModoiumShutDown();

        public int port => ModoiumPort();
        public bool running => port > 0;

        public void Startup() {
            if (running) { return; }

            var p = ModoiumStartUpAsEmbedded("Modoium Remote");
            if (p > 0) {
                ModoiumPlugin.videoBitrate = 24_000_000;
            }
            else {
                Debug.LogError($"[modoium] failed to start core: {p}");
            }
        }

        internal void Shutdown() {
            if (running == false) { return; }

            ModoiumShutDown();
        }
    }
}

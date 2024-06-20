using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

namespace Modoium.Service.Editor {
    public class ModoiumCore {
        [MenuItem("Modoium/Core/Startup", false, 100)]
        public static void Startup() {
            _instance.startup();
        }

        [MenuItem("Modoium/Core/Shutdown", false, 101)]
        public static void Shutdown() {
            _instance.shutdown();
        }

        private static string generateRandomBase64Key(int length) {
            byte[] key = new byte[length];
            System.Random random = new System.Random();
            random.NextBytes(key);
            return System.Convert.ToBase64String(key);
        }

        //-----------------------------------------------------------------------------------------

#if UNITY_EDITOR_OSX
        private const string LibName = "modoiumCore";
#else
        private const string LibName = "modoium-core";
#endif

        private static ModoiumCore _instance;

        [DllImport(LibName)] private static extern int ModoiumStartUp(ushort port, string parameters);
        [DllImport(LibName)] private static extern void ModoiumShutDown();

        static ModoiumCore() {
            _instance = new ModoiumCore();
        }

        private void startup() {
            var adminKey = generateRandomBase64Key(32);
            
            var config = $"{{\"hostName\":\"Unity Editor\",\"verificationCode\":\"123456\",\"appInstanceId\":\"\",\"platformName\":\"{SystemInfo.operatingSystemFamily}\",\"platformVersion\":\"{SystemInfo.operatingSystem}\"}}";
            var parameters = $"{{\"adminKey\":\"{adminKey}\",\"config\":{config}}}";

            var ret = ModoiumStartUp(0, parameters);
            if (ret < 0) {
                Debug.LogError($"Failed to start ModoiumCore: {ret}");
            }
        }

        private void shutdown() {
            ModoiumShutDown();
        }


    }
}

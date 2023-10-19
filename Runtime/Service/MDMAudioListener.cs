using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modoium.Service {
    [RequireComponent(typeof(AudioListener))]
    public class MDMAudioListener : MonoBehaviour {
        private static MDMAudioListener _instance;

        private void Awake() {
            if (_instance != null) {
                throw new UnityException("[modoium] there must be only one MDMAudioListener in the scene.");
            }
            _instance = this;

            hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
        }

        private void OnAudioFilterRead(float[] data, int channels) {
            ModoiumPlugin.ProcessMasterAudioOutput(data, data.Length / channels, channels, AudioSettings.dspTime);
        }
    }
}

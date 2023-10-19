using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Modoium.Service {
    public enum MDMMirrorBlitMode {
        None = 0,
        Left = -1,
        Right = -2,
        SideBySide = -3
    }

    public enum MDMRenderPass {
        SinglePassInstanced = 0,
        MultiPass = 1
    }

    public enum MDMTextureColorSpaceHint {
        None = 0,
        Gamma = 1,
        Linear = 2
    }

    public enum MDMContentType {
        None = 0,
        MR = 0x01,
        VR = 0x02,
        Scene = 0x04
    }

    [Serializable]
    [XRConfigurationData("Modoium", SettingsKey)]
    public class ModoiumSettings : ScriptableObject {
        internal const string SettingsKey = "com.modoium.service.settings";

        internal static bool IsUniversalRenderPipeline() => UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType()?.Name?.Equals("UniversalRenderPipelineAsset") ?? false;
        internal static bool IsHDRenderPipeline() => UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType()?.Name?.Equals("HDRenderPipelineAsset") ?? false;

#if UNITY_EDITOR
        internal static ModoiumSettings instance {
            get {
                UnityEngine.Object obj;
                UnityEditor.EditorBuildSettings.TryGetConfigObject(SettingsKey, out obj);
                if (obj == null || (obj is ModoiumSettings) == false) { return null; }

                var settings = obj as ModoiumSettings;
                settings.ParseCommandLine();

                return settings;
            }
        }
#else
        internal static ModoiumSettings runtimeInstance { get; private set; } = null;
        internal static ModoiumSettings instance => runtimeInstance;

        public void Awake() {
            if (runtimeInstance != null) { return; }

            ParseCommandLine();
            runtimeInstance = this;
        }
#endif

        [SerializeField] private int contentType = (int)MDMContentType.None;
        [SerializeField] [Range(10, 120)] private int minFrameRate = 10;
        [SerializeField] private MDMMirrorBlitMode defaultMirrorBlitMode = MDMMirrorBlitMode.Left;
        [SerializeField] private MDMRenderPass desiredRenderPass = MDMRenderPass.SinglePassInstanced;

        [SerializeField] private bool advancedSettingsEnabled = false;
        [SerializeField] private MDMTextureColorSpaceHint displayTextureColorSpaceHint = MDMTextureColorSpaceHint.None;

        internal int propMinFrameRate => minFrameRate;
        internal int propDefaultMirrorBlitMode => (int)defaultMirrorBlitMode;
        internal MDMRenderPass propDesiredRenderPass => advancedSettingsEnabled ? desiredRenderPass : MDMRenderPass.SinglePassInstanced;

        internal int propIdleFrameRate {
            get {
                if (Application.isEditor == false) { return propMinFrameRate; }

                var resolution = Screen.currentResolution;
                return (int)(resolution.refreshRateRatio.numerator / resolution.refreshRateRatio.denominator);
            }
        }
        
        internal MDMTextureColorSpaceHint propDisplayTextureColorSpaceHint {
            get {
                var value = advancedSettingsEnabled ? displayTextureColorSpaceHint : MDMTextureColorSpaceHint.None;

                if (value == MDMTextureColorSpaceHint.None) {
                    if (IsUniversalRenderPipeline()) {
                        // workaround: URP uses always non-sRGB texture even if color space is set to linear. (but xr plugin misleads as if it were sRGB.)
                        value = MDMTextureColorSpaceHint.Gamma;
                    }
                    else if (IsHDRenderPipeline() && QualitySettings.activeColorSpace == ColorSpace.Gamma) {
                        // workaround: On HDRP, xr plugin misleads as if texture were sRGB even when color space is set to gamma.
                        value = MDMTextureColorSpaceHint.Gamma;
                    }
                }
                return value;
            }
        }

        internal string serviceName => Application.isEditor ? $"Unity Editor - {Application.productName}" : Application.productName;

        internal string serviceUserdata {
            get {
                var ctypes = new List<string>();
                if ((contentType & (int)MDMContentType.MR) != 0) { 
                    ctypes.Add("mr");
                }
                if ((contentType & (int)MDMContentType.VR) != 0) { 
                    ctypes.Add("vr"); 
                }
                if ((contentType & (int)MDMContentType.Scene) != 0) { 
                    ctypes.Add("scene"); 
                }
                return $"ctypes={string.Join(";", ctypes)}";
            }
        }

        internal ModoiumSettings ParseCommandLine() {
            var pairs = MDMUtils.ParseCommandLine(Environment.GetCommandLineArgs());
            if (pairs == null) { return this; }

            foreach (var key in pairs.Keys) {
                if (key.Equals("modoium_min_frame_rate")) {
                    minFrameRate = MDMUtils.ParseInt(pairs[key], propMinFrameRate, (parsed) => parsed > 0);
                }
            }
            return this;
        }

        // experimental features
        #pragma warning disable 0414

        [SerializeField] private bool cpuReadableEncodeBuffer = false;
        [SerializeField] private MDMCodec codecs = MDMCodec.All;
        [SerializeField] private MDMEncodingPreset encodingPreset = MDMEncodingPreset.LowLatency;
        [SerializeField] private MDMEncodingQuality encodingQuality = MDMEncodingQuality.VeryHigh;

        #pragma warning restore 0414        

#if MODOIUM_EXPERIMENTAL
        internal bool propCpuReadableEncodeBuffer => advancedSettingsEnabled ? cpuReadableEncodeBuffer : false;
        internal MDMCodec propCodecs => advancedSettingsEnabled ? codecs : MDMCodec.All;
        internal MDMEncodingPreset propEncodingPreset => advancedSettingsEnabled ? encodingPreset : MDMEncodingPreset.LowLatency;
        internal MDMEncodingQuality propEncodingQuality => advancedSettingsEnabled ? encodingQuality : MDMEncodingQuality.VeryHigh;
#else
        internal bool propCpuReadableEncodeBuffer => false;
        internal MDMCodec propCodecs => MDMCodec.All;
        internal MDMEncodingPreset propEncodingPreset => MDMEncodingPreset.LowLatency;
        internal MDMEncodingQuality propEncodingQuality => MDMEncodingQuality.VeryHigh;
#endif
    }
}

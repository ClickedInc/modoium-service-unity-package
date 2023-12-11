using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

#if UNITY_XR_MANAGEMENT
using UnityEngine.XR.Management;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Modoium.Service {
    internal static class ModoiumPlugin {
        private const string LibName = "modoium";

#if UNITY_XR_MANAGEMENT
    #if UNITY_EDITOR
        public static bool isXR {
            get {
                if (XRGeneralSettings.Instance != null) {
                    return XRGeneralSettings.Instance.InitManagerOnStart;
                }

                // Workaround: force to set XRGeneralSettings.Instance in non-play mode
                EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettings _);
                if (XRGeneralSettings.Instance != null) {
                    return XRGeneralSettings.Instance.InitManagerOnStart;
                }
                return false;
            }
        }
    #else
        public static bool isXR => XRSettings.enabled;
    #endif
#else
        public static bool isXR => false;
#endif
        
        [DllImport(LibName, EntryPoint = "mdm_startupService")]
        public static extern void StartupService(string serviceName, string userdata);

        [DllImport(LibName, EntryPoint = "mdm_shutdownService")]
        public static extern void ShutdownService();

        [DllImport(LibName, EntryPoint = "mdm_reopenService")]
        public static extern void ReopenService(string serviceName, string userdata);

        [DllImport(LibName, EntryPoint = "mdm_checkMessageQueue")]
        public static extern bool CheckMessageQueue(out IntPtr source, out IntPtr data, out int length);

        [DllImport(LibName, EntryPoint = "mdm_removeFirstMessageFromQueue")]
        public static extern void RemoveFirstMessageFromQueue();

        [DllImport(LibName, EntryPoint = "mdm_setBitrate")]
        public static extern void SetBitrate(long bitrate);

        public static void SetBitrateInMbps(float bitrateInMbps) {
            long roundedInKbps = Mathf.RoundToInt(bitrateInMbps * 1000);
            SetBitrate(roundedInKbps * 1000);
        }

        [DllImport(LibName, EntryPoint = "mdm_processMasterAudioOutput")]
        public static extern void ProcessMasterAudioOutput(float[] data, int sampleCount, int channels, double timestamp);

        [DllImport(LibName, EntryPoint = "mdm_play")]
        public static extern void Play(string clientAppData,
                                       float idleFrameRate,
                                       float maxFrameRate,
                                       int audioSampleRate,
                                       int displayTextureColorSpaceHint,
                                       int codecs,
                                       int encodingPreset,
                                       int encodingQuality);

        [DllImport(LibName, EntryPoint = "mdm_stop")]
        public static extern void Stop();
        
        public static bool GetInputTouch2D(byte device, byte control, out Vector2 position, out byte state) {
            if (mdm_getInputTouch2D(device, control, out var pos, out state)) {
                position = pos.ToVector2();
                return true;
            }
            else {
                position = Vector2.zero;
                return false;
            }
        }

        [DllImport(LibName, EntryPoint = "mdm_updateInputFrame")]
        public static extern void UpdateInputFrame();

        [DllImport(LibName, EntryPoint = "mdm_isInputActive")]
        public static extern bool IsInputActive(byte device, byte control);

        [DllImport(LibName, EntryPoint = "mdm_getInputActivated")]
        public static extern bool GetInputActivated(byte device, byte control);

        [DllImport(LibName, EntryPoint = "mdm_getInputDeactivated")]
        public static extern bool GetInputDeactivated(byte device, byte control);

        public static void RenderInit(CommandBuffer commandBuffer) {
            commandBuffer.IssuePluginEvent(mdm_init_renderThread_func(), 0);
        }

        public static void RenderUpdate(CommandBuffer commandBuffer) {
            commandBuffer.IssuePluginEvent(mdm_update_renderThread_func(), 0);
        }

        public static void RenderFramebuffersReallocated(CommandBuffer commandBuffer, IntPtr nativeFramebufferArray) {
            commandBuffer.IssuePluginEventAndData(mdm_framebuffersReallocated_renderThread_func(), 0, nativeFramebufferArray);
        }

        public static void RenderPreRender(CommandBuffer commandBuffer) {
            commandBuffer.IssuePluginEvent(mdm_preRender_renderThread_func(), 0);
        }

        public static void RenderPostRender(CommandBuffer commandBuffer, int framebufferIndex) {
            commandBuffer.IssuePluginEvent(mdm_postRender_renderThread_func(), framebufferIndex);
        }

        public static void RenderCleanup(CommandBuffer commandBuffer) {
            commandBuffer.IssuePluginEvent(mdm_cleanup_renderThread_func(), 0);
        }

        [DllImport(LibName)] private static extern IntPtr mdm_init_renderThread_func();
        [DllImport(LibName)] private static extern IntPtr mdm_update_renderThread_func();
        [DllImport(LibName)] private static extern IntPtr mdm_framebuffersReallocated_renderThread_func();
        [DllImport(LibName)] private static extern IntPtr mdm_preRender_renderThread_func();
        [DllImport(LibName)] private static extern IntPtr mdm_postRender_renderThread_func();
        [DllImport(LibName)] private static extern IntPtr mdm_cleanup_renderThread_func();
        [DllImport(LibName)] private static extern bool mdm_getInputTouch2D(byte device, byte control, out Vector2D position, out byte state);

        [StructLayout(LayoutKind.Sequential)]
        private struct Vector2D {
            public float x;
            public float y;

            public Vector2D(Vector2 value) {
                x = value.x;
                y = value.y;
            }

            public Vector2 ToVector2() {
                return new Vector2(x, y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Vector3D {
            public float x;
            public float y;
            public float z;

            public Vector3D(Vector3 value) {
                x = value.x;
                y = value.y;
                z = value.z;
            }

            public Vector3 ToVector3() {
                return new Vector3(x, y, z);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Vector4D {
            public float x;
            public float y;
            public float z;
            public float w;

            public Vector4D(Vector4 value) {
                x = value.x;
                y = value.y;
                z = value.z;
                w = value.w;
            }

            public Vector4D(Quaternion value) {
                x = value.x;
                y = value.y;
                z = value.z;
                w = value.w;
            }

            public Vector4 ToVector4() {
                return new Vector4(x, y, z, w);
            }

            public Quaternion ToQuaternion() {
                return new Quaternion(x, y, z, w);
            }
        }
    }
}

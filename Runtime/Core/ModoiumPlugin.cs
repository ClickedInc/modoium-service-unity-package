using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

#if UNITY_XR_MANAGEMENT
using UnityEngine.XR.Management;
#endif

namespace Modoium.Service {
    internal static class ModoiumPlugin {
        private const string LibName = "modoium";

#if UNITY_XR_MANAGEMENT
        public static bool isXR {
            get {
                if (Application.isEditor == false) { return XRSettings.enabled; }

                return XRGeneralSettings.Instance?.InitManagerOnStart ?? false;
            }
        }
#else
        public static bool isXR => false;
#endif

        [DllImport(LibName, EntryPoint = "mdm_startupService")]
        public static extern void StartupService(string serviceName, string userdata);

        [DllImport(LibName, EntryPoint = "mdm_shutdownService")]
        public static extern void ShutdownService();

        [DllImport(LibName, EntryPoint = "mdm_checkMessageQueue")]
        public static extern bool CheckMessageQueue(out IntPtr source, out IntPtr data, out int length);

        [DllImport(LibName, EntryPoint = "mdm_removeFirstMessageFromQueue")]
        public static extern void RemoveFirstMessageFromQueue();

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
    }
}

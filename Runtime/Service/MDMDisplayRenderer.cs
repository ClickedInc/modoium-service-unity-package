using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace Modoium.Service {
    internal class MDMDisplayRenderer {
        private MonoBehaviour _driver;
        private CommandBuffer _commandBuffer;
        private SwapChain _swapChain;
        private Material _blitMaterial;
        private bool _running;

        public MDMDisplayRenderer(MonoBehaviour driver = null) {
            _driver = driver;
            _commandBuffer = new CommandBuffer();
        }

        public void Start(MDMVideoDesc displayConfig) {
            if (ModoiumPlugin.isXR || _running) { return; }
            _running = true;

            if (_blitMaterial == null) {
                _blitMaterial = new Material(Shader.Find("Modoium/Fullscreen Blit"));
            }
            _swapChain = new SwapChain(displayConfig, _blitMaterial);
            
            startCoroutine(renderLoop());
        }

        public void Stop() {
            _running = false;
        }

        private IEnumerator renderLoop() {
            ModoiumPlugin.RenderInit(_commandBuffer);
            flushCommandBuffer(_commandBuffer);

            while (_running) {
                yield return new WaitForEndOfFrame();
                if (_running == false) { break; }

                ModoiumPlugin.RenderUpdate(_commandBuffer);

                if (_swapChain.reallocated) {
                    ModoiumPlugin.RenderFramebuffersReallocated(_commandBuffer, _swapChain.nativeFramebufferArray);
                }

                ModoiumPlugin.RenderPreRender(_commandBuffer);
                _swapChain.CopyFrameBuffer(_commandBuffer, out var framebufferIndex, out var aspect);
                ModoiumPlugin.RenderPostRender(_commandBuffer, framebufferIndex, aspect);

                flushCommandBuffer(_commandBuffer);
            }

            ModoiumPlugin.RenderCleanup(_commandBuffer);
            flushCommandBuffer(_commandBuffer);

            _swapChain.Release();
        }

        private void flushCommandBuffer(CommandBuffer commandBuffer) {
            Graphics.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        private void startCoroutine(IEnumerator coroutine) {
            if (_driver == null) {
                _driver = Driver.Create();
            }

            _driver.StartCoroutine(coroutine);
        }

        private class SwapChain {
            private const int Length = 4;

            private Material _blitMaterial;
            private RenderTexture[] _textures;
            private FramebufferArray _framebufferArray;
            private int _cursor;
            private bool _reallocated;

            public IntPtr nativeFramebufferArray { get; private set; } = IntPtr.Zero;
            
            public bool reallocated {
                get {
                    if (_reallocated == false) { return false; }

                    _reallocated = false;
                    return true;
                }
            }

            public SwapChain(MDMVideoDesc displayConfig, Material blitMaterial) {
                _blitMaterial = blitMaterial;
                _textures = new RenderTexture[Length];
                _framebufferArray = new FramebufferArray(Length);
                nativeFramebufferArray = Marshal.AllocHGlobal(_framebufferArray.count * IntPtr.Size + sizeof(int));

                reallocate(displayConfig);
            }

            public void CopyFrameBuffer(CommandBuffer commandBuffer, out int framebufferIndex, out float aspect) {
                var displaySize = (Display.main.renderingWidth, Display.main.renderingHeight);
                var texture = _textures[_cursor];

                if (displaySize.renderingWidth == texture.width && displaySize.renderingHeight == texture.height) {
                    aspect = 0;
                    commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, texture);
                }
                else {
                    aspect = (float)displaySize.renderingWidth / displaySize.renderingHeight;
                    commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, texture, _blitMaterial);
                }

                framebufferIndex = _cursor;
                _cursor = (_cursor + 1) % _textures.Length;
            }

            public void Release() {
                for (var index = 0; index < _textures.Length; index++) {
                    _textures[index]?.Release();
                    _textures[index] = null;
                }

                Marshal.DestroyStructure(nativeFramebufferArray, typeof(FramebufferArray));
                Marshal.FreeHGlobal(nativeFramebufferArray);
            }

            private void reallocate(MDMVideoDesc displayConfig) {
                for (var index = 0; index < _textures.Length; index++) {
                    _textures[index]?.Release();

                    _textures[index] = createTexture(displayConfig);
                    _framebufferArray.framebuffers[index] = _textures[index].GetNativeTexturePtr();
                }

                Marshal.StructureToPtr(_framebufferArray, nativeFramebufferArray, false);

                _cursor = 0;
                _reallocated = true;
            }

            private RenderTexture createTexture(MDMVideoDesc displayConfig) {
                var texture = new RenderTexture(displayConfig.videoWidth, displayConfig.videoHeight, 0, RenderTextureFormat.BGRA32) {
                    useMipMap = false,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0
                };
                texture.Create();

                return texture;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct FramebufferArray {
                [MarshalAs(UnmanagedType.LPArray)]
                public IntPtr[] framebuffers;
                public int count;

                public FramebufferArray(int length) {
                    framebuffers = new IntPtr[length];
                    count = length;
                }
            }
        }

        private class Driver : MonoBehaviour {
            public static Driver Create() {
                var go = new GameObject("DisplayRendererDriver") {
                    hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
                };
                DontDestroyOnLoad(go);

                return go.AddComponent<Driver>();
            }
        }
    }
}

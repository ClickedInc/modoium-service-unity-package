using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace Modoium.Service {
    internal class MDMDisplayRenderer {
        private MDMInputProvider _inputProvider;
        private MonoBehaviour _driver;
        private CommandBuffer _commandBuffer;
        private SwapChain _swapChain;
        private Material _blitMaterial;
        private bool _running;

        public MDMDisplayRenderer(MonoBehaviour driver = null) {
            _driver = driver;
            _commandBuffer = new CommandBuffer();
        }

        public void Start(MDMInputProvider inputProvider, MDMVideoDesc displayConfig) {
            if (ModoiumPlugin.isXR || _running) { return; }
            _running = true;

            if (_blitMaterial == null) {
                _blitMaterial = new Material(Shader.Find("Modoium/Fullscreen Blit"));
            }
            _swapChain = new SwapChain(displayConfig, _blitMaterial);
            
            _inputProvider = inputProvider;
            startCoroutine(renderLoop(displayConfig));
        }

        public void Stop() {
            _running = false;
        }

        private IEnumerator renderLoop(MDMVideoDesc displayConfig) {
            var prevFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = Mathf.RoundToInt(displayConfig.framerate);

            ModoiumPlugin.RenderStart(_commandBuffer);
            flushCommandBuffer(_commandBuffer);

            while (_running) {
                yield return new WaitForEndOfFrame();
                if (_running == false) { break; }

                _inputProvider.Update();
                ModoiumPlugin.RenderUpdate(_commandBuffer);

                if (_swapChain.reallocated) {
                    ModoiumPlugin.RenderFramebuffersReallocated(_commandBuffer, _swapChain.nativeFramebufferArray);
                }

                ModoiumPlugin.RenderPreRender(_commandBuffer);
                _swapChain.CopyFrameBuffer(_commandBuffer, out var framebufferIndex, out var aspect);
                ModoiumPlugin.RenderPostRender(_commandBuffer, framebufferIndex, aspect);

                flushCommandBuffer(_commandBuffer);
            }

            ModoiumPlugin.RenderStop(_commandBuffer);
            flushCommandBuffer(_commandBuffer);

            _swapChain.Release();

            Application.targetFrameRate = prevFrameRate;
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
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            private const int Length = 1;
#else 
            private const int Length = 4;
#endif

            private Material _blitMaterial;
            private RenderTexture[] _textures;
            private RenderTexture _blitBufferTexture;
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

                    if (_blitBufferTexture != null) {
                        commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, _blitBufferTexture);
                        commandBuffer.Blit(_blitBufferTexture, texture, _blitMaterial);
                    }
                    else {
                        commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, texture, _blitMaterial);
                    }
                }

                framebufferIndex = _cursor;
                _cursor = (_cursor + 1) % _textures.Length;
            }

            public void Release() {
                for (var index = 0; index < _textures.Length; index++) {
                    _textures[index]?.Release();
                    _textures[index] = null;
                }

                _blitBufferTexture?.Release();
                _blitBufferTexture = null;

                Marshal.DestroyStructure(nativeFramebufferArray, typeof(FramebufferArray));
                Marshal.FreeHGlobal(nativeFramebufferArray);
            }

            private void reallocate(MDMVideoDesc displayConfig) {
                for (var index = 0; index < _textures.Length; index++) {
                    _textures[index]?.Release();

                    _textures[index] = createTexture(displayConfig.videoWidth, displayConfig.videoHeight);
                    _framebufferArray.framebuffers[index] = _textures[index].GetNativeTexturePtr();
                }

                Marshal.StructureToPtr(_framebufferArray, nativeFramebufferArray, false);

                // workaround: blitting framebuffer to render texture with material does not work, so should use buffer texture
                if (Application.isEditor == false) {
                    _blitBufferTexture = createTexture(displayConfig.contentWidth, displayConfig.contentHeight);
                }

                _cursor = 0;
                _reallocated = true;
            }

            private RenderTexture createTexture(int width, int height) {
                var texture = new RenderTexture(width, height, 0, RenderTextureFormat.BGRA32) {
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

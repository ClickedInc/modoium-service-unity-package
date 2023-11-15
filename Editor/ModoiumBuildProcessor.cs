using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Modoium.Service.Editor {
    public class ModoiumBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport {
        int IOrderedCallback.callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report) {
            removeSettingsFromPreloadAssets<ModoiumSettings>();
            addSettingsToPreloadAssets<ModoiumSettings>(ModoiumSettings.SettingsKey);
            
#if UNITY_XR_MANAGEMENT
            removeSettingsFromPreloadAssets<ModoiumXRSettings>();
            addSettingsToPreloadAssets<ModoiumXRSettings>(ModoiumXRSettings.SettingsKey);
#endif
        }

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report) {
            removeSettingsFromPreloadAssets<ModoiumSettings>();
            
#if UNITY_XR_MANAGEMENT
            removeSettingsFromPreloadAssets<ModoiumXRSettings>();
#endif
        }

        private void addSettingsToPreloadAssets<T>(string key) where T : UnityEngine.Object {
            EditorBuildSettings.TryGetConfigObject(key, out T settings);
            if (settings == null) { return; }

            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets.Contains(settings) == false) {
                var assets = preloadedAssets.ToList();
                assets.Add(settings);
                PlayerSettings.SetPreloadedAssets(assets.ToArray());
            }
        }

        private void removeSettingsFromPreloadAssets<T>() {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets == null) { return; }

            var oldSettings = preloadedAssets.Where((asset) => asset?.GetType() == typeof(T));
            if (oldSettings?.Any() ?? false) {
                var assets = preloadedAssets.ToList();
                foreach (var setting in oldSettings) {
                    assets.Remove(setting);
                }

                PlayerSettings.SetPreloadedAssets(assets.ToArray());
            }
        }
    }
}

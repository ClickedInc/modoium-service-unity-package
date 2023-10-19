using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Modoium.Service.Editor {
    public class ModoiumBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport {
        int IOrderedCallback.callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report) {
            cleanOldSettings();

            EditorBuildSettings.TryGetConfigObject(ModoiumSettings.SettingsKey, out ModoiumSettings settings);
            if (settings == null) { return; }

            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets.Contains(settings) == false) {
                var assets = preloadedAssets.ToList();
                assets.Add(settings);
                PlayerSettings.SetPreloadedAssets(assets.ToArray());
            }
        }

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report) {
            cleanOldSettings();   
        }

        private void cleanOldSettings() {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets == null) { return; }

            var oldSettings = preloadedAssets.Where((asset) => asset?.GetType() == typeof(ModoiumSettings));
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

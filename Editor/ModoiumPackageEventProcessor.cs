using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modoium.Service.Editor {
    [InitializeOnLoad]
    internal class ModoiumPackageEventProcessor {
        static ModoiumPackageEventProcessor() {
            Events.registeringPackages += onRegisteringPackages;
        }

        static void onRegisteringPackages(PackageRegistrationEventArgs args) {
            var packageWillBeRemoved = false;
            foreach (var info in args.removed) {
                if (info.name == "com.modoium.service") {
                    packageWillBeRemoved = true;
                    break;
                }
            }
            if (packageWillBeRemoved == false) { return; }

            ModoiumPlugin.ShutdownService();

            if (EditorUtility.DisplayDialog("Modoium Remote will be removed", 
                                            "We STRONGLY RECOMMEND restart the Unity editor to unload Modoium Remote completely. Do you want to restart the Unity editor?", 
                                            "Restart", 
                                            "Later")) {
                var projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 7);
                EditorApplication.OpenProject(projectPath);
            }
        }
    }
}


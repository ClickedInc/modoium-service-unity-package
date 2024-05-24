using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Modoium.Service.Editor {
    public class MDMRemoteDoctorWindow : EditorWindow {
        //[MenuItem("Modoium/Open Doctor...", false, 101)]
        public static void OpenWindow() {
            GetWindow<MDMRemoteDoctorWindow>().titleContent = new GUIContent {
                text = "Modoium Remote Doctor",
                image = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.modoium.service/Graphics/window_icon.png")
            };
        }

        private void OnGUI() {
            EditorGUILayout.LabelField("Doctor Window");
        }

        private class Styles {
            
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modoium.Service.Editor {
    internal class MDMRemoteStatusNoRunningCoreView : VisualElement {
#if UNITY_EDITOR_OSX
        //private static string ModoiumHubInstallerURL = "https://apps.apple.com/app-bundle/microsoft-365/id1450038993";
        private static string ModoiumHubInstallerURL = "https://cloud.onairvr.io/modoium/ModoiumHub-0.8.3.dmg";
#else
        //private static string ModoiumHubInstallerURL = "ms-windows-store://pdp?productid=xp89dcgq3k6vld";
        private static string ModoiumHubInstallerURL = "https://cloud.onairvr.io/modoium/ModoiumHub-0.8.3.exe";
#endif

        private Action<string> _onOpenUrl;

        public MDMRemoteStatusNoRunningCoreView(VisualElement parent, Action<string> onOpenUrl) {
            this.FillParent().Padding(10);

            Add(createBody());
            Add(createInstallStatement());

            parent.Add(this);
            _onOpenUrl = onOpenUrl;
        }

        public void UpdateView(MDMRemoteStatusWindow.State state) {
            if (state != MDMRemoteStatusWindow.State.NoRunningCore) {
                style.display = DisplayStyle.None;
                return;
            }
            style.display = DisplayStyle.Flex;
        }

        private VisualElement createBody() {
            return new TextElement { text = Styles.bodyText };
        }

#if UNITY_2021_3_OR_NEWER
        private VisualElement createInstallStatement() {
            return new TextElement { text = string.Format(Styles.bodyInstall, ModoiumHubInstallerURL) };
        }
#else
        private VisualElement createInstallStatement() {
            var container = new VisualElement().Horizontal();
            
            container.Add(new TextElement { text = Styles.labelInstall });

            var button = new HyperLinkTextElement { text = Styles.buttonInstall };
            button.RegisterCallback<MouseUpEvent>((evt) => _onOpenUrl(ModoiumHubInstallerURL));
            container.Add(button);

            return container;
        }
#endif

        private class Styles {
#if UNITY_2021_3_OR_NEWER
            public static string bodyText = "Please make sure <b>Modoium Hub</b> is installed and running.";
            //public static string bodyInstall = "\nNot installed yet? <a href=\"{0}\">Install Modoium Hub from App Store.</a>";
            public static string bodyInstall = "\nNot installed yet? <a href=\"{0}\">Download & Install Modoium Hub.</a>";
#else
            public static string bodyText = "Please make sure Modoium Hub is installed and running.\n";
            public static string labelInstall = "Not installed yet? ";
            //public static string buttonInstall = "Install Modoium Hub from App Store.";
            public static string buttonInstall = "Download & Install Modoium Hub.";
#endif
        }
    }
}

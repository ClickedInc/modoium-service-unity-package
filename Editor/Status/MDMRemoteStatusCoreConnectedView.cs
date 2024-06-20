using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modoium.Service.Editor {
    internal class MDMRemoteStatusCoreConnectedView : VisualElement {
        private static string ModoiumAndroidAppURL = "https://cloud.onairvr.io/modoium/modoium-app_0.8.3_b202405301.apk";
        //private static string ModoiumAndroidAppURL = "https://play.google.com/store/apps/details?id=com.microsoft.launcher";
        //private static string ModoiumiOSAppURL = "https://apps.apple.com/us/app/microsoft-start/id945416273";
        //private static string ModoiumQuestAppURL = "https://drive.google.com/file/d/1IGsdzlemllTAMkI7j7Pg0GPbNIwRbECJ/view?usp=sharing";

        private Action<string> _onOpenUrl;
        private TextElement _bodyStatus;
        private MDMRemoteStatusVideoBitrate _videoBitrate;
        private TextElement _hostName;
        private TextElement _serviceName;
        private TextElement _verificationCode;

        public MDMRemoteStatusCoreConnectedView(VisualElement parent, Action<string> onOpenUrl) {
            this.FillParent().Padding(10);

            Add(createStatus());
            Add(createInfo());
            Add(createDivider());
            Add(createClientGuidelineIntro());
            Add(createClientGuidelineStep1());
            Add(createClientInstallFoldout());
            Add(createClientGuidelineStep2());
            Add(createClientGuidelineStep3());
            Add(createServiceInfo());
            Add(createClientGuidelineStep4());
            Add(createVerificationCode());

            parent.Add(this);
            _onOpenUrl = onOpenUrl;
        }

        public void UpdateView(MDMRemoteStatusWindow.State state, 
                               bool embeddedCoreRunning,
                               float videoBitrate, 
                               string hostname, 
                               string servname, 
                               string verificationCode) {
            style.display = state == MDMRemoteStatusWindow.State.CoreConnected ? 
                DisplayStyle.Flex : DisplayStyle.None;

            updateStatus(embeddedCoreRunning);
            _videoBitrate.UpdateView(videoBitrate, embeddedCoreRunning == false);
            updateServiceInfo(hostname, servname);
            updateVerificationCode(verificationCode);
        }

        private VisualElement createStatus() {
            _bodyStatus = new TextElement { text = Styles.bodyStatusHubConnected };
            _bodyStatus.style.unityFontStyleAndWeight = FontStyle.Bold;
            _bodyStatus.style.display = DisplayStyle.None;

            return _bodyStatus;
        }

        private VisualElement createInfo() {
            var box = new Box().Padding(4);
            box.style.marginTop = 4;

            _videoBitrate = new MDMRemoteStatusVideoBitrate(box);

            return box;
        }

        private VisualElement createDivider() {
            return new Box().Divider();
        }

        private VisualElement createClientGuidelineIntro() {
            return new TextElement { text = Styles.bodyClientGuidelineIntro };
        }

        private VisualElement createClientGuidelineStep1() {
            return new TextElement { text = Styles.bodyClientGuidelineStep1 };
        }

        private VisualElement createClientInstallFoldout() {
            var foldout = new Foldout { 
                text = Styles.foldoutClientInstall,
                value = false
            };
            foldout.style.marginLeft = 10;

            foldout.Add(createClientInstallAndroid());
            foldout.Add(createClientInstalliOS());
            foldout.Add(createClientInstallQuest());
            return foldout;
        }

        private VisualElement createClientGuidelineStep2() {
            return new TextElement { text = Styles.bodyClientGuidelineStep2 };
        }

        private VisualElement createClientGuidelineStep3() {
            return new TextElement { text = Styles.bodyClientGuidelineStep3 };
        }

        private VisualElement createServiceInfo() {
            var container = new Box().Padding(6);
            container.style.marginLeft = 14;

            _hostName = new TextElement { text = string.Empty };
            container.Add(_hostName);

            _serviceName = new TextElement { text = string.Empty };
            _serviceName.style.fontSize = 14;
            _serviceName.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(_serviceName);
            
            return container;
        }

        private VisualElement createClientGuidelineStep4() {
            return new TextElement { text = Styles.bodyClientGuidelineStep4 };
        }

        private VisualElement createVerificationCode() {
            var container = new Box().Horizontal().Padding(6);
            container.style.marginLeft = 14;

            _verificationCode = new TextElement { text = string.Empty };
            _verificationCode.style.fontSize = 14;
            _verificationCode.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(_verificationCode);

            return container;
        }

        private void updateStatus(bool embeddedCoreRunning) {
            _bodyStatus.style.display = embeddedCoreRunning ? DisplayStyle.None : DisplayStyle.Flex;   
        }

        private void updateServiceInfo(string hostname, string servname) {
            _serviceName.text = servname;
            _hostName.text = hostname;
        }

        private void updateVerificationCode(string code) {
            _verificationCode.text = code;
        }

#if UNITY_2022_3_OR_NEWER
        private VisualElement createClientInstallAndroid() {
            return new TextElement { text = string.Format(Styles.bodyClientInstallAndroid, ModoiumAndroidAppURL) };
        }

        private VisualElement createClientInstalliOS() {
            return new TextElement { text = Styles.bodyClientInstalliOS };
            //return new TextElement { text = string.Format(Styles.bodyClientInstalliOS, ModoiumiOSAppURL) };
        }

        private VisualElement createClientInstallQuest() {
            return new TextElement { text = Styles.bodyClientInstallQuest };
            //return new TextElement { text = string.Format(Styles.bodyClientInstallQuest, ModoiumQuestAppURL) };
        }
#else
        private VisualElement createClientInstallAndroid() {
            var container = new VisualElement().Horizontal();

            container.Add(new TextElement { text = Styles.bodyClientInstallAndroidPrefix });

            var button = new HyperLinkTextElement { text = Styles.modoium };
            button.RegisterCallback<MouseUpEvent>((evt) => _onOpenUrl(ModoiumAndroidAppURL));
            container.Add(button);

            container.Add(new TextElement { text = Styles.bodyClientInstallAndroidSuffix });

            return container;
        }

        private VisualElement createClientInstalliOS() {
            var container = new VisualElement().Horizontal();

            container.Add(new TextElement { text = Styles.bodyClientInstalliOSPrefix });

            /* var button = new HyperLinkTextElement { text = Styles.modoium };
            button.RegisterCallback<MouseUpEvent>((evt) => _onOpenUrl(ModoiumiOSAppURL));
            container.Add(button);

            container.Add(new TextElement { text = Styles.bodyClientInstalliOSSuffix }); */

            return container;
        }

        private VisualElement createClientInstallQuest() {
            var container = new VisualElement().Horizontal();

            container.Add(new TextElement { text = Styles.bodyClientInstallQuestPrefix });

            /* var button = new HyperLinkTextElement { text = Styles.linkClientInstallQuest };
            button.RegisterCallback<MouseUpEvent>((evt) => _onOpenUrl(ModoiumQuestAppURL));
            container.Add(button); */

            return container;
        }
#endif

        private class Styles {
            public static string bodyStatusHubConnected = $"Modoium Hub connected";
            public static string bodyClientGuidelineIntro = "How to connect from your mobile device :";
            public static string foldoutClientInstall = "Not installed yet?";
            public static string bodyClientGuidelineStep2 = "2) Make sure your mobile device is connected to the same network as this computer.";
            public static string bodyClientGuidelineStep4 = "4) Enter the verification code below if necessary :";

#if UNITY_2022_3_OR_NEWER
            public static string bodyClientGuidelineStep1 = "1) Run <b>Modoium</b> app.";
            //public static string bodyClientInstallAndroid = "\u2022 Android : Get <a href=\"{0}\">Modoium</a> from Google Play Store";
            public static string bodyClientInstallAndroid = "\u2022 Android : Download & Install <a href=\"{0}\">Modoium</a> app.";
            //public static string bodyClientInstalliOS = "\u2022 iOS : Get <a href=\"{0}\">Modoium</a> from App Store";
            public static string bodyClientInstalliOS = "\u2022 iOS : Not availble yet.";
            //public static string bodyClientInstallQuest = "\u2022 Quest : <a href=\"{0}\">Download and install Modoium APK</a>";
            public static string bodyClientInstallQuest = "\u2022 Quest : Not availble yet.";
            public static string bodyClientGuidelineStep3 = "3) Select your project from the list on <b>Modoium</b> app :";
#else
            public static string modoium = "Modoium";
            public static string bodyClientGuidelineStep1 = "1) Run Modoium app.";
            //public static string bodyClientInstallAndroidPrefix = "\u2022 Android : Get ";
            //public static string bodyClientInstallAndroidSuffix = " from Google Play Store";
            public static string bodyClientInstallAndroidPrefix = "\u2022 Android : Download & Install ";
            public static string bodyClientInstallAndroidSuffix = " app.";
            //public static string bodyClientInstalliOSPrefix = "\u2022 iOS : Get ";
            public static string bodyClientInstalliOSPrefix = "\u2022 iOS : Not availble yet.";
            public static string bodyClientInstalliOSSuffix = " from App Store";
            //public static string bodyClientInstallQuestPrefix = "\u2022 Quest : ";
            public static string bodyClientInstallQuestPrefix = "\u2022 Quest : Not availble yet.";
            public static string linkClientInstallQuest = "Download and install Modoium APK";
            public static string bodyClientGuidelineStep3 = "3) Select your project from the list on Modoium app :";
#endif
        }
    }    
}

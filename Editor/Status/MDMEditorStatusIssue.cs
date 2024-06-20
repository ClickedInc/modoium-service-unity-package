using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modoium.Service.Editor {
    internal class MDMRemoteIssueModoiumHubNotConnected : MDMRemoteIssue {
#if UNITY_EDITOR_OSX
        //private static string ModoiumHubInstallerURL = "https://apps.apple.com/app-bundle/microsoft-365/id1450038993";
        private static string ModoiumHubInstallerURL = "https://cloud.onairvr.io/modoium/ModoiumHub-0.8.3.dmg";
#else
        //private static string ModoiumHubInstallerURL = "ms-windows-store://pdp?productid=xp89dcgq3k6vld";
        private static string ModoiumHubInstallerURL = "https://cloud.onairvr.io/modoium/ModoiumHub-0.8.3.exe";
#endif

        public static bool IssueExists(MDMService service) => service.embeddedCoreRunning;

        public static string ID = "modoium-hub-not-connected";

        private MDMService _service;

        public MDMRemoteIssueModoiumHubNotConnected(MDMService service) {
            _service = service;
        }

        // implements MDMRemoteIssue
        public override string id => ID;
        public override Level level => Level.Info;

#if UNITY_2022_3_OR_NEWER
        public override string message => "Connect to <b>Modoium Hub</b> app for access to more powerful capabilities.";
#else
        public override string message => "Connect to Modoium Hub app for access to more powerful capabilities.";
#endif

        public override (string label, Action action)[] actions => new (string, Action)[] {
            ("Connect to Modoium Hub", () => {
                _service.SearchForModoiumHub();
            }),
            ("Get Modoium Hub from Store", () => {
                System.Diagnostics.Process.Start(ModoiumHubInstallerURL);
            })
        };
    }
}

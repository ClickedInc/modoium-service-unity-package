using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modoium.Service.Editor {
    internal class MDMRemoteIssueRemoteGameViewNotSelected : MDMRemoteIssue {
        public static string ID = "remote-game-view-not-selected";

        private MDMService _service;

        public MDMRemoteIssueRemoteGameViewNotSelected(MDMService service) {
            _service = service;
        }

        public static bool IssueExists(MDMService service) {
            if (service.remoteViewConnected == false) { return false; }

            return ModoiumPlugin.isXR == false &&
                   service.displayConfigurator.currentSizeLabel != MDMDisplayConfigurator.RemoteSizeLabel;
        }

        // implememts MDMRemoteIssue
        public override string id => ID;
        public override Level level => Level.Warning;

    #if UNITY_2022_3_OR_NEWER
        public override string message => "<b>Game View</b> follows the connected device screen size only when the size <b>\"Modoium Remote\"</b> is selected.";
    #else
        public override string message => "Game View follows the connected device screen size only when the size \"Modoium Remote\" is selected.";
    #endif

        public override (string label, Action action)[] actions => new (string, Action)[] {
            ("Fix", () => {
                _service.displayConfigurator.SelectRemoteSize();
            })
        };
    }

    internal class MDMRemoteIssueDeviceNotSupportXR : MDMRemoteIssue {
        public static string ID = "device-not-support-xr";

        public static bool IssueExists(MDMService service) {
            if (service.remoteViewConnected == false) { return false; }

            return (service.remoteViewDesc is MDMStereoVideoDesc) == false && ModoiumPlugin.isXR;
        }

        // implements MDMRemoteIssue
        public override string id => ID;
        public override Level level => Level.Warning;
        public override string message => "The connected device does not support XR.";
        public override (string label, Action action)[] actions => null;
    }

    internal class MDMRemoteIssueNotRunInBackground : MDMRemoteIssue {
        public static string ID = "not-run-in-background";

        public static bool IssueExists() => Application.runInBackground == false;

        // implements MDMRemoteIssue
        public override string id => ID;
        public override Level level => Level.Info;
    #if UNITY_2022_3_OR_NEWER
        public override string message => "<b>Run In Background</b> in Standalone Player Settings is strongly recommended for better experience.";
    #else
        public override string message => "Run In Background in Standalone Player Settings is strongly recommended for better experience.";
    #endif
        public override (string label, Action action)[] actions => null;
    }

    internal class MDMRemoteIssueInputSystemNotEnabled : MDMRemoteIssue {
        private static string LearnMoreURL = "https://clickedcorp.notion.site/Known-Issues-16f7711b0c8843cd9f660dd2bd66aa52";

        public static string ID = "input-system-not-enabled";

        public static bool IssueExists() {
            if (ModoiumPlugin.isXR) { return false; }

#if UNITY_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return false;
#else
            return true;
#endif
        }

        // implements MDMRemoteIssue
        public override string id => ID;
        public override Level level => Level.Info;
#if UNITY_2022_3_OR_NEWER
        public override string message => "Consider using only <b>Input System</b>, or if you use UnityEngine.Input directly you should use <b>Modoium.Service.Input</b> instead.";
#else
        public override string message => "Consider using only Input System, or if you use UnityEngine.Input directly you should use Modoium.Service.Input instead.";
#endif
        public override (string label, Action action)[] actions => new (string, Action)[] {
            ("Learn More..", () => {
                System.Diagnostics.Process.Start(LearnMoreURL);
            })
        };
    }
}

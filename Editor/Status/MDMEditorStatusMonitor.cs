using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modoium.Service.Editor {
    internal class MDMEditorStatusMonitor {
        private const float IntervalToCheckIssues = 0.2f;

        private MDMService _service;
        private float _remainingToCheckIssues;

        public Dictionary<string, MDMRemoteIssue> issues { get; private set; }
        public bool hasIssues => issues != null && issues.Count > 0;

        public MDMEditorStatusMonitor(MDMService service) {
            _service = service;
        }

        public bool Update() {
            _remainingToCheckIssues -= Time.unscaledDeltaTime;
            if (_remainingToCheckIssues > 0) { return false; }

            _remainingToCheckIssues = IntervalToCheckIssues;

            return checkIssues();
        }

        private bool checkIssues() {
            var updated = false;

            if (issues == null) {
                issues = new Dictionary<string, MDMRemoteIssue>();
                updated = true;
            }

            checkIfModoiumNotConnected(ref updated);

            return updated;
        }

        private void checkIfModoiumNotConnected(ref bool updated) {
            var issueExists = MDMRemoteIssueModoiumHubNotConnected.IssueExists(_service);
            if (issues.ContainsKey(MDMRemoteIssueModoiumHubNotConnected.ID) == issueExists) { return; }

            if (issueExists) {
                issues.Add(MDMRemoteIssueModoiumHubNotConnected.ID, 
                           new MDMRemoteIssueModoiumHubNotConnected(_service));
            }
            else {
                issues.Remove(MDMRemoteIssueModoiumHubNotConnected.ID);
            }

            updated = true;
        }
    }
}

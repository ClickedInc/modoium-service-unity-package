using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modoium.Service.Editor {
    internal class MDMEditorStatusMonitor {
        public struct Issue {
            public enum Level {
                Warining
            }

            public enum Code {
                Unknown
            }

            public Level level;
            public Code code;
        }

        private MDMService _service;

        public List<Issue> issues { get; private set; }

        public MDMEditorStatusMonitor(MDMService service) {
            _service = service;
            issues = new List<Issue>();

            /* issues.Add(new Issue {
                level = Issue.Level.Warining,
                code = Issue.Code.Unknown
            }); */
        }

        public void Update() {

        }
    }
}

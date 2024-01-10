using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modoium.Service {
    internal class MDMServiceConfiguator {
        private const float DelayToApplyAfterEdit = 1.5f;

        private string _lastServiceName;
        private string _serviceNameInEdit;
        private float _timeToApply = -1f;

        private string currentServiceName => Application.productName;

        public MDMServiceConfiguator() {
            _lastServiceName = currentServiceName;
        }

        public void Update() {
            if (Application.isEditor == false ||
                string.IsNullOrEmpty(currentServiceName)) { return; }

            if (currentServiceName == _lastServiceName) {
                _serviceNameInEdit = string.Empty;
                _timeToApply = -1f;
                return;
            }

            var now = Time.realtimeSinceStartup;
            if (currentServiceName != _serviceNameInEdit) {
                _serviceNameInEdit = currentServiceName;
                _timeToApply = now + DelayToApplyAfterEdit;
            }
            if (now < _timeToApply) { return; }

            applyConfigs();

            _lastServiceName = currentServiceName;
            _serviceNameInEdit = string.Empty;
            _timeToApply = -1f;
        }

        private void applyConfigs() {
            var settings = ModoiumSettings.instance;

            ModoiumPlugin.ChangeServiceProps(settings.serviceName);
        }
    }
}

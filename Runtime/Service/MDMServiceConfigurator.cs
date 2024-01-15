using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modoium.Service {
    internal class MDMServiceConfigurator {
        private const float DelayToApplyAfterEdit = 1.5f;

        private string _serviceNameInEdit;
        private float _timeToApply = -1f;

        private string currentServiceName => Application.productName;

        public MDMServiceConfigurator() {
            if (string.IsNullOrEmpty(currentServiceName) == false &&
                string.IsNullOrEmpty(ModoiumPlugin.serviceConfigurator_serviceName)) {
                ModoiumPlugin.serviceConfigurator_serviceName = currentServiceName;
            }
        }

        public void Update() {
            if (Application.isEditor == false ||
                string.IsNullOrEmpty(currentServiceName)) { return; }

            if (currentServiceName == ModoiumPlugin.serviceConfigurator_serviceName) {
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

            ModoiumPlugin.serviceConfigurator_serviceName = currentServiceName;
            _serviceNameInEdit = string.Empty;
            _timeToApply = -1f;
        }

        private void applyConfigs() {
            var settings = ModoiumSettings.instance;

            ModoiumPlugin.ChangeServiceProps(settings.serviceName);
        }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Platform.Registry.Sync.Service.Runtime {
    using Microsoft.Azure.IIoT.Platform.Registry.Runtime;
    using Microsoft.Azure.IIoT.Platform.Registry;
    using Microsoft.Azure.IIoT.Platform.Edge;
    using Microsoft.Azure.IIoT.Azure.AppInsights.Runtime;
    using Microsoft.Azure.IIoT.Azure.ServiceBus;
    using Microsoft.Azure.IIoT.Azure.ServiceBus.Runtime;
    using Microsoft.Azure.IIoT.Azure.IoTHub.Runtime;
    using Microsoft.Azure.IIoT.Azure.IoTHub;
    using Microsoft.Azure.IIoT.AspNetCore.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Registry sync configuration
    /// </summary>
    public class Config : AppInsightsConfig, IIoTHubConfig, IServiceBusConfig,
        IActivationSyncConfig, IServiceEndpoint, IWriterGroupOrchestrationConfig,
        IMetricServerConfig, ISettingsSyncConfig {

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string ServiceBusConnString => _sb.ServiceBusConnString;
        /// <inheritdoc/>
        public TimeSpan? ActivationSyncInterval => _sync.ActivationSyncInterval;
        /// <inheritdoc/>
        public TimeSpan? UpdatePlacementInterval => _or.UpdatePlacementInterval;

        /// <inheritdoc/>
        public TimeSpan? SettingSyncInterval => _ep.SettingSyncInterval;
        /// <inheritdoc/>
        public string ServiceEndpoint => _ep.ServiceEndpoint;
        /// <inheritdoc/>
        public event EventHandler OnServiceEndpointUpdated {
            add => _ep.OnServiceEndpointUpdated += value;
            remove => _ep.OnServiceEndpointUpdated -= value;
        }

        /// <inheritdoc/>
        public int Port => 9505;

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _sb = new ServiceBusConfig(configuration);
            _hub = new IoTHubConfig(configuration);
            _sync = new ActivationSyncConfig(configuration);
            _ep = new SettingsSyncConfig(configuration);
            _or = new OrchestrationConfig(configuration);
        }

        private readonly IServiceBusConfig _sb;
        private readonly IIoTHubConfig _hub;
        private readonly SettingsSyncConfig _ep;
        private readonly ActivationSyncConfig _sync;
        private readonly OrchestrationConfig _or;
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Azure.LogAnalytics {
    using Microsoft.Azure.IIoT.Azure.LogAnalytics.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Autofac;

    /// <summary>
    /// Prometheus module
    /// </summary>
    public class PrometheusLogAnalyticsCollector : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            // Register prometheus logging
            builder.RegisterType<PrometheusCollectorHost>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<LogAnalyticsConfig>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<LogAnalyticsMetricsHandler>()
                .AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(
                    PropertyWiringOptions.AllowCircularDependencies);
        }
    }
}

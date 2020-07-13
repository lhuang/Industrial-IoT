// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Serilog;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Performs continous endpoint placement of writer groups
    /// </summary>
    public sealed class OrchestrationHost : AbstractRunHost {

        /// <summary>
        /// Create process
        /// </summary>
        /// <param name="orchestrator"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public OrchestrationHost(IPublisherOrchestration orchestrator,
            ILogger logger, IOrchestrationConfig config = null) :
            base(logger, "Service Endpoint Update",
                config?.UpdatePlacementInterval ?? TimeSpan.FromMinutes(3)) {
            _orchestrator = orchestrator ??
                throw new ArgumentNullException(nameof(orchestrator));
        }

        /// <inheritdoc/>
        protected override Task RunAsync(CancellationToken token) {
            return _orchestrator.SynchronizeWriterGroupPlacementsAsync(token);
        }

        private readonly IPublisherOrchestration _orchestrator;
    }
}

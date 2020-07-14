// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controllers {
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using System;

    /// <summary>
    /// Supervisor settings controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    public class SupervisorSettingsController : ISettingsController, IServiceEndpoint {

        /// <inheritdoc/>
        public string ServiceEndpoint {
            get => _serviceEndpoint;
            set {
                _serviceEndpoint = value;
                OnServiceEndpointUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        public event EventHandler OnServiceEndpointUpdated;

        private string _serviceEndpoint;
    }
}

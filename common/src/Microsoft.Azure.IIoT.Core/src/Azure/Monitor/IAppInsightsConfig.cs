// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Azure.AppInsights {
    using Microsoft.Azure.IIoT.Diagnostics;

    /// <summary>
    /// AppInsights configuration
    /// </summary>
    public interface IAppInsightsConfig : IDiagnosticsConfig {

        /// <summary>
        /// Instrumentation key if it exists
        /// </summary>
        string InstrumentationKey { get; }
    }
}

// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Implementation of twin and job services, talking to iot hub
    /// directly. Alternatively, there is a sdk based implementation
    /// in the Hub.Client nuget package that can also be used.
    /// </summary>
    public sealed class IoTHubServiceHttpClient : IIoTHubTwinServices, IHealthCheck {

        /// <summary>
        /// The host name the client is talking to
        /// </summary>
        public string HostName => HubConnectionString.HostName;

        /// <summary>
        /// Hub connection string to use
        /// </summary>
        ConnectionString HubConnectionString {
            get {
                if (_connectionString == null) {
                    // Lazy parse and return
                    if (!ConnectionString.TryParse(_config.IoTHubConnString,
                            out _connectionString)) {
                        throw new InvalidConfigurationException(
                            "No or bad IoT Hub owner connection string in configuration.");
                    }
                }
                return _connectionString;
            }
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public IoTHubServiceHttpClient(IHttpClient httpClient,
            IIoTHubConfig config, IJsonSerializer serializer, ILogger logger) {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken ct) {
            try {
                await QueryAsync("SELECT * FROM devices", null, 1, ct);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex) {
                return new HealthCheckResult(context.Registration.FailureStatus,
                    exception: ex);
            }
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel twin, bool force,
            CancellationToken ct) {
            if (twin == null) {
                throw new ArgumentNullException(nameof(twin));
            }
            if (string.IsNullOrEmpty(twin.Id)) {
                throw new ArgumentNullException(nameof(twin.Id));
            }
            // Retry transient errors
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                // First try create device
                try {
                    var device = NewRequest($"/devices/{twin.Id}");
                    _serializer.SerializeToRequest(device, new {
                        deviceId = twin.Id,
                        capabilities = twin.Capabilities
                    });
                    var response = await _httpClient.PutAsync(device, ct);
                    response.Validate();
                }
                catch (ConflictingResourceException)
                    when (!string.IsNullOrEmpty(twin.ModuleId) || force) {
                    // Continue onward
                }
                if (!string.IsNullOrEmpty(twin.ModuleId)) {
                    // Try create module
                    try {
                        var module = NewRequest(
                            $"/devices/{twin.Id}/modules/{twin.ModuleId}");
                        _serializer.SerializeToRequest(module, new {
                            deviceId = twin.Id,
                            moduleId = twin.ModuleId
                        });
                        var response = await _httpClient.PutAsync(module, ct);
                        response.Validate();
                    }
                    catch (ConflictingResourceException)
                        when (force) {
                    }
                }
                return await PatchAsync(twin, true, ct);  // Force update of twin
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> PatchAsync(DeviceTwinModel twin, bool force, CancellationToken ct) {
            if (twin == null) {
                throw new ArgumentNullException(nameof(twin));
            }
            if (string.IsNullOrEmpty(twin.Id)) {
                throw new ArgumentNullException(nameof(twin.Id));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {

                // Then update twin assuming it now exists. If fails, retry...
                var patch = NewRequest(
                    $"/twins/{ToResourceId(twin.Id, twin.ModuleId)}");
                patch.Headers.Add("If-Match",
                     $"\"{(string.IsNullOrEmpty(twin.Etag) || force ? "*" : twin.Etag)}\"");
                if (!string.IsNullOrEmpty(twin.ModuleId)) {

                    // Patch module
                    _serializer.SerializeToRequest(patch, new {
                        deviceId = twin.Id,
                        moduleId = twin.ModuleId,
                        tags = twin.Tags ?? new Dictionary<string, VariantValue>(),
                        properties = new {
                            desired = twin.Properties?.Desired ?? new Dictionary<string, VariantValue>()
                        }
                    });
                }
                else {
                    // Patch device
                    _serializer.SerializeToRequest(patch, new {
                        deviceId = twin.Id,
                        tags = twin.Tags ?? new Dictionary<string, VariantValue>(),
                        properties = new {
                            desired = twin.Properties?.Desired ?? new Dictionary<string, VariantValue>()
                        }
                    });
                }
                {
                    var response = await _httpClient.PatchAsync(patch, ct);
                    response.Validate();
                    var result = _serializer.DeserializeResponse<DeviceTwinModel>(response);
                    _logger.Information(
                        "{id} ({moduleId}) created or updated ({twinEtag} -> {resultEtag})",
                        twin.Id, twin.ModuleId ?? string.Empty, twin.Etag ?? "*", result.Etag);
                    return result;
                }
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<MethodResultModel> CallMethodAsync(string deviceId, string moduleId,
            MethodParameterModel parameters, CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            if (parameters == null) {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (string.IsNullOrEmpty(parameters.Name)) {
                throw new ArgumentNullException(nameof(parameters.Name));
            }
            var request = NewRequest(
                $"/twins/{ToResourceId(deviceId, moduleId)}/methods");

            _serializer.SerializeToRequest(request, new {
                methodName = parameters.Name,
                // TODO: Add timeouts...
                // responseTimeoutInSeconds = ...
                payload = _serializer.Parse(parameters.JsonPayload)
            });
            var response = await _httpClient.PostAsync(request, ct);
            response.Validate();
            var result = _serializer.ParseResponse(response);
            return new MethodResultModel {
                JsonPayload = _serializer.SerializeToString(result["payload"]),
                Status = (int)result["status"]
            };
        }

        /// <inheritdoc/>
        public Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, VariantValue> properties, string etag, CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}");
                _serializer.SerializeToRequest(request, new {
                    deviceId,
                    properties = new {
                        desired = properties ?? new Dictionary<string, VariantValue>()
                    }
                });
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.PatchAsync(request, ct);
                response.Validate();
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task ApplyConfigurationAsync(string deviceId,
            ConfigurationContentModel configuration, CancellationToken ct) {
            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, null)}/applyConfigurationContent");
                _serializer.SerializeToRequest(request, configuration);
                var response = await _httpClient.PostAsync(request, ct);
                response.Validate();
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> GetAsync(string deviceId, string moduleId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request, ct);
                response.Validate();
                return _serializer.DeserializeResponse<DeviceTwinModel>(response);
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceModel> GetRegistrationAsync(string deviceId, string moduleId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request, ct);
                response.Validate();
                return ToDeviceRegistrationModel(_serializer.ParseResponse(response));
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<QueryResultModel> QueryAsync(string query, string continuation,
            int? pageSize, CancellationToken ct) {
            if (string.IsNullOrEmpty(query)) {
                throw new ArgumentNullException(nameof(query));
            }
            var request = NewRequest("/devices/query");
            if (continuation != null) {
                _serializer.DeserializeContinuationToken(continuation,
                    out query, out continuation, out pageSize);
                request.Headers.Add(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.Headers.Add(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SerializeToRequest(request, new {
                query
            });
            var response = await _httpClient.PostAsync(request, ct);
            response.Validate();
            if (response.Headers.TryGetValues(HttpHeader.ContinuationToken, out var values)) {
                continuation = _serializer.SerializeContinuationToken(
                    query, values.First(), pageSize);
            }
            else {
                continuation = null;
            }
            var results = _serializer.ParseResponse(response);
            return new QueryResultModel {
                ContinuationToken = continuation,
                Result = results.Values
            };
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string deviceId, string moduleId, string etag,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            etag = null; // TODO : Fix - Currently prevents internal server error
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.DeleteAsync(request, ct);
                response.Validate();
            }, kMaxRetryCount);
        }

        /// <summary>
        /// Convert json to registration
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static DeviceModel ToDeviceRegistrationModel(VariantValue result) {
            return new DeviceModel {
                Etag = (string)result["etag"],
                Id = (string)result["deviceId"],
                ModuleId = (string)result["moduleId"],
                Status = ((string)result["status"])?.ToLowerInvariant(),
                ConnectionState = (string)result["connectionState"],
                Authentication = new DeviceAuthenticationModel {
                    PrimaryKey = (string)result["authentication"]["symmetricKey"]["primaryKey"],
                    SecondaryKey = (string)result["authentication"]["symmetricKey"]["secondaryKey"]
                }
            };
        }

        /// <summary>
        /// Helper to create new request
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private IHttpRequest NewRequest(string path) {
            var request = _httpClient.NewRequest(new UriBuilder {
                Scheme = "https",
                Host = HubConnectionString.HostName,
                Path = path,
                Query = "api-version=" + kApiVersion
            }.Uri);
            request.Headers.Add(HttpRequestHeader.Authorization.ToString(),
                CreateSasToken(HubConnectionString, 3600));
            request.Headers.Add(HttpRequestHeader.UserAgent.ToString(), kClientId);
            return request;
        }

        /// <summary>
        /// Helper to create resource path for device and optional module
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        private static string ToResourceId(string deviceId, string moduleId) {
            return string.IsNullOrEmpty(moduleId) ? deviceId : $"{deviceId}/modules/{moduleId}";
        }

        /// <summary>
        /// Create a token for iothub from connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="validityPeriodInSeconds"></param>
        /// <returns></returns>
        private static string CreateSasToken(ConnectionString connectionString,
            int validityPeriodInSeconds) {
            // http://msdn.microsoft.com/en-us/library/azure/dn170477.aspx
            // signature is computed from joined encoded request Uri string and expiry string
            var expiryTime = DateTime.UtcNow + TimeSpan.FromSeconds(validityPeriodInSeconds);
            var expiry = ((long)(expiryTime -
                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            var encodedScope = Uri.EscapeDataString(connectionString.HostName);
            // the connection string signature is base64 encoded
            var key = connectionString.SharedAccessKey.DecodeAsBase64();
            using (var hmac = new HMACSHA256(key)) {
                var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedScope + "\n" + expiry))
                    .ToBase64String();
                return $"SharedAccessSignature sr={encodedScope}" +
                    $"&sig={Uri.EscapeDataString(sig)}&se={Uri.EscapeDataString(expiry)}" +
                    $"&skn={Uri.EscapeDataString(connectionString.SharedAccessKeyName)}";
            }
        }

        private const string kApiVersion = "2020-03-01";
        private const string kClientId = "AzureIIoT";
        private const int kMaxRetryCount = 4;

        private readonly IIoTHubConfig _config;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private ConnectionString _connectionString;
    }
}

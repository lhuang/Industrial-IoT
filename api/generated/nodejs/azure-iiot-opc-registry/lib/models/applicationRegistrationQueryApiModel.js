/*
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 *
 * Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
 * Changes may cause incorrect behavior and will be lost if the code is
 * regenerated.
 */

'use strict';

/**
 * Application information
 *
 */
class ApplicationRegistrationQueryApiModel {
  /**
   * Create a ApplicationRegistrationQueryApiModel.
   * @property {string} [applicationType] Possible values include: 'Server',
   * 'Client', 'ClientAndServer', 'DiscoveryServer'
   * @property {string} [applicationUri] Application uri
   * @property {string} [productUri] Product uri
   * @property {string} [applicationName] Name of application
   * @property {string} [locale] Locale of application name - default is "en"
   * @property {string} [capability] Application capability to query with
   * @property {string} [discoveryProfileUri] Discovery profile uri
   * @property {string} [gatewayServerUri] Gateway server uri
   * @property {string} [siteOrGatewayId] Supervisor or site the application
   * belongs to.
   * @property {boolean} [includeNotSeenSince] Whether to include apps that
   * were soft deleted
   * @property {string} [discovererId] Discoverer id to filter with
   */
  constructor() {
  }

  /**
   * Defines the metadata of ApplicationRegistrationQueryApiModel
   *
   * @returns {object} metadata of ApplicationRegistrationQueryApiModel
   *
   */
  mapper() {
    return {
      required: false,
      serializedName: 'ApplicationRegistrationQueryApiModel',
      type: {
        name: 'Composite',
        className: 'ApplicationRegistrationQueryApiModel',
        modelProperties: {
          applicationType: {
            required: false,
            serializedName: 'applicationType',
            type: {
              name: 'Enum',
              allowedValues: [ 'Server', 'Client', 'ClientAndServer', 'DiscoveryServer' ]
            }
          },
          applicationUri: {
            required: false,
            serializedName: 'applicationUri',
            type: {
              name: 'String'
            }
          },
          productUri: {
            required: false,
            serializedName: 'productUri',
            type: {
              name: 'String'
            }
          },
          applicationName: {
            required: false,
            serializedName: 'applicationName',
            type: {
              name: 'String'
            }
          },
          locale: {
            required: false,
            serializedName: 'locale',
            type: {
              name: 'String'
            }
          },
          capability: {
            required: false,
            serializedName: 'capability',
            type: {
              name: 'String'
            }
          },
          discoveryProfileUri: {
            required: false,
            serializedName: 'discoveryProfileUri',
            type: {
              name: 'String'
            }
          },
          gatewayServerUri: {
            required: false,
            serializedName: 'gatewayServerUri',
            type: {
              name: 'String'
            }
          },
          siteOrGatewayId: {
            required: false,
            serializedName: 'siteOrGatewayId',
            type: {
              name: 'String'
            }
          },
          includeNotSeenSince: {
            required: false,
            serializedName: 'includeNotSeenSince',
            type: {
              name: 'Boolean'
            }
          },
          discovererId: {
            required: false,
            serializedName: 'discovererId',
            type: {
              name: 'String'
            }
          }
        }
      }
    };
  }
}

module.exports = ApplicationRegistrationQueryApiModel;
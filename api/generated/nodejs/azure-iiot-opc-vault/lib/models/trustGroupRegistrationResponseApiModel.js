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
 * Trust group registration response model
 *
 */
class TrustGroupRegistrationResponseApiModel {
  /**
   * Create a TrustGroupRegistrationResponseApiModel.
   * @property {string} [id] The id of the trust group
   */
  constructor() {
  }

  /**
   * Defines the metadata of TrustGroupRegistrationResponseApiModel
   *
   * @returns {object} metadata of TrustGroupRegistrationResponseApiModel
   *
   */
  mapper() {
    return {
      required: false,
      serializedName: 'TrustGroupRegistrationResponseApiModel',
      type: {
        name: 'Composite',
        className: 'TrustGroupRegistrationResponseApiModel',
        modelProperties: {
          id: {
            required: false,
            serializedName: 'id',
            type: {
              name: 'String'
            }
          }
        }
      }
    };
  }
}

module.exports = TrustGroupRegistrationResponseApiModel;
/**
 * Cognito Pre Token Generation V2_0 trigger.
 *
 * Injects the user's `custom:tenant_id` attribute into the access token so
 * that API Gateway can forward it via the X-Tenant-Id header.
 *
 * @param {import('aws-lambda').PreTokenGenerationTriggerEvent} event
 * @returns {Promise<import('aws-lambda').PreTokenGenerationTriggerEvent>}
 */
export const handler = async (event) => {
    const userAttributes = event.request?.userAttributes ?? {};
    const tenantId = userAttributes["custom:tenant_id"];

    const claimsToAddOrOverride = tenantId ? { "custom:tenant_id": tenantId } : {};

    event.response = {
        claimsAndScopeOverrideDetails: {
            accessTokenGeneration: {
                claimsToAddOrOverride,
            },
        },
    };

    return event;
};

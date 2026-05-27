export const handler = async (event) => {
    const tenantId = event.request?.userAttributes?.["custom:tenantId"];

    if (tenantId) {
        event.response = {
            claimsAndScopeOverrideDetails: {
                accessTokenGeneration: {
                    claimsToAddOrOverride: {
                        "custom:tenantId": tenantId,
                    },
                },
                idTokenGeneration: {
                    claimsToAddOrOverride: {
                        "custom:tenantId": tenantId,
                    },
                },
            },
        };
    }

    return event;
};

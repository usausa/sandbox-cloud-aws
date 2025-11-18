/**
 * Stops ECS service or Aurora cluster based on the `action` field in the
 * EventBridge Scheduler input JSON.
 *
 * Expected input:
 *   { "action": "stop-ecs" }  – sets ECS desired count to 0
 *   { "action": "stop-db"  }  – calls rds:StopDBCluster
 *
 * Environment variables:
 *   ECS_CLUSTER   – ECS cluster name
 *   ECS_SERVICE   – ECS service name
 *   DB_CLUSTER_ID – Aurora cluster identifier
 */
import { ECSClient, UpdateServiceCommand } from "@aws-sdk/client-ecs";
import { RDSClient, StopDBClusterCommand } from "@aws-sdk/client-rds";

const ecs = new ECSClient({});
const rds = new RDSClient({});

const ECS_CLUSTER   = process.env.ECS_CLUSTER;
const ECS_SERVICE   = process.env.ECS_SERVICE;
const DB_CLUSTER_ID = process.env.DB_CLUSTER_ID;

export const handler = async (event) => {
    const action = event?.action ?? "";

    if (action === "stop-ecs") {
        await ecs.send(new UpdateServiceCommand({
            cluster: ECS_CLUSTER,
            service: ECS_SERVICE,
            desiredCount: 0,
        }));
        return { status: "ok", action };
    }

    if (action === "stop-db") {
        try {
            await rds.send(new StopDBClusterCommand({ DBClusterIdentifier: DB_CLUSTER_ID }));
        } catch (err) {
            if (err.name !== "InvalidDBClusterStateFault") throw err;
            // Already stopped – ignore.
        }
        return { status: "ok", action };
    }

    throw new Error(`Unknown action: ${action}`);
};

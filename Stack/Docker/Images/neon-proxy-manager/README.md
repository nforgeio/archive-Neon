**Do not use: Work in progress**

Dynamically generates HAProxy configurations from routes and certificates persisted to Consul and Vault for NeonCluster proxies based on the [neon-proxy](https://hub.docker.com/r/neoncluster/neon-proxy/) image.

# Supported Tags

* `1.0.0, 1.0, 1, latest`

# Description

NeonClusters deploy two general purpose reverse HTTP/TCP proxy services:

* **neon-proxy-public** which is responsible for routing external network traffic (e.g. from an Internet facing load balancer or router) to cluster services.

* **neon-proxy-private** which is used for internal routing for the scenarios the Docker overlay mesh network doesn't address out-of-the-box (e.g. load balancing and fail-over for groups of stateful containers that cannot be deployed as Docker swarm mode services).

These proxy services are based on the [neon-proxy](https://hub.docker.com/r/neoncluster/neon-proxy/) image which deploys [HAProxy](http://haproxy.org) that actually handles the routing, along with some scripts that can dynamically download the proxy configuration from HashiCorp Consul and TLS certificates from HashiCorp Vault.

The **neon-proxy-manager** image handles the generation and updating of the proxy service configuration in Consul based on proxy definitions and TLS certificates loaded into Consul by the **neon.exe** tool.

# Environment Variables

* **VAULT_CREDENTIALS** (*required*) Names the file within `/run/secrets/` that holds the Vault credentials the proxy manager will need to access TLS certificates.

* **LOG_LEVEL** (*optional*) Specifies the logging level: `FATAL`, `ERROR`, `WARN`, `INFO`, `DEBUG`, or `NONE` (defaults to `INFO`).

# Secrets

**neon-proxy-manager** needs to be able to read the TLS certificates stored in Vault and also be able to read/write Consul NeonCluster service keys for itself as well as **neon-proxy-public** and **neon-proxy-private**.  The credentials are serialized as JSON to the `/run/secrets/${VAULT_CREDENTIALS}` file using the Docker secrets feature.

Two types of credentials are currently supported: **vault-token** and **vault-approle**.

**token:**
&nbsp;&nbsp;&nbsp;&nbsp;`{`
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"type": "vault-token",`
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"vault_token": "65b74ffd-842c-fd43-1386-f7d7006e520a"`
&nbsp;&nbsp;&nbsp;&nbsp;`}`

**approle:**
&nbsp;&nbsp;&nbsp;&nbsp;`{`
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"type": "vault-approle",`
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"vault_role_id": "db02de05-fa39-4855-059b-67221c5c2f63",`
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`"vault_secret_id": "6a174c20-f6de-a53c-74d2-6018fcceff64"`
&nbsp;&nbsp;&nbsp;&nbsp;`}`

This service also requires Consul read/write access to `neon/service/neon-proxy-manager/*`, `neon\service\neon-proxy-public` and `neon\service\neon-proxy-private`.  NeonCluster doesn't currently enforce security on Consul, so there's no authentication necessary for this yet.

# Deployment

**neon-proxy-manager** is typically deployed only to manager nodes.  Multiple instances may be run safely because they will coordinate their activities using a Consul lock, but the best practice is to deploy this as a Docker swarm mode service with one replica constrained to manager nodes with **mode=global**.  This relies on Docker to ensure that only one instance is running.

**neon.exe** deploys **neon-proxy-manager** when the cluster is provisioned using this Docker command:

````
docker service create --name neon-proxy-manager \
    --mount type=bind,src=/etc/neoncluster/env-host,dst=/etc/neoncluster/env-host,readonly=true \
    --mount type=bind,src=/etc/ssl/certs,dst=/etc/ssl/certs,readonly=true \
    --env VAULT_CREDENTIALS=neon-proxy-manager-credentials \
    --env LOG_LEVEL=INFO \
    --secret neon-proxy-manager-credentials \
    --constraint node.role==manager \
    --replicas 1 \
    --log-driver fluentd \
    --log-opt tag=neon-common \
    neoncluster/neon-proxy-manager
````
&nbsp;

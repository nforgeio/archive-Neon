#!/bin/bash
#------------------------------------------------------------------------------
# FILE:         setup-vault-client.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# NOTE: Variables formatted like $<name> will be expanded by [node-conf]
#       using a [PreprocessReader].
#
# Installs the HashiCorp Vault client on a cluster's worker nodes.

# Configure Bash strict mode so that the entire script will fail if 
# any of the commands fail.
#
#       http://redsymbol.net/articles/unofficial-bash-strict-mode/

set -euo pipefail

echo
echo "**********************************************" 1>&2
echo "** SETUP-VAULT-CLIENT                       **" 1>&2
echo "**********************************************" 1>&2

# Load the cluster configuration and setup utilities.

. $<load-cluster-config>
. setup-utility.sh

# Ensure that setup is idempotent.

startsetup setup-vault

echo "*** BEGIN: Install Vault Client" 1>&2

#------------------------------------------------------------------------------
# Download the Vault ZIP file to [/tmp] and then unzip and copy the binary
# to [/usr/local/bin] and make it executable.

echo "***     Downloading Vault" 1>&2

curl -fsSLv ${CURL_RETRY} ${NEON_VAULT_DOWNLOAD} -o /tmp/vault.zip 1>&2
unzip -o /tmp/vault.zip -d /tmp
rm /tmp/vault.zip

mv /tmp/vault /usr/local/bin/vault
chmod 700 /usr/local/bin/vault

#------------------------------------------------------------------------------
# IMPORTANT:
#
# We need to prevent Vault memory from being swapped out to disk to prevent
# secrets from appearing unencrypted in the file system (this is also why
# we're not deploying Vault as a container).

echo "***     Prevent memory swapping" 1>&2

setcap cap_ipc_lock=+ep $(readlink -f /usr/local/bin/vault)

#------------------------------------------------------------------------------
# We need to map the Vault host name to the Docker host IP in local hosts 
# file so the VAULT_ADDR and VAULT_DIRECT_ADDR URLs will resolve.

cat <<EOF >> /etc/hosts

# Map the Hashicorp Vault host name to the Docker node address.

${NEON_NODE_IP} ${NEON_VAULT_HOSTNAME}
EOF

echo "*** END: Install Vault Client" 1>&2

# Indicate that the script has completed.

endsetup setup-vault



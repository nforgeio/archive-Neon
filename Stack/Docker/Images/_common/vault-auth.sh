#------------------------------------------------------------------------------
# FILE:         vault-auth.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# This script logs the container into Vault by reading the Vault credentials
# at [/run/secrets/${VAULT_CREDENTIALS}] and then setting the VAULT_TOKEN environment
# variable to the Vault authentication token.
#
# This script handles [vault-token] and [vault-approle] credentials.
#
# REQUIRES: Vault client binaries as well as the [jq] JSON parser.
#
# USAGE: . vault-auth.sh

TYPE=""

if [ "${VAULT_CREDENTIALS}" == "" ] ; then
    . log-fatal.sh "[VAULT_CREDENTIALS] environment variable is not set."
    exit 1
elif [ ! -f "/run/secrets/${VAULT_CREDENTIALS}" ] ; then
    . log-fatal.sh "[/run/secrets/${VAULT_CREDENTIALS}] file does not exist."
    exit 1
else
    # Extract the credentials type.

    TYPE=$(cat "/run/secrets/${VAULT_CREDENTIALS}" | jq -r '.type')
fi

# Authenticate based on the credentials type.

case ${TYPE} in

vault-token)

    TOKEN=$(cat "/run/secrets/${VAULT_CREDENTIALS}" | jq -r '.vault_token')

    if [ "${TOKEN}" == "" ] ; then
        . log-warn.sh "[/run/secrets/${VAULT_CREDENTIALS}] does not include a token."
        exit 1
    fi

    export VAULT_TOKEN=${TOKEN}
    ;;

vault-approle)

    ROLE_ID=$(cat "/run/secrets/${VAULT_CREDENTIALS}" | jq -r '.vault_role_id')

    if [ "${ROLE_ID}" == "" ] ; then
        . log-fatal.sh "[/run/secrets/${VAULT_CREDENTIALS}] does not include a role ID."
        exit 1
    fi

    SECRET_ID=$(cat "/run/secrets/${VAULT_CREDENTIALS}" | jq -r '.vault_secret_id')

    if [ "${SECRET_ID}" == "" ] ; then
        . log-fatal.sh "[/run/secrets/${VAULT_CREDENTIALS}] does not include a secret ID."
        exit 1
    fi

    export VAULT_TOKEN=$(vault write -format=json auth/approle/login role_id=${ROLE_ID} secret_id=${SECRET_ID} | jq -r '.auth.client_token' )

    if [ "${VAULT_TOKEN}" == "" ] ; then
        . log-fatal.sh "Vault AppRole login failed."
        exit 1
    fi
    ;;

*)
    . log-fatal.sh "Vault credentials type [${TYPE}] is not supported."
    exit 1
    ;;

esac

#!/bin/bash
#------------------------------------------------------------------------------
# FILE:         docker-entrypoint.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

# Load the Docker host node environment variables if present.

if [ -f /etc/neoncluster/env-host ] ; then
    . /etc/neoncluster/env-host
fi

# Load the [/etc/neoncluster/env-container] environment variables if present.

if [ -f /etc/neoncluster/env-container ] ; then
    . /etc/neoncluster/env-container
fi

# Add the root directory to the PATH.

PATH=${PATH}:/

# Load the NeonCluster constants.

. neoncluster.sh

# Initialize the configuration defaults.

if [ "${ELASTICSEARCH_URL}" == "" ] ; then
    export ELASTICSEARCH_URL=http://${NeonHosts_LogEsData}:${NeonHostPorts_ProxyPrivateHttpLogEsData}
fi

if [ "${PERIOD}" == "" ] ; then
    export PERIOD=60s
fi

if [ "${PROCESSES}" == "" ] ; then
    export PROCESSES=[\'dockerd\',\'consul\']
fi

if [ "${LOG_LEVEL}" == "" ] ; then
    export LOG_LEVEL=info
else
    export LOG_LEVEL=$(echo ${LOG_LEVEL} | tr '[:upper:]' '[:lower:]')
fi

# We're either going to run [metricbeat] or import the dashboards.

if [ "${1}" == "service" ] ; then

    # Generate the Metricbeat config file and then start Metricbeat.

    /metricbeat.yml.sh
    /metricbeat -e -system.hostfs=/hostfs

elif [ "${1}" == "import-dashboards" ] ; then
    /scripts/import_dashboards -es ${ELASTICSEARCH_URL}
else 
    echo "Invalid command line: $@"
    exit 1
fi

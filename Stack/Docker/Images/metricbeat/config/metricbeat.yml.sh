#------------------------------------------------------------------------------
# FILE:         metricbeat.yml.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Generates the Metricbeat configuration file by replacing environment variable
# references (I had trouble with embedding these directly into the YML file).
#
# You can find the full configuration reference here:
# https://www.elastic.co/guide/en/beats/metricbeat/index.html

cat <<EOF > /metricbeat.yml
#==========================  Modules configuration ============================
metricbeat.modules:

#------------------------------- System Module -------------------------------
- module: system
  metricsets:
    # CPU stats
    - cpu

    # System Load stats
    - load

    # Per CPU core stats
    #- core

    # IO stats
    - diskio

    # Per filesystem stats
    - filesystem

    # File system summary stats
    - fsstat

    # Memory stats
    - memory

    # Network stats
    - network

    # Per process stats
    - process
  enabled: true
  period: ${PERIOD}
  processes: ${PROCESSES}

#------------------------------- Docker Module -------------------------------
metricbeat.modules:
- module: docker
  metricsets: ["cpu", "info", "memory", "network", "diskio", "container"]
  hosts: ["${NeonClusterConst_DockerApiInternalEndpoint}"]
  enabled: true
  period: ${PERIOD}

#================================ Outputs =====================================

# Configure what outputs to use when sending the data collected by the beat.
# Multiple outputs may be used.

#-------------------------- Elasticsearch output ------------------------------
output.elasticsearch:
  # Array of hosts to connect to.
  hosts: ["${ELASTICSEARCH_URL}"]

  # Optional protocol and basic auth credentials.
  #protocol: "https"
  #username: "elastic"
  #password: "changeme"

#================================ Logging =====================================

# Sets log level. The default log level is info.
# Available log levels are: critical, error, warning, info, debug
logging.level: ${LOG_LEVEL}
EOF

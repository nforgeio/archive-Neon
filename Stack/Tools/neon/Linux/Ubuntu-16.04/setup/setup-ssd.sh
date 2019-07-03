#!/bin/bash

#------------------------------------------------------------------------------
# FILE:         setup-ssd.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# NOTE: This script must be run under [sudo].
#
# NOTE: Variables formatted like $<name> will be expanded by [node-conf]
#       using a [PreprocessReader].

# Configure Bash strict mode so that the entire script will fail if 
# any of the commands fail.
#
#       http://redsymbol.net/articles/unofficial-bash-strict-mode/

set -euo pipefail

echo
echo "**********************************************" 1>&2
echo "** SETUP-SSD                                **" 1>&2
echo "**********************************************" 1>&2

# Load the cluster configuration and setup utilities.

. $<load-cluster-config>
. setup-utility.sh

# Ensure that setup is idempotent.

startsetup setup-ssd

if ${NEON_NODE_SSD} ; then

    echo "*** BEGIN: Tuning for SSD" 1>&2

    # This script works by generating the [/usr/local/tune-ssd.sh] script so that it
    # configures up to 8 [sd?] devices to:
    #
    #       * Use the [deadline] scheduler
    #       * Indicate that the device does not rotate
    #       * Sets the read-ahead value
    #
    # Then the script configures a systemd service unit file that calls the script
    # during system boot.  Here's the official Debian SSD optimization suggestions:
    #
    #   https://wiki.debian.org/SSDOptimization

    read_ahead_size_kb=64

    # Generate [/usr/local/bin/tune-ssd.sh]

    rm -f /usr/local/bin/tune-ssd.sh

    echo "# This script is generated during setup by [setup-ssd.sh] to execute"         > /usr/local/bin/tune-ssd.sh
    echo "# the commands necessary to properly tune any attached SSDs"                 >> /usr/local/bin/tune-ssd.sh

    for DEVICE in sda sdb sdc sde sdf sdg sdh sdi
    do
        if [ -d /sys/block/$DEVICE ]; then
            echo " "                                                                   >> /usr/local/bin/tune-ssd.sh
            echo "# DEVICE: $DEVICE"                                                   >> /usr/local/bin/tune-ssd.sh
            echo "# ---------------"                                                   >> /usr/local/bin/tune-ssd.sh
            echo "echo deadline > /sys/block/$DEVICE/queue/scheduler"                  >> /usr/local/bin/tune-ssd.sh
            echo "echo 0 > /sys/block/$DEVICE/queue/rotational"                        >> /usr/local/bin/tune-ssd.sh
            echo "echo ${read_ahead_size_kb} > /sys/block/$DEVICE/queue/read_ahead_kb" >> /usr/local/bin/tune-ssd.sh
        fi
    done

    chmod 700 /usr/local/bin/tune-ssd.sh

    # Configure and start the [tune-ssd] systemd service.

    cat <<EOF > /lib/systemd/system/tune-ssd.service
[Unit]
Description=SSD Tuning Service
Documentation=
After=sysinit.target
Requires=

[Service]
Type=oneshot
ExecStart=/bin/bash /usr/local/bin/tune-ssd.sh

[Install]
WantedBy=multi-user.target
EOF

    systemctl enable tune-ssd
    systemctl start tune-ssd

    echo "*** END: Tuning for SSD" 1>&2
else
    echo "*** SSD tuning is disabled" 1>&2
fi

# Indicate that the script has completed.

endsetup setup-ssd

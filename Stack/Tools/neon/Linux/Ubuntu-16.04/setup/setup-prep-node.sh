#!/bin/bash
#------------------------------------------------------------------------------
# FILE:         setup-prep-node.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# NOTE: This script must be run under [sudo].
#
# NOTE: Variables formatted like $<name> will be expanded by [node-conf]
#       using a [PreprocessReader].
#
# This script handles the configuration of a near-virgin Ubuntu 14.04 server 
# install into one suitable for deploying a NeonCluster cluster upon.  This
# script requires that:
#
#       * OpenSSH was installed
#       * Host name was left as the default: "ubuntu"
#       * [sudo] be configured to not request passwords

# Configure Bash strict mode so that the entire script will fail if 
# any of the commands fail.
#
#       http://redsymbol.net/articles/unofficial-bash-strict-mode/
#
# NOTE: I'm not using [-u] here to avoid failing for undefined
#       environment variables because some won't be initialized
#       when prepping nodes without a full cluster definition.

set -eo pipefail

echo
echo "**********************************************" 1>&2
echo "** SETUP-PREP-NODE                          **" 1>&2
echo "**********************************************" 1>&2
echo

# $hack(jeff.lill): 
# 
# I'm hacking this path rather than using nice macro substitution because
# this script is the only one that's executed before the formal cluster
# setup process and I don't want to bother the existing code.
#
# This means that I'll have to manually edit this if we ever update
# the path defined by [NodeHostFolder.State].

NEON_STATE_FOLDER=/var/local/neoncluster

if [ -f ${NEON_STATE_FOLDER}/finished-setup-prep-node ] ; then

    echo "This host has already been prepared."
    exit 0
fi

#------------------------------------------------------------------------------
# We need to configure things such that [apt-get] won't complain
# about being unable to initialize Dialog when called from 
# non-interactive SSH sessions.

echo "** Configuring Dialog" 1>&2

echo 'debconf debconf/frontend select Noninteractive' | debconf-set-selections

#------------------------------------------------------------------------------
# We need to modify how [getaddressinfo] handles DNS lookups 
# IPv4 lookups are preferred over IPv6.  This can cause
# performance problems because in most situations right now,
# the server would be doing 2 DNS queries, one for AAAA (IPv6) which
# will nearly always fail (at least until IPv6 is more prevalent)
# and then querying for the for A (IPv4) record.
#
# This can also cause issues when the server is behind a NAT.
# I ran into a situation where [apt-get update] started failing
# because one of the archives had an IPv6 address too.  Here'script
# a note about this issue:
#
#       http://ubuntuforums.org/showthread.php?t=2282646
#
# We're going to uncomment the line below in [gai.conf] and
# change it to the following line to prefer IPv4.
#
#       #precedence ::ffff:0:0/96  10
#       precedence ::ffff:0:0/96  100

sed -i 's!^#precedence ::ffff:0:0/96  10$!precedence ::ffff:0:0/96  100!g' /etc/gai.conf

#------------------------------------------------------------------------------
# Update the Bash profile so the global environment variables will be loaded
# into Bash sessions.

cat <<EOF > /etc/profile.d/env.sh
. /etc/environment
EOF

#------------------------------------------------------------------------------
# [sudo] doesn't allow the subprocess it creates to inherit the environment 
# variables by default.  You need to use the [-E] option to accomplish this.
# 
# As a convienence, we're going to create an [sbash] script, that uses
# [sudo] to start Bash while inheriting the current environment.

cat <<EOF > /usr/bin/sbash
# Starts Bash with elevated permissions while also inheriting
# the current environment variables.

/usr/bin/sudo -E bash \$@
EOF

chmod a+x /usr/bin/sbash

#------------------------------------------------------------------------------
# Install some useful packages.

apt-get install -yq supervisor

#------------------------------------------------------------------------------
# Update the APT package index and install some common packages.

apt-get update -yq
apt-get install -yq unzip curl nano sysstat dstat iotop iptraf apache2-utils daemon

#------------------------------------------------------------------------------
# Ensure that we have the latest packages including any 
# security updates.

echo "** Upgrading packages" 1>&2

apt-get update -yq
apt-get dist-upgrade -yq

#------------------------------------------------------------------------------
# Clean some things up.

echo "** Cleanup" 1>&2

# Clear any cached [apt-get] related files.

apt-get autoclean -yq
rm -rf /var/lib/apt/lists/* 
rm -rf /var/cache/apt/archives/*

# Clear any DHCP leases to be super sure that cloned node
# VMs will obtain fresh IP addresses.

rm -f /var/lib/dhcp/*.leases

#------------------------------------------------------------------------------
# Indicate that the host has been prepared.

touch ${NEON_STATE_FOLDER}/finished-setup-prep-node

echo "**********************************************" 1>&2

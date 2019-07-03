#!/bin/bash
#------------------------------------------------------------------------------
# FILE:         setup-apt-ready.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Waits for the Debian APT package manager to become ready.
#
# Sometimes (especially on Ubuntu 16.04), the APT will be busy mmediately after
# boot, resulting in the possibility of subsequent package commands to fail.  
#
# Usage:    setup-apt-ready.sh

# Wait a few seconds in attempt to ensure that the system won't spin up
# the package manager after our script was started (fragile).

sleep 30

# Wait until the lock file is available.

while ! apt-get check
do
    sleep 5
done

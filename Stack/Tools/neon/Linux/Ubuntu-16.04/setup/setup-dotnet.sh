#!/bin/bash
#------------------------------------------------------------------------------
# FILE:         setup-dotnet.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# NOTE: This script must be run under [sudo].
#
# NOTE: Variables formatted like $<name> will be expanded by [node-conf]
#       using a [PreprocessReader].
#
# This script handles the installation of the .NET Execution Engines (DNX) for
# for .NET Core.
#
#   http://docs.asp.net/en/latest/getting-started/installing-on-linux.html
#

# Configure Bash strict mode so that the entire script will fail if 
# any of the commands fail.
#
#       http://redsymbol.net/articles/unofficial-bash-strict-mode/

set -euo pipefail

echo
echo "**********************************************" 1>&2
echo "** SETUP-DOTNET                             **" 1>&2
echo "**********************************************" 1>&2

# Load the cluster configuration and setup utilities.

. $<load-cluster-config>
. setup-utility.sh

# Ensure that setup is idempotent.

startsetup setup-dotnet

if ${NEON_DOTNET_ENABLED} ; then

    echo "*** BEGIN: Install .NET Core" 1>&2

    # Install .NET Core

    echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ xenial main" > /etc/apt/sources.list.d/dotnetdev.list
    apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893
    apt-get update
    apt-get install -yq ${NEON_DOTNET_VERSION}

    # Build a simple app so that common packages will be downloaded and cached
    # and then delete the app.

    cd ${HOME}
    mkdir temp-app
    cd temp-app
    dotnet new
    dotnet restore
    dotnet build

    cd ..
    rm -r temp-app

    echo "*** END: Install .NET Core" 1>&2
else
    echo "*** .NET Core installation is disabled" 1>&2
fi

endsetup setup-dotnet

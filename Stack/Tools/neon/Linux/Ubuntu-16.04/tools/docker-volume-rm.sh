#!/bin/bash
#------------------------------------------------------------------------------
# FILE:         docker-volume-rm.sh
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Removes a named local Docker volume if it exists.
#
# USAGE:    docker-volume-rm VOLUME-NAME

if docker-volume-exists ${1} ; then
    docker volume rm ${1}
fi

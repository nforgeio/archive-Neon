﻿#------------------------------------------------------------------------------
# FILE:         publish.ps1
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Builds all of the supported Node images and pushes them to Docker Hub.
#
# NOTE: You must be logged into Docker Hub.
#
# Usage: powershell -file ./publish.ps1

param 
(
	[switch]$all = $False
)

#----------------------------------------------------------
# Global Includes
$image_root = "$env:NR_ROOT\\Stack\\Docker\\Images"
. $image_root/includes.ps1
#----------------------------------------------------------

$registry = "neoncluster/node"

# Note that we currently build only one Node image (latest).

./build.ps1
Exec { docker push ${registry}:latest }

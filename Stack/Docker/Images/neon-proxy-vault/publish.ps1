﻿#------------------------------------------------------------------------------
# FILE:         publish.ps1
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Builds the [neon-proxy-vault] image and pushes it to Docker Hub.
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

$registry = "neoncluster/neon-proxy-vault"

# Note that we currently build only one [neon-proxy-vault] image (latest).

./build.ps1
Exec { docker push ${registry}:latest }

#------------------------------------------------------------------------------
# FILE:         build.ps1
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Builds a NeonCluster Node.js base image.
#
# Usage: powershell -file build.ps1

#----------------------------------------------------------
# Global Includes
$image_root = "$env:NR_ROOT\\Stack\\Docker\\Images"
. $image_root/includes.ps1
#----------------------------------------------------------

"   "
"======================================="
"* NODE"
"======================================="

# Build the images.

$registry           = "neoncluster/node";
$dockerTemplatePath = "Dockerfile.template";
$dockerFilePath     = "Dockerfile";

Exec { copy $dockerTemplatePath $dockerFilePath }
Exec { docker build -f $dockerFilePath -t "${registry}:latest" . }
Exec { del $dockerFilePath }

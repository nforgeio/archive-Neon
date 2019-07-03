﻿#------------------------------------------------------------------------------
# FILE:         build.ps1
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Builds a NeonCluster Metricbear image with the specified version, subversion
# and majorversion.  The image built will be a slightly modified version of the 
# Elasticsearch Metricbeat reference image.
#
# Usage: powershell -file build.ps1 VERSION [-latest]

param 
(
	[parameter(Mandatory=$True,Position=1)][string] $version,                # like: "5.0.0"
	[parameter(Mandatory=$False,Position=2)][string] $subversion = "-",      # like: "5.0"
	[parameter(Mandatory=$False,Position=3)][string] $majorversion = "-",    # like: "5"
	[switch]$latest = $False
)

#----------------------------------------------------------
# Global Includes
$image_root = "$env:NR_ROOT\\Stack\\Docker\\Images"
. $image_root/includes.ps1
#----------------------------------------------------------

"   "
"======================================="
"* METRICBEAT " + $version
"======================================="

$registry           = "neoncluster/metricbeat";
$dockerTemplatePath = "Dockerfile.template";
$dockerFilePath     = "Dockerfile";

# Copy the common scripts.

if (Test-Path _common)
{
	Exec { Remove-Item -Recurse _common }
}

Exec { mkdir _common }
Exec { copy ..\_common\*.* .\_common }

# Build the image

Exec { copy $dockerTemplatePath $dockerFilePath }
Exec { text replace-var "-VERSION=$version" "-TINI_VERSION=$tini_version" $dockerFilePath }

Exec { docker build -f $dockerFilePath -t "${registry}:$version" . }

if ($subversion -ne "-")
{
	Exec { docker build -f $dockerFilePath -t "${registry}:$subversion" . }
}

if ($majorversion -ne "-")
{
	Exec { docker build -f $dockerFilePath -t "${registry}:$majorversion" . }
}

if ($latest)
{
	Exec { docker build -f $dockerFilePath -t "${registry}:latest" . }
}

# Cleanup

Exec { del $dockerFilePath }

sleep 5 # Docker sometimes appears to hold references to the files below for a bit.

Exec { Remove-Item -Recurse _common }

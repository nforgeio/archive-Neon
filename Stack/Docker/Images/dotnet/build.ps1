﻿#------------------------------------------------------------------------------
# FILE:         build.ps1
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Builds a [neoncluster/dotnet] image.
#
# Usage: powershell -file build.ps1 VERSION [-latest]

param 
(
	[parameter(Mandatory=$True,Position=1)][string] $version,    # like: "1.1.0-runtime"
	[switch]$latest = $False
)

#----------------------------------------------------------
# Global Includes
$image_root = "$env:NR_ROOT\\Stack\\Docker\\Images"
. $image_root/includes.ps1
#----------------------------------------------------------

"   "
"======================================="
"* DOTNET " + $version
"======================================="

# Build the images.

$registry           = "neoncluster/dotnet";
$dockerTemplatePath = "Dockerfile.template";
$dockerFilePath     = "Dockerfile";

# Build the image.

Exec { copy $dockerTemplatePath $dockerFilePath }
Exec { text replace-var "-VERSION=$version" "-TINI_VERSION=$tini_version" $dockerFilePath }
Exec { docker build -f $dockerFilePath -t "${registry}:$version" . }

if ($latest)
{
	Exec { docker build -f $dockerFilePath -t "${registry}:latest" . }
}

Exec { rmdir $dockerFilePath }


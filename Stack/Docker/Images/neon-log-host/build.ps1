#------------------------------------------------------------------------------
# FILE:         build.ps1
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Builds the base [neon-log-host] image.
#
# Usage: powershell -file build.ps1 VERSION [-latest]

param 
(
	[parameter(Mandatory=$True,Position=1)][string] $version,    # like: "1.2.3"
	[switch]$latest = $False
)

#----------------------------------------------------------
# Global Includes
$image_root = "$env:NR_ROOT\\Stack\\Docker\\Images"
. $image_root/includes.ps1
#----------------------------------------------------------

"   "
"======================================="
"* NEON-LOG-HOST " + $version
"======================================="

# Copy the common scripts.

if (Test-Path _common)
{
	Exec { Remove-Item -Recurse _common }
}

Exec { mkdir _common }
Exec { copy ..\_common\*.* .\_common }

# Build the image.

$registry           = "neoncluster/neon-log-host";
$dockerTemplatePath = "Dockerfile.template";
$dockerFilePath     = "Dockerfile";

Exec { copy $dockerTemplatePath $dockerFilePath }

Exec { docker build -f $dockerFilePath -t "${registry}:${version}" . }

if ($latest)
{
	Exec { docker build -f $dockerFilePath -t "${registry}:latest" . }
}

# Cleanup

Exec { del $dockerFilePath }
Exec { Remove-Item -Recurse _common }

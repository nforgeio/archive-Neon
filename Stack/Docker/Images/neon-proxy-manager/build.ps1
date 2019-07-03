﻿#------------------------------------------------------------------------------
# FILE:         build.ps1
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Builds the NeonCluster [neon-proxy-manager] image.
#
# Usage: powershell -file build.ps1 VERSION [-latest]

param 
(
	[parameter(Mandatory=$True,Position=1)][string] $version,    # like: "1.0.0"
	[switch]$latest = $False
)

#----------------------------------------------------------
# Global Includes
$image_root = "$env:NR_ROOT\\Stack\\Docker\\Images"
. $image_root/includes.ps1
#----------------------------------------------------------

"   "
"======================================="
"* NEON-PROXY-MANAGER " + $version
"======================================="

# Build and publish the [neon-proxy-manager] to a local [bin] folder.

if (Test-Path bin)
{
	Remove-Item bin
}

Exec { mkdir bin }
Exec { dotnet publish "$src_stack_services_path\\neon-proxy-manager\\neon-proxy-manager.csproj" -c Release -o "$pwd\bin" }

# Build the images.

$registry           = "neoncluster/neon-proxy-manager";
$dockerTemplatePath = "Dockerfile.template";
$dockerFilePath     = "Dockerfile";

Exec { copy $dockerTemplatePath $dockerFilePath }
Exec { text replace-var "-VERSION=$version" $dockerFilePath }
Exec { docker build -f $dockerFilePath -t "${registry}:$version" . }

if ($latest)
{
	Exec { docker build -f $dockerFilePath -t "${registry}:latest" . }
}

Exec { del $dockerFilePath }
Exec { rm -r bin }

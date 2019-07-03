#------------------------------------------------------------------------------
# FILE:         includes.ps1
# CONTRIBUTOR:  Jeff Lill
# COPYRIGHT:    Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.
#
# Misc image build related utilities.

#------------------------------------------------------------------------------
# Important source code paths.

$src_path                = $env:NR_ROOT
$src_stack_path          = "$src_path\\Stack"
$src_stack_lib_path      = "$src_path\\Stack\\Lib"
$src_stack_services_path = "$src_path\\Stack\\Services"
$src_stack_tools_path    = "$src_path\\Stack\\Tools"

#------------------------------------------------------------------------------
# Global constants.

$tini_version = "v0.13.2"

#------------------------------------------------------------------------------
# Executes a command, throwing an exception for non-zero error codes.

function Exec
{
    [CmdletBinding()]
    param (
        [Parameter(Position=0, Mandatory=1)]
        [scriptblock]$Command,
        [Parameter(Position=1, Mandatory=0)]
        [string]$ErrorMessage = "*** FAILED: $Command"
    )
    & $Command
    if ($LastExitCode -ne 0) {
        throw "Exec: $ErrorMessage"
    }
}

#------------------------------------------------------------------------------
# Makes any text files that will be included in Docker images Linux safe, by
# converting CRLF line endings to LF and replacing TABs with spaces.

exec { unix-text --recursive $image_root\Dockerfile }
exec { unix-text --recursive $image_root\*.template }
exec { unix-text --recursive $image_root\*.sh }
exec { unix-text --recursive $image_root\*.yml }
exec { unix-text --recursive .\*.cfg }
exec { unix-text --recursive .\*.js }
exec { unix-text --recursive .\*.conf }
exec { unix-text --recursive .\*.md }
exec { unix-text --recursive .\*.json }

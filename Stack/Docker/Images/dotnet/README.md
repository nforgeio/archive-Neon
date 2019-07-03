**Work in progress: Do not use**

NeonCluster based Microsoft .NET Core Linux images.

# Supported Tags

* `1.1.0-runtime, 1.1-runtime, 1-runtime, latest`

# Details

These images are based off of the corresponding versions at [microsoft/dotnet](https://hub.docker.com/r/microsoft/dotnet/).  These images are based on Debian.

Note that **latest** refers to the the latest runtime image and that we're not currently supporting SDK images because the MSBuild support is still very shaky at this time (Nov 2016).

# Additional Packages

This image includes the following packages:

* [tini](https://github.com/krallin/tini) is a simple init manager that can be used to ensure that zombie processes are reaped and that Linux signals are forwarded to sub-processes.

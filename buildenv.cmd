@echo on
REM Configures the environment variables required to build NeonResearch.
REM 
REM 	buildenv [ <source folder> ]
REM
REM Note that <source folder> defaults to the folder holding this
REM batch file.
REM
REM This must be [RUN AS ADMINISTRATOR].

REM Default NR_ROOT to the folder holding this batch file after stripping
REM off the trailing backslash.

set NR_ROOT=%~dp0 
set NR_ROOT=%NR_ROOT:~0,-2%

if not [%1]==[] set NR_ROOT=%1

if exist %NR_ROOT%\Neon.sln goto goodPath
echo The [%NR_ROOT%\Neon.sln] file does not exist.  Please pass the path
echo to the Neon solution folder.
goto done

:goodPath

REM Configure the environment variables.

set NR_TOOLBIN=%NR_ROOT%\ToolBin
set NR_BUILD=%NR_ROOT%\build
set NR_TEMP=C:\Temp
set DOTNETPATH=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set WINSDKPATH=C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools\x64

REM Persist the environment variables.

setx NR_ROOT "%NR_ROOT%" /M
setx NR_TOOLBIN "%NR_TOOLBIN%" /M
setx NR_BUILD "%NR_BUILD%" /M
setx NR_TEMP "%NR_TEMP%" /M

setx DOTNETPATH "%DOTNETPATH%" /M
setx DEV_WORKSTATION 1 /M
setx OPENSSL_CONF "%NR_TOOLBIN%\OpenSSL\openssl.cnf" /M

REM Make sure required folders exist.

if not exist "%NR_TEMP%" mkdir "%NR_TEMP%"
if not exist "%NR_TOOLBIN%" mkdir "%NR_TOOLBIN%"

REM Configure the PATH

%NR_TOOLBIN%\pathtool -dedup -system -add "%NR_BUILD%"
%NR_TOOLBIN%\pathtool -dedup -system -add "%NR_TOOLBIN%"
%NR_TOOLBIN%\pathtool -dedup -system -add "%NR_TOOLBIN%\OpenSSL"
%NR_TOOLBIN%\pathtool -dedup -system -add "%NR_ROOT%\.nuget"
%NR_TOOLBIN%\pathtool -dedup -system -add "%DOTNETPATH%"
%NR_TOOLBIN%\pathtool -dedup -system -add "%WINSDKPATH%"
%NR_TOOLBIN%\pathtool -dedup -system -add "C:\Program Files\7-Zip"
%NR_TOOLBIN%\pathtool -dedup -system -add "C:\Program Files (x86)\PuTTY"
%NR_TOOLBIN%\pathtool -dedup -system -add "C:\Program Files (x86)\WinSCP"

:done
pause


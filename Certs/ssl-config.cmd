@echo off
REM This batch file configures the local SSL certificate IP/port bindings 
REM and ACLs.
REM
REM Note: The certificates must already be loaded into the certificate store.

REM Specify the APPID parameter.  

REM Define the certificate thumb prints (without spaces).  You'll need to 
REM add to or update these when you create or reissue a certificate.

set NEONBOWL_INFO_THUMBPRINT=3614ad7a1dd10150f6f2a2c0ab92e82c102cde58

REM It doesn't seem to matter what the APPID parameter as long as it's a valid GUID.

set APPID={00000000-0000-0000-0000-000000000000}

REM Specify the HTTP ACL user

set ACL_USER=Everyone

REM Specify the certificate store name.

set CERT_STORE=MY

REM ---------------------------------------------------------------------------
REM Ensure that HTTP.SYS is listening on all network interfaces.

REM netsh http add iplisten ipaddress=0.0.0.0

REM ---------------------------------------------------------------------------
REM NEONBOWL.INFO: Configure the port 80 and 443 bindings.

set DOMAIN=neonbowl.info
set HOSTS=www api sts log cops cdb mdb
set CERT_THUMPRINT=%NEONBOWL_INFO_THUMBPRINT%

echo Configuring: %DOMAIN%...
echo ------------------------------------------

REM Configure the base domain

netsh http add sslcert hostnameport=%DOMAIN%:443 certhash=%CERT_THUMPRINT% appid=%APPID% certstorename=%CERT_STORE%
netsh http add urlacl url=https://%DOMAIN%:443/ user=%ACL_USER%
netsh http add urlacl url=http://%DOMAIN%:80/ user=%ACL_USER%

REM Configure the domain hosts

for %%H in (%HOSTS%) do netsh http add sslcert hostnameport=%%H.%DOMAIN%:443 certhash=%CERT_THUMPRINT% appid=%APPID% certstorename=%CERT_STORE%
for %%H in (%HOSTS%) do netsh http add urlacl url=https://%%H.%DOMAIN%:443/ user=%ACL_USER%
for %%H in (%HOSTS%) do netsh http add urlacl url=http://%%H.%DOMAIN%:80/ user=%ACL_USER%

echo ------------------------------------------
echo.
echo ** Operation Complete **
echo.

pause

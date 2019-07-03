@echo off
REM This batch file removes the local SSL certificate IP/port bindings and ACLs.

echo.

REM ---------------------------------------------------------------------------
REM NEONBOWL.INFO: Unconfigure port 80 and 443 bindings.

set DOMAIN=neonbowl.info
set HOSTS=www api sts log cops cdb mdb

echo Unconfiguring: %HOST%...
echo ------------------------------------------

REM Unconfigure the base domain

netsh http delete sslcert hostnameport=%DOMAIN%:443
netsh http delete urlacl url=https://%DOMAIN%:443/
netsh http delete urlacl url=http://%DOMAIN%:80/

REM Unconfigure the domain hosts

for %%H in (%HOSTS%) do netsh http delete sslcert hostnameport=%%H.%DOMAIN%:443
for %%H in (%HOSTS%) do netsh http delete urlacl url=https://%%H.%DOMAIN%:443/
for %%H in (%HOSTS%) do netsh http delete urlacl url=http://%%H.%DOMAIN%:80/

echo ------------------------------------------
echo.
echo ** Operation Complete **
echo.

pause

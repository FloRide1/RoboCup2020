@echo off
echo Choose:
echo [A] Set Static IP
echo [B] Set DHCP
echo.
:choice
SET /P C=[A,B]?
for %%? in (A) do if /I "%C%"=="%%?" goto A
for %%? in (B) do if /I "%C%"=="%%?" goto B
goto choice
:A
@echo off
echo "Please enter Static IP Address Information"
echo "Static IP Address:"
set /p IP_Addr=

echo "Default Gateway:"
set /p D_Gate=

echo "Subnet Mask:"
set /p Sub_Mask=

echo "Setting Static IP Information"
netsh interface ip set address "LAN" static %IP_Addr% %Sub_Mask% %D_Gate% 1
netsh int ip show config
pause
goto end

:B
@ECHO OFF
ECHO Resetting IP Address and Subnet Mask For DHCP
netsh int ip set address name = "LAN" source = dhcp

ipconfig /renew

ECHO Here are the new settings for %computername%:
netsh int ip show config

pause
goto end
:end

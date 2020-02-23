@ECHO OFF
ECHO Resetting IP Address and Subnet Mask For DHCP
netsh interface set interface "Wi-Fi" admin=enable
netsh int ip set address name = "Wi-Fi" source = dhcp
netsh interface set interface "Ethernet" admin=disable
ipconfig /renew
ECHO Here are the new settings for %computername%:
netsh int ip show config

pause
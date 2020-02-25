@ECHO OFF
ECHO Resetting IP Address and Subnet Mask For DHCP
netsh interface set interface "Wi-Fi" admin=disable
netsh interface set interface "Ethernet" admin=enable
netsh int ip set address name = "Ethernet" source = dhcp
netsh interface ip set dnsservers name="Ethernet" source=dhcp
ipconfig /release
ipconfig /renew
ipconfig /registerdns
ECHO Here are the new settings for %computername%:
netsh int ip show config

pause
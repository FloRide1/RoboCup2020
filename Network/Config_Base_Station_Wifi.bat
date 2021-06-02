echo "Setting Static IP Information"
netsh interface set interface "Ethernet" admin=disable
netsh interface set interface "Wi-Fi" admin=enable
netsh interface ip set address "Wi-Fi" static 172.16.79.1 255.255.0.0 172.16.1.1 1
netsh interface ip set dns "Wi-Fi" static 172.16.1.1
netsh int ip show config
pause
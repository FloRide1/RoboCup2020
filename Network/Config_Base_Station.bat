echo "Setting Static IP Information"
netsh interface set interface "Wi-Fi" admin=disable
netsh interface set interface "Ethernet" admin=enable
netsh interface ip set address "Ethernet" static 172.16.79.1 255.255.0.0 172.16.0.1 1
netsh int ip show config
pause
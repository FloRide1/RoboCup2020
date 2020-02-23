echo "Setting Network Configuration for Robot 1"
netsh interface set interface "Wi-Fi" admin=enable
netsh interface set interface "Ethernet" admin=disable
netsh interface ip set address "Wi-Fi" static 172.16.79.101 255.255.255.0 172.16.0.1 1
netsh int ip show config
pause
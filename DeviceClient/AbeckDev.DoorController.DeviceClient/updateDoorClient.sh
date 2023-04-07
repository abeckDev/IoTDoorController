#!/bin/bash
# This script is used to update the DoorController device client on a Raspberry Pi with Raspbian OS

#Check for root privileges
if [ "$EUID" -ne 0 ]
  then echo "Please run as root"
  exit
fi 


# Updating Device Client
echo "Updating Device Client..."
cd /opt/DoorClient

# Save current appsettings.json
cp /opt/DoorClient/appsettings.json $HOME/appsettings.json_backup

#stop service
systemctl stop DoorDeviceClient.service

echo "Removing existing Version in /opt/DoorClient"
rm -rf /opt/DoorClient/*

echo "Downloading latest release"
mkdir $HOME/DoorController-Update
cd $HOME/DoorController-Update
wget https://github.com/abeckDev/IoTDoorController/releases/download/latest/DoorController-Release.zip

echo "Extracting Application"
unzip DoorController-Release.zip -d DoorController-Release/

# Move files to runtime path 
mv -f DoorController-Release/* /opt/DoorClient/
rm -f /opt/DoorClient/appsettings.json
cp $HOME/appsettings.json_backup /opt/DoorClient/appsettings.json

#Cleanup
cd /opt/DoorClient
rm -rf $HOME/DoorController-Update

#Start service
systemctl restart DoorDeviceClient.service

# Finished
echo "Finished update. The appsettings were preserved and will still work."
echo "The service has been restarted already."
echo "You can check the status with systemctl status DoorDeviceClient.service"

exit 0
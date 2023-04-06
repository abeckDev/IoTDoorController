#!/bin/bash
# This script is used to setup the DoorController device client on a Raspberry Pi with Raspbian OS

#Check for root privileges
if [ "$EUID" -ne 0 ]
  then echo "Please run as root"
  exit
fi 

# Install pending updates
echo "Installing pending updates..."
apt-get update 
apt-get upgrade -y

# Install git
echo "Installing Git..."
apt-get install git vim screen -y

# Install .NET6 runtime
echo "Installing .NET6 runtime..."
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel LTS
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc

# Installing 433Util Dependencies
echo "Installing 433Util dependencies..."
cd /opt/
git clone https://github.com/WiringPi/WiringPi.git
git clone --recursive https://github.com/ninjablocks/433Utils.git
cd WiringPi
./build
cd /opt/433Utils/RPi_utils
make all

# Installing helper scripts 
echo "Installing helper scripts..."
cd /opt/
mkdir doorControllerHelper
cd doorControllerHelper
wget https://raw.githubusercontent.com/abeckDev/IoTDoorController/master/DeviceClient/AbeckDev.DoorController.DeviceClient/senddecimalcode.sh
wget https://raw.githubusercontent.com/abeckDev/IoTDoorController/master/DeviceClient/AbeckDev.DoorController.DeviceClient/updateDoorClient.sh
wget https://raw.githubusercontent.com/abeckDev/IoTDoorController/master/DeviceClient/AbeckDev.DoorController.DeviceClient/DoorDeviceClient.service
wget https://raw.githubusercontent.com/abeckDev/IoTDoorController/master/DeviceClient/AbeckDev.DoorController.DeviceClient/checkDeviceclient.sh
chmod +x senddecimalcode.sh updateDoorClient.sh checkDeviceclient.sh

# Installing Device Client
echo "Installing Device Client..."
cd /opt/
mkdir DoorClient
cd DoorClient
echo "Removing existing Version in /opt/DoorClient"
rm -rf /opt/DoorClient/*

echo "Downloading latest release"
cd $HOME
wget https://github.com/abeckDev/IoTDoorController/releases/download/latest/DoorController-Release.zip

echo "Extracting Application"
unzip DoorController-Release.zip -d DoorController-Release/

# Move files to runtime path 
mv -f DoorController-Release/* /opt/DoorClient/

#Cleanup
rm -rf DoorController-Release/
rm DoorController-Release.zip

# Setup service
echo "Setting up service..."
cp /opt/doorControllerHelper/DoorDeviceClient.service /etc/systemd/system/
systemctl enable DoorDeviceClient.service

#Setup cronjobs
echo "Setting up cronjobs..."
(crontab -l ; echo "*/5 * * * * /opt/doorControllerHelper/checkDeviceclient.sh") | crontab -

# Finished
echo "Finished setup. Please now configure the appsettings.json file in /opt/DoorClient/"
echo "After that you can start the service with systemctl start DoorDeviceClient.service"
echo "You can check the status with systemctl status DoorDeviceClient.service"

exit 0
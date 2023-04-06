#!/bin/bash
# This script is used to update the Door Controller Device Client it needs the Zipped output of the publish folder

# Vars
runtimePath="/opt/DoorClient/"

echo "Installing Door Controller Device Client"
echo "Updating from Package $1"

echo "Removing existing Version in $runtimePath"
rm -rf /opt/DoorClient/*

echo "Extracting Applicatio"
unzip $1
mv -f publish/* $runtimePath/
rm /opt/DoorClient/appsettings.json
cp /home/pi/appsettings.json /opt/DoorClient/

echo "Cleanup"

rm -rf publish/

echo "Done"
#!/bin/bash
# Install as Cron Job to check if Deviceclient died 

# Check if screen session is running
screen -ls deviceclientsession > /dev/null
isStillRunning=$?

if [ $isStillRunning != 0 ]
then
  systemctl restart DoorDeviceClient
fi

exit 0


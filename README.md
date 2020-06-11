# IoTDoorController

A collection of software used to utilize IoT devices to control 433MHz based door control systems. 

## Software in this Repository

* [Device Client](DeviceClient/AbeckDev.DoorController.DeviceClient/AbeckDev.DoorController.DeviceClient/)
  * The Software running on the Edge device which will send the 433 MHz signals to your doors.
* [IoT Central Trigger Function](IoTCentralTriggerFunction/IoTCentralTriggerFunctions/IoTCentralTriggerFunctions/IoTCentralTriggerFunctions/)
  * The Function used to receive signals from the end devices and forwarding them securely to Azure IoT Central


## Included Components 

* Microsoft Azure - As main Cloud Environment
  * Azure Functions - Main Entrypoint for REST Clients and Connector to Azure IoT Central
  * Azure Key Vault - Secret Management for Cloud Environment
  * Azure IoT Central - IoT AllIn One suite for Communication with the IoT End Devices
* RaspberryPi - As Test Hardware
  * .NET Core 3.1 as Client App Framework
  
**Documentation in Detail will (hopefully) follow soon!**

  
  

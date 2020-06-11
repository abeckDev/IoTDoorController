# IoTDoorController

A collection of software used to utilize IoT devices to control 433MHz based door control systems. 

## Getting Started
ToDo: Add a Intoduction to project and explaint setup process. 

### Software in this Repository

* [Device Client](DeviceClient/AbeckDev.DoorController.DeviceClient/AbeckDev.DoorController.DeviceClient/)
  * The Software running on the Edge device which will send the 433 MHz signals to your doors.
* [IoT Central Trigger Function](IoTCentralTriggerFunction/IoTCentralTriggerFunctions/IoTCentralTriggerFunctions/IoTCentralTriggerFunctions/)
  * The Function used to receive signals from the end devices and forwarding them securely to Azure IoT Central


### Included Components 

* Microsoft Azure - As main Cloud Environment
  * Azure Functions - Main Entrypoint for REST Clients and Connector to Azure IoT Central
  * Azure Key Vault - Secret Management for Cloud Environment
  * Azure IoT Central - IoT AllIn One suite for Communication with the IoT End Devices
* RaspberryPi - As Test Hardware
  * .NET Core 3.1 as Client App Framework
  
**Documentation in Detail will (hopefully) follow soon!**


### Prerequisites

ToDo: Add what things you need to install the software and how to install them

### Installing


## Running the tests


## Deployment

ToDo: Add deployment details

## Built With

* [Azure Functions](https://azure.microsoft.com/en-us/services/functions/) - Serverless Framework which receive signals from the end devices and forwarding them securely to Azure IoT Central
* [Azure IoT Central](https://azure.microsoft.com/en-us/services/iot-central/) - Central IoT App Plattform
* [.NET Core](https://dotnet.microsoft.com/learn/dotnet/what-is-dotnet) - The Framework used to build the Device Client software

## Contributing

Please read [Contributing](CONTRIBUTING.md) for the process for submitting pull requests to us and refer to [Code of Conduct](CODE_OF_CONDUCT.md) for details on our code of conduct. 

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/abeckDev/IoTDoorController/releases). 

## Authors

* **Alexander Beck** - *Initial work* - [abeckdev](https://github.com/abeckDev)

See also the list of [contributors](https://github.com/abeckDev/IoTDoorController/graphs/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## Acknowledgments

* Thanks to the Microsoft Learn Plattform Module [Create your first iot central app](https://docs.microsoft.com/en-us/learn/modules/create-your-first-iot-central-app/) for a great introduction to IoT Central and a sample Application code. 


  
  

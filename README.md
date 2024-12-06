# IoTDoorController

A collection of software used to utilize IoT devices to control 433MHz based door control systems. 

## Getting Started
ToDo: Add a Intoduction to project and explaint setup process. 

### Software in this Repository

* [Device Client](DeviceClient/AbeckDev.DoorController.DeviceClient/AbeckDev.DoorController.DeviceClient/)
  * The Software running on the Edge device which will send the 433 MHz signals to your doors.
* [IoT Central Trigger Function](IoTCentralTriggerFunction/)
  * The Function used to receive signals from the end devices and forwarding them securely to Azure IoT Central


### Included Components 

* Microsoft Azure - As main Cloud Environment
  * Azure Functions - Main Entrypoint for REST Clients and Connector to Azure IoT Central
    * Azure managed Identities - To connect Azure Functions and Azure IoT Central
  * Azure IoT Central - IoT AllIn One suite for Communication with the IoT End Devices
* RaspberryPi - As Test Hardware
  * .NET 8 as the current LTS version as Client App Framework
  


### Prerequisites

You need the following prerequisites to operate the doorcontroller:
* A Raspberry Pi system which can run the latest Version of [Raspberry Pi OS 11](https://www.raspberrypi.com/software/operating-systems/)
* An Azure Subscription to host the Azure Functions in an Azure Function App


### Installing

#### Azure Functions 

The Azure Function can be deployed with the Azure Function CLI. The Setup of a managed Identity with access to the Azure IoT Central app is also neccesary. 

#### Device Client

An installer is available to setup the device client, the Linux service and the cronjob. You can execute the installer with the following command in the Raspbian OS system as ```root```:

```bash
curl https://raw.githubusercontent.com/abeckDev/IoTDoorController/master/DeviceClient/AbeckDev.DoorController.DeviceClient/setupDoorController.sh | bash -
```

### Updateting 

#### Device Client

The Device Client can be updated with a specific updater file. The usage is similiar to the installation process. The script also need to be run as ```root```:

```bash
curl https://raw.githubusercontent.com/abeckDev/IoTDoorController/master/DeviceClient/AbeckDev.DoorController.DeviceClient/updateDoorClient.sh | bash -
```

## Built With

* [Azure Functions](https://azure.microsoft.com/en-us/services/functions/) - Serverless Framework which receive signals from the end devices and forwarding them securely to Azure IoT Central
* [Azure IoT Central](https://azure.microsoft.com/en-us/services/iot-central/) - Central IoT App Plattform
* [.NET 8](https://dotnet.microsoft.com/learn/dotnet/what-is-dotnet) - The Framework used to build the Device Client software

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


  
  

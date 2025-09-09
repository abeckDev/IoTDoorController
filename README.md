# IoT Door Controller

A comprehensive .NET-based solution for controlling 433MHz door systems through Azure IoT Central. This project enables remote door control via IoT devices running on Raspberry Pi, integrated with Azure cloud services for secure and scalable IoT management.

## 🏗️ Architecture Overview

The solution consists of two main components:

- **Device Client** (.NET 8): Runs on Raspberry Pi edge devices to control 433MHz door systems
- **Azure Function** (.NET 6): HTTP trigger function that interfaces with Azure IoT Central for secure cloud integration

## 🚀 Features

- **Remote Door Control**: Control multiple 433MHz door systems remotely
- **Azure IoT Central Integration**: Secure device management and telemetry
- **Automated CI/CD**: GitHub Actions for building, testing, and releasing
- **Docker Support**: Containerized deployment options
- **Raspberry Pi Optimized**: Native support for Raspberry Pi hardware
- **Automatic Updates**: Built-in update mechanisms for edge devices
- **Service Management**: Systemd service integration with health monitoring

## 📋 Prerequisites

### Hardware Requirements
- Raspberry Pi (3B+ or newer) running Raspberry Pi OS 11 or later
- 433MHz transmitter module (connected to GPIO pins)
- Compatible 433MHz door/gate systems

### Software Requirements
- Azure Subscription with IoT Central application
- .NET 8 SDK (for development) or .NET 8 Runtime (for deployment)
- Docker (optional, for containerized deployment)

### Azure Services
- Azure IoT Central application
- Azure Function App
- Azure Managed Identity (for secure IoT Central access)

## 🔧 Installation

### Quick Installation (Raspberry Pi)

For a complete automated setup on Raspberry Pi, run as **root**:

```bash
curl https://raw.githubusercontent.com/abeckDev/IoTDoorController/main/DeviceClient/AbeckDev.DoorController.DeviceClient/setupDoorController.sh | bash -
```

> **Note**: If you encounter issues with the `main` branch, the setup script may still reference the legacy `master` branch internally. The installation will work regardless of this branch reference.

This script will:
- Install .NET 8 runtime
- Install 433Utils and WiringPi dependencies  
- Download and configure the Device Client
- Set up systemd service and monitoring
- Configure automatic health checks

### Manual Installation

#### 1. Device Client Setup

Clone the repository and build the Device Client:

```bash
cd DeviceClient/AbeckDev.DoorController.DeviceClient/
dotnet restore AbeckDev.DoorController.DeviceClient.sln
dotnet build AbeckDev.DoorController.DeviceClient.sln --configuration Release
dotnet publish -c Release -o /opt/DoorClient
```

Configure the application by editing `/opt/DoorClient/appsettings.json`:

```json
{
  "IotCentralGlobalDeviceEndpoint": "global.azure-devices-provisioning.net",
  "IotCentralScopeId": "YOUR_SCOPE_ID",
  "IotCentralDeviceId": "YOUR_DEVICE_ID", 
  "IotCentralPrimaryKey": "YOUR_PRIMARY_KEY",
  "DeviceLocation": "Your Location",
  "RemoteDoors": [
    {
      "Name": "Main Gate",
      "ID": 1,
      "Decimalcode": "5510420"
    }
  ]
}
```

#### 2. Azure Function Setup

Deploy the Azure Function:

```bash
cd IoTCentralTriggerFunction/
dotnet restore IoTCentralTriggerFunctions.sln
dotnet build IoTCentralTriggerFunctions.sln --configuration Release
func azure functionapp publish YOUR_FUNCTION_APP_NAME
```

Configure the Function App with required settings:
- Set up Managed Identity
- Configure IoT Central access permissions
- Set environment variables for announcement triggers (optional)

### Docker Deployment

Build and run the Device Client using Docker:

```bash
cd DeviceClient/AbeckDev.DoorController.DeviceClient/AbeckDev.DoorController.DeviceClient/
docker build . --file Dockerfile --tag iotdoordeviceclient
docker run -d --name doorcontroller \
  --device /dev/gpiomem \
  -v /path/to/your/appsettings.json:/app/appsettings.json \
  iotdoordeviceclient
```

## 🔄 Updates

### Device Client Updates

Update your Device Client installation while preserving configuration:

```bash
curl https://raw.githubusercontent.com/abeckDev/IoTDoorController/main/DeviceClient/AbeckDev.DoorController.DeviceClient/updateDoorClient.sh | bash -
```

The update script will:
- Stop the current service
- Backup existing configuration
- Download and install the latest version
- Restore configuration
- Restart the service

### Checking Status

Monitor your Device Client:

```bash
# Check service status
systemctl status DoorDeviceClient.service

# View real-time logs
journalctl -u DoorDeviceClient.service -f

# Check if process is running
ps aux | grep AbeckDev.DoorController.DeviceClient
```

## 🔨 Development & Building

### Building Device Client

```bash
cd DeviceClient/AbeckDev.DoorController.DeviceClient/
dotnet restore AbeckDev.DoorController.DeviceClient.sln
dotnet build AbeckDev.DoorController.DeviceClient.sln --configuration Release --no-restore
```

### Building Azure Function

```bash
cd IoTCentralTriggerFunction/
dotnet restore IoTCentralTriggerFunctions.sln  
dotnet build IoTCentralTriggerFunctions.sln --configuration Release --no-restore
```

### Local Development

Run the Device Client locally:

```bash
cd DeviceClient/AbeckDev.DoorController.DeviceClient/AbeckDev.DoorController.DeviceClient/
dotnet run
```

Run the Azure Function locally:

```bash
cd IoTCentralTriggerFunction/
func start
```

## 🚀 CI/CD Pipeline

The project uses GitHub Actions for automated building and deployment:

### Workflows

1. **Device Client Build** (`.github/workflows/dotnet-build.yml`)
   - Triggers on changes to Device Client code
   - Builds and tests the .NET 8 application
   - Creates release artifacts with automatic versioning
   - Publishes GitHub releases

2. **Docker Build** (`.github/workflows/docker-publish.yml`)
   - Builds Docker images for the Device Client
   - Publishes to GitHub Container Registry
   - Tags images with version numbers

### Automatic Releases

- Releases are automatically created when code is pushed to `main` branch
- Version numbers are extracted from the project file
- Release artifacts include compiled binaries and installation scripts
- Docker images are tagged with both `latest` and version-specific tags

## ⚙️ Configuration

### Device Client Configuration

The `appsettings.json` file supports the following configuration options:

```json
{
  "IotCentralGlobalDeviceEndpoint": "global.azure-devices-provisioning.net",
  "IotCentralScopeId": "Your IoT Central Scope ID",
  "IotCentralDeviceId": "Unique device identifier", 
  "IotCentralPrimaryKey": "Device primary key",
  "DeviceLocation": "Physical location description",
  "RemoteDoors": [
    {
      "Name": "Door display name",
      "ID": 1,
      "Decimalcode": "433MHz decimal code",
      "SystemCode": "Alternative system code (optional)",
      "DeviceCode": "Alternative device code (optional)"
    }
  ]
}
```

### Azure Function Configuration

Required application settings:
- `AzureWebJobsStorage`: Storage account connection string
- `FUNCTIONS_WORKER_RUNTIME`: Set to `dotnet`
- `Announcement_TriggerUrl`: Optional URL for notifications

## 🔧 Troubleshooting

### Common Issues

**Device Client won't start:**
- Check IoT Central credentials in `appsettings.json`
- Verify .NET 8 runtime is installed: `dotnet --version`
- Check service logs: `journalctl -u DoorDeviceClient.service -n 50`

**433MHz signals not working:**
- Verify WiringPi installation: `gpio readall`
- Check 433Utils installation: `ls /opt/433Utils/RPi_utils/`
- Ensure GPIO permissions for the application user

**Azure Function deployment fails:**
- Verify Azure CLI authentication: `az account show`
- Check Function App configuration and managed identity setup
- Ensure IoT Central access permissions are properly configured

**Docker build issues:**
- Ensure Docker daemon is running
- Check network connectivity for NuGet package restore
- Verify Dockerfile syntax and base image availability

### Log Locations

- **Device Client**: `journalctl -u DoorDeviceClient.service`
- **Azure Function**: Azure portal Function App logs
- **Installation logs**: Terminal output during script execution

## 📚 Project Structure

```
IoTDoorController/
├── DeviceClient/                           # Device Client (.NET 8)
│   └── AbeckDev.DoorController.DeviceClient/
│       ├── AbeckDev.DoorController.DeviceClient/  # Main application
│       ├── setupDoorController.sh          # Installation script
│       ├── updateDoorClient.sh             # Update script
│       └── DoorDeviceClient.service        # Systemd service file
├── IoTCentralTriggerFunction/             # Azure Function (.NET 6)
│   ├── DoorActionFunction.cs               # HTTP trigger function
│   └── IoTCentralTriggerFunctions.csproj   # Project file
├── .github/workflows/                      # CI/CD pipelines
│   ├── dotnet-build.yml                   # Build and release workflow
│   └── docker-publish.yml                 # Docker build workflow
└── README.md                              # This file
```

## 🛠️ Built With

### Core Technologies
- **[.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)** - Latest LTS framework for Device Client application
- **[.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)** - Framework for Azure Functions (Azure Functions v4)
- **[Azure IoT Device SDK](https://github.com/Azure/azure-iot-sdk-csharp)** - Official Azure IoT device connectivity
- **[Azure Functions](https://azure.microsoft.com/en-us/products/functions/)** - Serverless compute for IoT Central integration

### Azure Services
- **[Azure IoT Central](https://azure.microsoft.com/en-us/products/iot-central/)** - Comprehensive IoT application platform
- **[Azure Managed Identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)** - Secure authentication without credentials
- **[Azure Container Registry](https://azure.microsoft.com/en-us/products/container-registry/)** - Docker image hosting

### Hardware & System Integration
- **[WiringPi](http://wiringpi.com/)** - GPIO interface library for Raspberry Pi
- **[433Utils](https://github.com/ninjablocks/433Utils)** - 433MHz RF transmission utilities
- **Docker** - Containerization platform for flexible deployment
- **systemd** - Linux service management

### Development & CI/CD
- **GitHub Actions** - Automated build, test, and deployment pipelines
- **Docker** - Multi-stage builds and container deployment
- **SemVer** - Semantic versioning for release management

## 🤝 Contributing

We welcome contributions to the IoT Door Controller project! Please read our [Contributing Guidelines](CONTRIBUTING.md) for details on:

- Code of conduct
- Pull request process  
- Development workflow
- Testing requirements

### Development Setup

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes and test thoroughly
4. Update documentation as needed
5. Submit a pull request

### Reporting Issues

Please use GitHub Issues to report bugs or request features. Include:
- Detailed description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details (OS, .NET version, etc.)

## 📖 Versioning

This project follows [Semantic Versioning (SemVer)](http://semver.org/) principles:

- **MAJOR** version for incompatible API changes
- **MINOR** version for backward-compatible functionality additions  
- **PATCH** version for backward-compatible bug fixes

### Current Versions
- **Device Client**: v1.0.11
- **Azure Function**: Compatible with Azure Functions v4

For all available versions, see the [releases page](https://github.com/abeckDev/IoTDoorController/releases).

## 👥 Authors & Contributors

* **Alexander Beck** ([@abeckDev](https://github.com/abeckDev)) - *Project creator and maintainer*

See the complete list of [contributors](https://github.com/abeckDev/IoTDoorController/graphs/contributors) who have participated in this project.

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

The MIT License allows you to:
- ✅ Use commercially
- ✅ Modify
- ✅ Distribute  
- ✅ Private use

## 🙏 Acknowledgments

- **Microsoft Learn** - [Create your first IoT Central app](https://docs.microsoft.com/en-us/learn/modules/create-your-first-iot-central-app/) tutorial provided foundational IoT Central integration patterns
- **Azure IoT Team** - Excellent documentation and SDK support
- **Raspberry Pi Foundation** - Outstanding hardware platform and community
- **Open Source Community** - WiringPi, 433Utils, and other essential libraries

## 📞 Support

For support and questions:

1. **Check the documentation** - This README and inline code comments
2. **Search existing issues** - Someone may have encountered the same problem
3. **Create a new issue** - Use our issue templates for bug reports or feature requests
4. **Join discussions** - Participate in GitHub Discussions for general questions

---

**Happy IoT building! 🚪🔧** 

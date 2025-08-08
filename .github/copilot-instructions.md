# IoT Door Controller
The IoT Door Controller is a .NET-based system for controlling 433MHz door systems via IoT devices. It consists of a Device Client (.NET 8) that runs on Raspberry Pi and an Azure Function (.NET 6) that interfaces with Azure IoT Central.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Setup
- .NET 8 SDK is required for Device Client development and building
- .NET 6 SDK is required for Azure Function development (but .NET 8 can build .NET 6 projects)
- Docker is available for containerized deployment of Device Client
- Azure CLI and Functions CLI are needed for Azure Function deployment

### Bootstrap, Build, and Test the Repository
**Device Client (.NET 8):**
- Navigate to: `cd DeviceClient/AbeckDev.DoorController.DeviceClient/`
- `dotnet restore AbeckDev.DoorController.DeviceClient.sln` -- takes 20-25 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
- `dotnet build AbeckDev.DoorController.DeviceClient.sln --configuration Release --no-restore` -- takes 1-2 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
- Or navigate to project: `cd AbeckDev.DoorController.DeviceClient/` and use:
  - `dotnet build --configuration Release` -- takes 10 seconds. Set timeout to 30+ seconds.
  - `dotnet publish -c Release -o publish/` -- takes 1-2 seconds. Set timeout to 30+ seconds.

**Azure Function (.NET 6):**
- Navigate to: `cd IoTCentralTriggerFunction/`
- `dotnet restore IoTCentralTriggerFunctions.sln` -- takes 40-45 seconds. NEVER CANCEL. Set timeout to 120+ seconds.
- `dotnet build IoTCentralTriggerFunctions.sln --configuration Release --no-restore` -- takes 2-3 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
- `dotnet publish -c Release -o publish/` -- takes 4-5 seconds. Set timeout to 30+ seconds.

**Docker Build (Device Client):**
- Navigate to: `cd DeviceClient/AbeckDev.DoorController.DeviceClient/AbeckDev.DoorController.DeviceClient/`
- `docker build . --file Dockerfile --tag iotdoordeviceclient` -- takes 5-10 minutes. NEVER CANCEL. Set timeout to 15+ minutes.

### No Tests Available
- This repository contains no unit tests or test frameworks
- Manual validation must be performed by running the applications and verifying functionality
- Do not attempt to run test commands as they will fail

### Run the Applications
**Device Client:**
- After building and publishing, navigate to the publish directory
- `dotnet AbeckDev.DoorController.DeviceClient.dll` -- will start the application
- Application requires proper Azure IoT Central configuration in appsettings.json
- Without valid configuration, it will fail to connect but will show door registrations loading

**Azure Function (Local Development):**
- Requires Azure Functions Core Tools (`func start`)
- Navigate to: `cd IoTCentralTriggerFunction/`
- `func start` -- requires proper local.settings.json configuration
- Function will start HTTP endpoint for door control requests

## Validation

### Manual Validation Requirements
**CRITICAL**: After making any changes, ALWAYS perform these validation steps:

1. **Build Validation:**
   - Both projects must build without errors
   - Device Client: Run `dotnet build` in Device Client directory
   - Azure Function: Run `dotnet build` in IoTCentralTriggerFunction directory

2. **Application Startup Validation:**
   - Device Client: Navigate to `DeviceClient/AbeckDev.DoorController.DeviceClient/AbeckDev.DoorController.DeviceClient/bin/Release/net8.0/`
   - Run: `dotnet AbeckDev.DoorController.DeviceClient.dll` and verify it shows:
     - "Starting DoorController Device Client" message
     - Version information (e.g., "1.0.8")
     - "Reading Configuration" message
     - Door registration messages for configured doors (e.g., "Found Registration for Door: Hoftor")
   - Azure Function: Start locally and verify HTTP endpoint responds (requires Azure Functions CLI)

3. **Configuration Validation:**
   - Device Client appsettings.json must contain valid IoT Central connection details
   - Azure Function local.settings.json must contain required Azure settings
   - Both configurations contain example/placeholder values by default

4. **Docker Validation (if modified):**
   - Docker build must complete successfully
   - Container must start and show same startup messages as direct execution

### CI/CD Pipeline Validation
- Always ensure changes pass GitHub Actions workflows:
  - `.github/workflows/dotnet-build.yml` -- builds Device Client
  - `.github/workflows/docker-publish.yml` -- builds and publishes Docker image
- These workflows automatically run on push/PR to main branch

## Common Tasks

### Key Project Structures
**Device Client Project:**
```
DeviceClient/AbeckDev.DoorController.DeviceClient/
├── AbeckDev.DoorController.DeviceClient/        # Main application
│   ├── AbeckDev.DoorController.DeviceClient.csproj
│   ├── Program.cs                               # Main entry point
│   ├── Service/                                 # Core services
│   ├── Model/                                   # Data models
│   ├── Extension/                               # Extension methods
│   ├── Dockerfile                               # Docker configuration
│   └── appsettings.json                         # Configuration
├── AbeckDev.DoorController.DeviceClient.sln     # Solution file
├── setupDoorController.sh                       # Raspberry Pi setup script
├── updateDoorClient.sh                          # Update script
└── DoorDeviceClient.service                     # SystemD service file
```

**Azure Function Project:**
```
IoTCentralTriggerFunction/
├── IoTCentralTriggerFunctions.csproj           # Project file
├── IoTCentralTriggerFunctions.sln              # Solution file
├── DoorActionFunction.cs                       # HTTP trigger function
├── host.json                                   # Function host configuration
└── Properties/                                 # Project properties
```

### Repository Root Contents
```
.
├── DeviceClient/                               # Device Client projects
├── IoTCentralTriggerFunction/                  # Azure Function project
├── .devcontainer/                              # VS Code dev container config
├── .github/                                    # GitHub Actions workflows
├── .deployment                                 # Deployment configuration
├── README.md                                   # Project documentation
├── CONTRIBUTING.md                             # Contribution guidelines
├── local.settings.json                         # Local Azure Function settings
└── LICENSE                                     # MIT License
```

### Configuration Details
**Device Client (appsettings.json):**
- `IotCentralGlobalDeviceEndpoint`: Azure IoT Central endpoint
- `IotCentralScopeId`: Scope ID from IoT Central
- `IotCentralDeviceId`: Device identifier
- `IotCentralPrimaryKey`: Primary key for authentication
- `DeviceLocation`: Physical location of device
- `RemoteDoors`: Array of door configurations with names, IDs, and control codes

**Azure Function (local.settings.json):**
- `AzureWebJobsStorage`: Storage account connection string
- `FUNCTIONS_WORKER_RUNTIME`: Set to "dotnet"

### Development Environment
- VS Code dev container configuration available in `.devcontainer/`
- Container includes .NET 8, Azure CLI, and Function Core Tools
- Extensions: C# tools, Azure Functions, Docker, GitHub Copilot

### Deployment Options
1. **Raspberry Pi Direct Installation:**
   - Use `setupDoorController.sh` script as root
   - Installs .NET runtime, 433Utils dependencies, WiringPi
   - Sets up systemd service and cron monitoring
   
2. **Docker Deployment:**
   - Build Docker image using included Dockerfile
   - Run container with proper volume mounts for configuration
   
3. **Azure Function Deployment:**
   - Deploy using Azure Functions CLI or GitHub Actions
   - Requires managed identity setup for IoT Central access

### Important Files for Common Changes
- **Device functionality**: Modify `DeviceClient/AbeckDev.DoorController.DeviceClient/AbeckDev.DoorController.DeviceClient/Service/DoorService.cs`
- **IoT communication**: Check `DeviceClient/AbeckDev.DoorController.DeviceClient/AbeckDev.DoorController.DeviceClient/Service/DeviceService.cs`
- **Azure Function logic**: Modify `IoTCentralTriggerFunction/DoorActionFunction.cs`
- **Configuration**: Update respective appsettings.json files
- **Dependencies**: Check .csproj files for NuGet packages

### Build Artifacts and Timing Expectations
- **Device Client Build**: Total ~30-35 seconds (restore + build + publish)
- **Azure Function Build**: Total ~45-50 seconds (restore + build + publish)
- **Docker Build**: 5-10 minutes for full build including base image download
- **GitHub Actions**: Complete workflow runs in 3-5 minutes
- All builds are relatively fast - if builds exceed these times significantly, investigate network or configuration issues

### Troubleshooting Common Issues
- **Build Failures**: Ensure .NET 8 SDK is installed and paths are correct
- **Runtime Failures**: Check appsettings.json configuration and Azure connectivity
- **Docker Build Issues**: Verify network connectivity for NuGet package restore
- **Azure Function Issues**: Verify Functions CLI is installed and local.settings.json is configured
# IoT Door Controller

IoT Door Controller is a .NET-based system for controlling 433MHz door systems via IoT devices.

## Architecture (current)

- **Device Client**: .NET 8 console app on Raspberry Pi
  - Path: `DeviceClient/AbeckDev.DoorController.DeviceClient/AbeckDev.DoorController.DeviceClient/`
  - Cloud path (V2): Azure IoT Hub (`IoTHubConnectionString`, optional DPS)
- **Azure Function**: .NET 8 isolated worker
  - Path: `IoTCentralTriggerFunction/`
  - Cloud path (V2): IoT Hub direct methods via service SDK

## Legacy architecture (deprecated)

- V1 used Azure IoT Central plus DPS-focused config keys.
- Keep legacy behavior valid when needed, but implement new work in V2 IoT Hub mode.

## Build commands

### Device Client
```bash
cd DeviceClient/AbeckDev.DoorController.DeviceClient/
dotnet restore AbeckDev.DoorController.DeviceClient.sln
dotnet build AbeckDev.DoorController.DeviceClient.sln --configuration Release --no-restore
```

### Azure Function
```bash
cd IoTCentralTriggerFunction/
dotnet restore IoTCentralTriggerFunctions.sln
dotnet build IoTCentralTriggerFunctions.sln --configuration Release --no-restore
```

## Validation reminders

- Build both projects in Release after changes.
- Device Client startup should log configuration loading and door registrations.
- Function should accept `?door=<id>` and dispatch IoT Hub direct method commands.

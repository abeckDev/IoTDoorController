# IoT Door Controller

IoT Door Controller is a .NET 8 solution for controlling 433MHz doors from a Raspberry Pi.

## V2 (current, breaking change): Azure IoT Hub

V2 migrates cloud/device communication from Azure IoT Central to Azure IoT Hub:

- **Device Client** (`DeviceClient/.../.NET 8`)
  - Default auth: `IoTHubConnectionString`
  - Optional auth: DPS (`DpsGlobalDeviceEndpoint`, `DpsIdScope`, `DpsRegistrationId`, `DpsSymmetricKey`)
  - Direct-method handler: `DoorCommand` (configurable via `DirectMethodName`)
- **Azure Function** (`IoTCentralTriggerFunction/.NET 8 isolated`)
  - Sends direct methods using IoT Hub service SDK (`Microsoft.Azure.Devices`)
  - Uses `IoTHubConnectionString` **or** `IoTHubHostName` + `DefaultAzureCredential`
  - Keeps optional announcement webhook behavior unchanged
  - Folder name is intentionally kept as `IoTCentralTriggerFunction` for compatibility with existing pipelines/scripts

## V1 (legacy/deprecated): Azure IoT Central

V1 IoT Central/DPS settings are considered legacy and deprecated.  
Main branch history still represents the legacy approach; V2 is the active direction for new deployments.

## V2 configuration

### Device Client `appsettings.json`

```json
{
  "IoTHubConnectionString": "HostName=your-hub.azure-devices.net;DeviceId=your-device-id;SharedAccessKey=your-device-key",
  "DpsGlobalDeviceEndpoint": "global.azure-devices-provisioning.net",
  "DpsIdScope": "",
  "DpsRegistrationId": "",
  "DpsSymmetricKey": "",
  "DirectMethodName": "DoorCommand",
  "CooldownIntervalInMilliseconds": 5000,
  "UpdateIntervalInMilliseconds": 900000,
  "DeviceLocation": "Backyard",
  "RemoteDoors": [
    {
      "Name": "Main Gate",
      "ID": 1,
      "Decimalcode": "5510420"
    }
  ]
}
```

### Azure Function app settings

- `IoTHubConnectionString` **or** `IoTHubHostName`
- `TargetDeviceId`
- `DirectMethodName` (default: `DoorCommand`)
- `Announcement_TriggerUrl` (optional)
- `Announcement_Token` (optional)
- `Announcement_FlowId` (optional)

## Build

```bash
cd DeviceClient/AbeckDev.DoorController.DeviceClient/
dotnet restore AbeckDev.DoorController.DeviceClient.sln
dotnet build AbeckDev.DoorController.DeviceClient.sln --configuration Release --no-restore

cd /home/runner/work/IoTDoorController/IoTDoorController/IoTCentralTriggerFunction/
dotnet restore IoTCentralTriggerFunctions.sln
dotnet build IoTCentralTriggerFunctions.sln --configuration Release --no-restore
```

## Raspberry Pi install/update scripts

- Setup: `DeviceClient/AbeckDev.DoorController.DeviceClient/setupDoorController.sh`
- Update: `DeviceClient/AbeckDev.DoorController.DeviceClient/updateDoorClient.sh`

Both scripts now reference the `main` branch for raw GitHub helper files.

## Migration guide

See [MIGRATION.md](MIGRATION.md) for V1 → V2 migration steps.

# V1 to V2 Migration Guide (IoT Central → IoT Hub)

## Breaking changes

1. Device cloud connectivity moves from IoT Central-focused DPS config to IoT Hub.
2. Azure Function command dispatch moves from IoT Central REST API to IoT Hub direct methods.
3. Configuration keys change.

## Device Client migration

### Old (V1)
- `IotCentralGlobalDeviceEndpoint`
- `IotCentralScopeId`
- `IotCentralDeviceId`
- `IotCentralPrimaryKey`

### New (V2)
- `IoTHubConnectionString` (recommended)
- Optional DPS path:
  - `DpsGlobalDeviceEndpoint`
  - `DpsIdScope`
  - `DpsRegistrationId`
  - `DpsSymmetricKey`
- `DirectMethodName` (optional, default `DoorCommand`)

## Azure Function migration

### Old (V1)
- `IoTAppName`
- `IoTDeviceName`
- `CommandName`

### New (V2)
- `IoTHubConnectionString` **or** `IoTHubHostName`
- `TargetDeviceId`
- `DirectMethodName`

Announcement settings are unchanged:
- `Announcement_TriggerUrl`
- `Announcement_Token`
- `Announcement_FlowId`

## Azure setup checklist (V2)

1. Create IoT Hub.
2. Register the Raspberry Pi device identity.
3. Copy the device connection string into Device Client `appsettings.json`.
4. Configure Function App settings with IoT Hub service access and target device id.
5. Deploy/restart Device Client and Azure Function.
6. Call the function endpoint with `?door=<id>` and verify direct-method execution.

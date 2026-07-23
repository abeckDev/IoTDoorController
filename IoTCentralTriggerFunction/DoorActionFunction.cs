using System.Net;
using Azure.Identity;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace IoTCentralTriggerFunctions;

public class DoorActionFunction
{
    private readonly ILogger<DoorActionFunction> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DefaultAzureCredential _credential;

    public DoorActionFunction(
        ILogger<DoorActionFunction> logger,
        IHttpClientFactory httpClientFactory,
        DefaultAzureCredential credential)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _credential = credential;
    }

    [Function("DoorActionFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        _logger.LogInformation("Door control request received (V2 IoT Hub mode).");

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var door = query["door"];

            if (string.IsNullOrWhiteSpace(door))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required 'door' parameter" });
                return badResponse;
            }

            if (!int.TryParse(door, out var doorId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Door parameter must be numeric" });
                return badResponse;
            }

            var config = GetConfiguration();
            if (!config.IsValid)
            {
                _logger.LogError("Missing required configuration: {MissingVars}", string.Join(", ", config.MissingVariables));
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Server configuration error" });
                return errorResponse;
            }

            if (config.AnnouncementConfigured)
            {
                await TriggerAnnouncementAsync(config, req.FunctionContext.CancellationToken);
            }

            var success = await SendIoTHubCommandAsync(config, doorId, req.FunctionContext.CancellationToken);
            var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new
            {
                message = $"Command sent to door: {doorId}",
                success,
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing door control request");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "An unexpected error occurred" });
            return errorResponse;
        }
    }

    private async Task TriggerAnnouncementAsync(Configuration config, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{config.AnnouncementTriggerUrl}?token={config.AnnouncementToken}&flow={config.AnnouncementFlowId}";
            var response = await httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Announcement triggered successfully. Response: {Response}", responseBody);
            }
            else
            {
                _logger.LogWarning("Announcement request failed with status code: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger announcement, continuing with door control");
        }
    }

    private async Task<bool> SendIoTHubCommandAsync(Configuration config, int doorId, CancellationToken cancellationToken)
    {
        ServiceClient? serviceClient = null;
        try
        {
            serviceClient = string.IsNullOrWhiteSpace(config.IoTHubConnectionString)
                ? ServiceClient.Create(config.IoTHubHostName!, _credential)
                : ServiceClient.CreateFromConnectionString(config.IoTHubConnectionString);

            var method = new CloudToDeviceMethod(config.DirectMethodName)
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
                ConnectionTimeout = TimeSpan.FromSeconds(30),
            };
            method.SetPayloadJson(doorId.ToString());

            _logger.LogInformation("Sending IoT Hub direct method {MethodName} to {DeviceId}", config.DirectMethodName, config.TargetDeviceId);
            var response = await serviceClient.InvokeDeviceMethodAsync(config.TargetDeviceId, method, cancellationToken);

            if (response.Status >= 200 && response.Status < 300)
            {
                _logger.LogInformation("IoT Hub direct method succeeded with status: {Status}", response.Status);
                return true;
            }

            _logger.LogError("IoT Hub direct method failed with status: {Status}, payload: {Payload}", response.Status, response.GetPayloadAsJson());
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send IoT Hub direct method");
            return false;
        }
        finally
        {
            if (serviceClient != null)
            {
                await serviceClient.CloseAsync();
                serviceClient.Dispose();
            }
        }
    }

    private static Configuration GetConfiguration()
    {
        return new Configuration
        {
            IoTHubHostName = Environment.GetEnvironmentVariable("IoTHubHostName"),
            IoTHubConnectionString = Environment.GetEnvironmentVariable("IoTHubConnectionString"),
            TargetDeviceId = Environment.GetEnvironmentVariable("TargetDeviceId"),
            DirectMethodName = Environment.GetEnvironmentVariable("DirectMethodName") ?? "DoorCommand",
            AnnouncementTriggerUrl = Environment.GetEnvironmentVariable("Announcement_TriggerUrl"),
            AnnouncementToken = Environment.GetEnvironmentVariable("Announcement_Token"),
            AnnouncementFlowId = Environment.GetEnvironmentVariable("Announcement_FlowId"),
        };
    }

    private class Configuration
    {
        public string? IoTHubHostName { get; init; }
        public string? IoTHubConnectionString { get; init; }
        public string? TargetDeviceId { get; init; }
        public string? DirectMethodName { get; init; }
        public string? AnnouncementTriggerUrl { get; init; }
        public string? AnnouncementToken { get; init; }
        public string? AnnouncementFlowId { get; init; }

        public bool IsValid =>
            (!string.IsNullOrWhiteSpace(IoTHubHostName) || !string.IsNullOrWhiteSpace(IoTHubConnectionString)) &&
            !string.IsNullOrWhiteSpace(TargetDeviceId) &&
            !string.IsNullOrWhiteSpace(DirectMethodName);

        public bool AnnouncementConfigured =>
            !string.IsNullOrWhiteSpace(AnnouncementTriggerUrl) &&
            !string.IsNullOrWhiteSpace(AnnouncementToken) &&
            !string.IsNullOrWhiteSpace(AnnouncementFlowId);

        public IEnumerable<string> MissingVariables
        {
            get
            {
                if (string.IsNullOrWhiteSpace(IoTHubHostName) && string.IsNullOrWhiteSpace(IoTHubConnectionString)) yield return "IoTHubHostName or IoTHubConnectionString";
                if (string.IsNullOrWhiteSpace(TargetDeviceId)) yield return "TargetDeviceId";
                if (string.IsNullOrWhiteSpace(DirectMethodName)) yield return "DirectMethodName";
            }
        }
    }
}

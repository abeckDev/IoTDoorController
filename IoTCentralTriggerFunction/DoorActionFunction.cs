using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
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
        _logger.LogInformation("Door control request received");

        try
        {
            // Validate and extract door parameter
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var door = query["door"];

            if (string.IsNullOrWhiteSpace(door))
            {
                _logger.LogWarning("Missing or invalid 'door' parameter");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required 'door' parameter" });
                return badResponse;
            }

            // Validate door parameter is numeric
            if (!int.TryParse(door, out var doorId))
            {
                _logger.LogWarning("Invalid door parameter: {Door}", door);
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Door parameter must be numeric" });
                return badResponse;
            }

            _logger.LogInformation("Processing door control request for door: {DoorId}", doorId);

            // Get required environment variables
            var config = GetConfiguration();
            if (!config.IsValid)
            {
                _logger.LogError("Missing required configuration: {MissingVars}", 
                    string.Join(", ", config.MissingVariables));
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Server configuration error" });
                return errorResponse;
            }

            // Get Azure AD token for IoT Central
            AccessToken token;
            try
            {
                token = await _credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://apps.azureiotcentral.com/.default" }),
                    req.FunctionContext.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire Azure AD token");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Authentication failed" });
                return errorResponse;
            }

            // Trigger announcement if configured
            if (config.AnnouncementConfigured)
            {
                await TriggerAnnouncementAsync(config, req.FunctionContext.CancellationToken);
            }

            // Send IoT Central command
            var success = await SendIoTCentralCommandAsync(config, doorId, token.Token, req.FunctionContext.CancellationToken);
            
            var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new 
            { 
                message = $"Command sent to door: {doorId}",
                success = success
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
            _logger.LogInformation("Triggering announcement");
            
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

    private async Task<bool> SendIoTCentralCommandAsync(
        Configuration config, 
        int doorId, 
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://{config.IoTAppName}.azureiotcentral.com/api/devices/{config.IoTDeviceName}/commands/{config.CommandName}?api-version=2022-07-31";
            
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            var payload = new { request = doorId };
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload), 
                Encoding.UTF8, 
                "application/json");

            _logger.LogInformation("Sending command to IoT Central device: {DeviceName}", config.IoTDeviceName);
            
            var response = await httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("IoT Central command sent successfully");
                return true;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("IoT Central command failed with status {StatusCode}: {Error}", 
                    response.StatusCode, errorBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send IoT Central command");
            return false;
        }
    }

    private Configuration GetConfiguration()
    {
        var config = new Configuration
        {
            IoTAppName = Environment.GetEnvironmentVariable("IoTAppName"),
            IoTDeviceName = Environment.GetEnvironmentVariable("IoTDeviceName"),
            CommandName = Environment.GetEnvironmentVariable("CommandName"),
            AnnouncementTriggerUrl = Environment.GetEnvironmentVariable("Announcement_TriggerUrl"),
            AnnouncementToken = Environment.GetEnvironmentVariable("Announcement_Token"),
            AnnouncementFlowId = Environment.GetEnvironmentVariable("Announcement_FlowId")
        };

        return config;
    }

    private class Configuration
    {
        public string? IoTAppName { get; init; }
        public string? IoTDeviceName { get; init; }
        public string? CommandName { get; init; }
        public string? AnnouncementTriggerUrl { get; init; }
        public string? AnnouncementToken { get; init; }
        public string? AnnouncementFlowId { get; init; }

        public bool IsValid => 
            !string.IsNullOrWhiteSpace(IoTAppName) &&
            !string.IsNullOrWhiteSpace(IoTDeviceName) &&
            !string.IsNullOrWhiteSpace(CommandName);

        public bool AnnouncementConfigured =>
            !string.IsNullOrWhiteSpace(AnnouncementTriggerUrl) &&
            !string.IsNullOrWhiteSpace(AnnouncementToken) &&
            !string.IsNullOrWhiteSpace(AnnouncementFlowId);

        public IEnumerable<string> MissingVariables
        {
            get
            {
                if (string.IsNullOrWhiteSpace(IoTAppName)) yield return "IoTAppName";
                if (string.IsNullOrWhiteSpace(IoTDeviceName)) yield return "IoTDeviceName";
                if (string.IsNullOrWhiteSpace(CommandName)) yield return "CommandName";
            }
        }
    }
}

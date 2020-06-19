using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace IoTCentralTriggerFunctions
{
    public static class DeviceHealthCheckFunction
    {
        [FunctionName("DeviceHealthCheckFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            using (var client = new HttpClient())
            {
                string IoTAppName = Environment.GetEnvironmentVariable("IoTAppName");
                string IoTDeviceName = Environment.GetEnvironmentVariable("IoTDeviceName");
                string IoTComponentName = Environment.GetEnvironmentVariable("IoTComponentName");
                bool isConvertibleToInt = Int32.TryParse(Environment.GetEnvironmentVariable("IoTDeviceTimeoutInMinutes"), out int IoTDeviceTimeoutInMinutes);
                string IoTCoreAccessToken = Uri.EscapeDataString(Environment.GetEnvironmentVariable("IoTCentralToken"));

                if (!isConvertibleToInt)
                {
                    return new BadRequestObjectResult("Error in Config");
                }

                string ApiUrl = $"https://{IoTAppName}.azureiotcentral.com/api/preview/devices/{IoTDeviceName}/components/{IoTComponentName}/properties?access_token={IoTCoreAccessToken}";
                var response = await client.GetAsync(ApiUrl);
                var content = await response.Content.ReadAsStringAsync();

                JObject responseJson = JObject.Parse(content);
                DateTime lastHealthcheck = responseJson["$metadata"]["DeviceStatus"]["ackLastUpdatedTimestamp"].ToObject<DateTime>();
                TimeSpan lastUpdateDiff = DateTime.Now.Subtract(lastHealthcheck);
                log.LogInformation($"last healthcheck: {lastHealthcheck.ToString()} and current time: {DateTime.Now}");
                log.LogInformation($"Calculated diff: {lastUpdateDiff.ToString()} equals: Days={lastUpdateDiff.Days}, Hours={lastUpdateDiff.Hours}, Minutes={lastUpdateDiff.Minutes}");
                if (lastUpdateDiff.Days >= 1 || lastUpdateDiff.Minutes > IoTDeviceTimeoutInMinutes)
                {
                    //Device is Timed out
                    //Set Device to Offline in IoT Central
                    ApiUrl = $"https://{IoTAppName}.azureiotcentral.com/api/preview/devices/{IoTDeviceName}/cloudProperties?access_token={IoTCoreAccessToken}";
                    var body = new StringContent("{\"DeviceStatus\": \"offline\"}", Encoding.UTF8, "application/json");
                    var debug = await client.PutAsync(ApiUrl, body);
                    return new OkObjectResult("Offline");
                }
                else
                {
                    //Set Device to online in IoT Central
                    ApiUrl = $"https://{IoTAppName}.azureiotcentral.com/api/preview/devices/{IoTDeviceName}/cloudProperties?access_token={IoTCoreAccessToken}";
                    var data = new StringContent("{\"DeviceStatus\": \"ready\"}", Encoding.UTF8, "application/json");
                    await client.PutAsync(ApiUrl, data);
                    return new OkObjectResult("Ok");
                }
            }
        }
    }
}

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
using System.Net;
using System.Web;
using System.Web.Http;
using System.Text;
using System.Text.Encodings.Web;
using Azure.Identity;


namespace IoTCentralTriggerFunctions
{
    public static class DoorActionFunction
    {
        [FunctionName("DoorActionFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");


            //Check if GET is used
            if (req.Method != HttpMethod.Get.ToString())
            {
                //Send an Error if wrong HTTP method is used
                return new BadRequestObjectResult("Bad Request");
            }

            //Define the variable which will be used to store the query parameter of the door being addressed
            string door;
            // parse query parameter
            try
            {
                door = req.GetQueryParameterDictionary()["door"];
            }
            catch (Exception)
            {
                return new BadRequestObjectResult("Bad Request");
            }

            log.LogInformation("Received request to control door " + door);
            //log.LogInformation("IoT Core Token = " + token);

            //Get a AzureAD Token from Azure Default Credentials to be used for IoT Central API Call
            var credential = new DefaultAzureCredential();
            var token = credential.GetToken(new Azure.Core.TokenRequestContext(new[] { "https://apps.azureiotcentral.com/.default" }));

            //Send IoT Central API Call to open/close door
            using (var client = new HttpClient())
            {
                string IoTAppName = Environment.GetEnvironmentVariable("IoTAppName");
                string IoTDeviceName = Environment.GetEnvironmentVariable("IoTDeviceName");
                string IoTComponentName = Environment.GetEnvironmentVariable("IoTComponentName");
                string CommandName = Environment.GetEnvironmentVariable("CommandName");

                //Obsolete content
                //string IoTCoreAccessToken = Uri.EscapeDataString(Environment.GetEnvironmentVariable("IoTCentralToken"));
                //string ApiUrl = $"https://.azureiotcentral.com/api/preview/devices/{IoTDeviceName}/components/{IoTComponentName}/commands/{CommandName}?access_token={IoTCoreAccessToken}";

                //Build the API Call Url
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri($"https://{IoTAppName}.azureiotcentral.com/api/devices/{IoTDeviceName}/components/{IoTComponentName}/commands/{CommandName}?api-version=2022-07-31"),
                    Method = HttpMethod.Post,
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                request.Content = new StringContent("{\"request\": " + door + "}", Encoding.UTF8, "application/json");

                //Send the request
                var response = await client.SendAsync(request);

                //Debug only
                var debug = response;
            }
            string responseMessage = "I sent the command to door: " + door;

            return new OkObjectResult(responseMessage);

        }
    }
}

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


            if (req.Method != HttpMethod.Get.ToString())
            {
                //Send an Error if wrong HTTP method is used

                return new BadRequestObjectResult("Bad Request");
            }
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


            log.LogInformation("Received request to controll door " + door);
            //log.LogInformation("IoT Core Token = " + token);


            using (var client = new HttpClient())
            {
                string IoTAppName = Environment.GetEnvironmentVariable("IoTAppName");
                string IoTDeviceName = Environment.GetEnvironmentVariable("IoTDeviceName");
                string IoTComponentName = Environment.GetEnvironmentVariable("IoTComponentName");
                string CommandName = Environment.GetEnvironmentVariable("CommandName");


                string IoTCoreAccessToken = Uri.EscapeDataString(Environment.GetEnvironmentVariable("IoTCentralToken"));


                string ApiUrl = $"https://{IoTAppName}.azureiotcentral.com/api/preview/devices/{IoTDeviceName}/components/{IoTComponentName}/commands/{CommandName}?access_token={IoTCoreAccessToken}";

                var data = new StringContent("{\"request\": " + door + "}", Encoding.UTF8, "application/json");
                var response = await client.PostAsync(ApiUrl, data);

                var debug = response;
            }

            string responseMessage = "I sent the command to door: " + door;
            return new OkObjectResult(responseMessage);

        }
    }
}

using Azure;
using DBAccess;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using TravelAd_Api.Models;
using static TravelAd_Api.Models.SmsModel;

namespace TravelAd_Api.Controllers
{
    [Route("[controller]/api")]
    [ApiController]
    [EnableCors("AllowSpecificOrigin")]
    public class SmsController : ControllerBase
    {
        //private readonly SmppClientService _smppClientService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;


        public SmsController(HttpClient httpClient, IConfiguration configuration)
        {
           // _smppClientService = smppClientService ?? throw new ArgumentNullException(nameof(smppClientService));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }


        [HttpGet("getchannels")]
        public async Task<IActionResult> GetAccountListAsync([FromServices] IDbHandler dbHandler)
        {
            try
            {
                // Get the base URL of the other project from configuration
                var otherProjectBaseUrl = _configuration["OtherProject:BaseUrl"];

                // Create the full URL for the endpoint you want to call
                var url = $"{otherProjectBaseUrl}/Message/getchannels";

                // Make the HTTP GET request
                var response = await _httpClient.GetAsync(url);

                // Ensure the request was successful
                response.EnsureSuccessStatusCode();

                // Read and return the response content
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(responseContent);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"Failed to call the other project: {ex.Message}"
                });
            }
        }





        [HttpPost("createchannel")]
        public async Task<IActionResult> CreateConnectionAsync([FromBody] CreateSmppChannel cc)
        {
            try
            {
                // Get base URL from configuration
                var otherProjectBaseUrl = _configuration["OtherProject:BaseUrl"];

                if (string.IsNullOrEmpty(otherProjectBaseUrl))
                {
                    return StatusCode(500, new { Status = "Error", Status_Description = "Other project BaseUrl is not configured." });
                }

                // Construct full URL
                var url = $"{otherProjectBaseUrl}/Message/createchannel";

                // Serialize the request body
                var jsonContent = new StringContent(JsonConvert.SerializeObject(cc), Encoding.UTF8, "application/json");

                // Make HTTP POST request with the serialized content
                var response = await _httpClient.PostAsync(url, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest(new
                    {
                        Status = "Error",
                        Status_Description = $"Failed to create channel. HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}"
                    });
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(JsonConvert.DeserializeObject<StatusBody>(responseContent));
                // Read response content

            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"Failed to call the other project: {ex.Message}"
                });
            }
        }

        /// 📌 Connect to SMPP Server (Calls MessageController in another project)
        [HttpPost("connect")]
        public async Task<IActionResult> ConnectAsync([FromBody] SmppConnectionRequest request)
        {
            if (request == null)
                return BadRequest(new { Status = "Error", Status_Description = "❌ Invalid request. Please provide connection details." });

            try
            {
                var otherProjectBaseUrl = _configuration["OtherProject:BaseUrl"];
                var url = $"{otherProjectBaseUrl}/Message/connect";

                var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);

                return await HandleApiResponse(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to connect to SMPP server.");
            }
        }

        /// 📌 Send SMS (Calls MessageController in another project)
        [HttpPost("send")]
        public async Task<IActionResult> SendSmsAsync([FromBody] SendSmsRequest request)
        {
            if (request == null)
                return BadRequest(new { Status = "Error", Status_Description = "❌ Invalid request. Please provide SMS details." });

            try
            {
                var otherProjectBaseUrl = _configuration["OtherProject:BaseUrl"];
                var url = $"{otherProjectBaseUrl}/Message/send";

                var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);

                return await HandleApiResponse(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error sending SMS.");
            }
        }

        /// 📌 Send Bulk SMS (Calls MessageController in another project)
        [HttpPost("sendBulk")]
        public async Task<IActionResult> SendBulkSmsAsync([FromBody] SendBulkSmsRequest request)
        {
            if (request == null || request.ChannelId == 0 || request.Recipients == null || request.Recipients.Count == 0)
            {
                return BadRequest(new
                {
                    Status = "Error",
                    Status_Description = "❌ Invalid request. Please provide a valid ChannelId and list of recipients.",
                });
            }

            try
            {
                var otherProjectBaseUrl = _configuration["OtherProject:BaseUrl"];
                var url = $"{otherProjectBaseUrl}/Message/sendBulk";

                var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);

                return await HandleApiResponse(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error sending bulk SMS.");
            }
        }


        /// 📌 Disconnect from SMPP Server (Calls MessageController in another project)
        [HttpPost("disconnect")]
        public async Task<IActionResult> DisconnectAsync(int channelId)
        {
            try
            {
                var otherProjectBaseUrl = _configuration["OtherProject:BaseUrl"];
                var url = $"{otherProjectBaseUrl}/Message/disconnect?channelId={channelId}";

                var response = await _httpClient.PostAsync(url, null);
                return await HandleApiResponse(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to disconnect from SMPP server.");
            }
        }

        /// 📌 Check if SMPP is Connected (Calls MessageController in another project)
        [HttpGet("isAlive")]
        public async Task<IActionResult> IsSMPPConnectedAsync(int channelId)
        {
            try
            {
                var otherProjectBaseUrl = _configuration["OtherProject:BaseUrl"];
                var url = $"{otherProjectBaseUrl}/Message/isAlive?channelId={channelId}";

                var response = await _httpClient.GetAsync(url);
                return await HandleApiResponse(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error checking SMPP service.");
            }
        }

        /// 🔹 Common Method to Handle API Responses
        private async Task<IActionResult> HandleApiResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return Ok(JsonConvert.DeserializeObject<StatusBody>(responseContent));

            return StatusCode((int)response.StatusCode, JsonConvert.DeserializeObject<StatusBody>(responseContent));
        }

        /// 🔹 Common Method to Handle Exceptions
        private IActionResult HandleException(Exception ex, string errorMessage)
        {
            return StatusCode(500, new { Status = "Error", Status_Description = $"❌ {errorMessage}: {ex.Message}" });
        }


    }
}

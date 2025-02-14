using DBAccess;
using System.Data;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using static TravelAd_Api.Models.AdvertiserAccountModel;
using static System.Net.WebRequestMethods;
using Azure;
using System.Collections.Concurrent;
using System.Threading.Channels;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;

public class Dialler
{
    private readonly IDbHandler _dbHandler;

    // Constructor to inject the DbHandler dependency
    public Dialler(IDbHandler dbHandler)
    {
        _dbHandler = dbHandler;
    }
    private WhatsappAccountDetails GetWhatsappAccountDetailsByWId(int workspaceId)
    {
        string procedure = "GetWhatsappAccountDetailsById";

        var parameters = new Dictionary<string, object>
    {
        { "@WorkspaceId", workspaceId }
    };
        DataTable campaignDetailsById = _dbHandler.ExecuteDataTable(procedure, parameters, CommandType.StoredProcedure);

        if (campaignDetailsById.Rows.Count == 0)
        {
            return null;
        }

        return new WhatsappAccountDetails
        {
            WabaId = campaignDetailsById.Rows[0]["wabaId"].ToString(),
            PhoneId = campaignDetailsById.Rows[0]["phoneId"].ToString(),
            AccessToken = campaignDetailsById.Rows[0]["accessToken"].ToString()
        };
    }

    public class MessageCounter
    {
        private readonly ConcurrentDictionary<string, int> _campaignMessageCounts = new ConcurrentDictionary<string, int>();

        // Increment the counter for a specific campaign ID
        public void Increment(string campaignId)
        {
            _campaignMessageCounts.AddOrUpdate(campaignId, 1, (key, currentValue) => currentValue + 1);
        }

        // Get the count for a specific campaign ID
        public int GetCount(string campaignId)
        {
            _campaignMessageCounts.TryGetValue(campaignId, out var count);
            return count;
        }

        // Get all campaign message counts
        public IDictionary<string, int> GetAllCounts()
        {
            return new Dictionary<string, int>(_campaignMessageCounts);
        }
    }

    // Method to process campaigns asynchronously
    public async Task ProcessCampaignsAsync()
    {
        Console.WriteLine("welcome...........");
        while (true)
        {
            try
            {
                var dt = _dbHandler.ExecuteDataTable("EXEC GetOpenCampaignDetails");

                if (dt.Rows.Count > 0)
                {
                    var messageCounter = new MessageCounter();
                    var campaignData = new List<(string campaignId, string listId, string firstName, string lastName, string phoneNo, string campaignName, string templateName, string channelType, string startDate, string endDate, string messageFrequency, string deliveryStartTime, string deliveryEndTime, string workspaceInfoId, int serverId, string smsNumber, int campaignBudget)>();

                    foreach (DataRow row in dt.Rows)
                    {
                        campaignData.Add((
                            campaignId: row["campaign_id"].ToString(),
                            listId: row["list_id"].ToString(),
                            firstName: row["firstname"].ToString(),
                            lastName: row["lastname"].ToString(),
                            phoneNo: row["phoneno"].ToString(),
                            campaignName: row["campaign_name"].ToString(),
                            templateName: row["template_full_name"].ToString(),
                            channelType: row["channel_full_type"].ToString(),
                            startDate: row["start_date_time"].ToString(),
                            endDate: row["end_date_time"].ToString(),
                            messageFrequency: row["message_frequency"].ToString(),
                            deliveryStartTime: row["delivery_start_time"].ToString(),
                            deliveryEndTime: row["delivery_end_time"].ToString(),
                            workspaceInfoId: row["workspace_id"].ToString(),
                            serverId: Convert.ToInt32(row["smpp_id"]),
                            smsNumber: row["sms_number"].ToString(),
                            campaignBudget: Convert.ToInt32(row["campaign_budget"])

                        ));
                    }

                    foreach (var campaign in campaignData)
                    {
                        DateTime campaignStartDate = DateTime.Parse(campaign.startDate);
                        DateTime campaignEndDate = DateTime.Parse(campaign.endDate);
                        DateTime currentDateTime = DateTime.Now;
                        TimeSpan currentTime = currentDateTime.TimeOfDay;
                        TimeSpan deliveryStartTime = TimeSpan.Zero;
                        TimeSpan deliveryEndTime = TimeSpan.Zero;

                        if (!string.IsNullOrEmpty(campaign.deliveryStartTime))
                        {
                            deliveryStartTime = TimeSpan.Parse(campaign.deliveryStartTime);
                        }

                        if (!string.IsNullOrEmpty(campaign.deliveryEndTime))

                        {
                            deliveryEndTime = TimeSpan.Parse(campaign.deliveryEndTime);
                        }
                        if (currentDateTime >= campaignStartDate && currentDateTime <= campaignEndDate)
                        {

                            var billingDetailsDt = _dbHandler.ExecuteDataTable($"EXEC GetWorkspaceBillingDetails {campaign.workspaceInfoId}");

                            if (billingDetailsDt.Rows.Count > 0)
                            {
                                decimal perMessageCost = Convert.ToDecimal(billingDetailsDt.Rows[0]["per_message"]);
                                if (campaign.campaignBudget < perMessageCost)
                                {
                                    Console.WriteLine($"Campaign {campaign.campaignId} has insufficient budget. Required: {perMessageCost}, Available: {campaign.campaignBudget}");
                                    continue;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"No billing details found for workspace {campaign.workspaceInfoId}. Skipping campaign {campaign.campaignId}.");
                                continue;
                            }
                            if (campaign.messageFrequency == "Every Day")
                            {
                                if (currentTime >= deliveryStartTime && currentTime <= deliveryEndTime)
                                {
                                    RunCampaign((campaign.campaignId, campaign.listId, campaign.firstName, campaign.lastName, campaign.phoneNo, campaign.campaignName, campaign.templateName, campaign.channelType, campaign.startDate, campaign.serverId, campaign.smsNumber, campaign.endDate), messageCounter);

                                }
                                else
                                {
                                    Console.WriteLine($"Current time is outside the delivery window for campaign {campaign.campaignId}.");
                                }
                            }
                            else if (campaign.messageFrequency == "Every 2 Days")
                            {

                                int daysSinceStart = (currentDateTime.Date - campaignStartDate.Date).Days;
                                int cycleNumber = daysSinceStart / 4;
                                int dayInCycle = daysSinceStart % 4;
                                if (dayInCycle <= 2)
                                {
                                    if (currentTime >= deliveryStartTime && currentTime <= deliveryEndTime)
                                    {
                                        RunCampaign((campaign.campaignId, campaign.listId, campaign.firstName, campaign.lastName, campaign.phoneNo, campaign.campaignName, campaign.templateName, campaign.channelType, campaign.startDate, campaign.serverId, campaign.smsNumber, campaign.endDate), messageCounter);
                                        Console.WriteLine($"Running campaign {campaign.campaignId} - Cycle {cycleNumber + 1}, Day {dayInCycle + 1} of active period");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Current time is outside the delivery window for campaign {campaign.campaignId}.");
                                    }
                                }



                            }
                            else if (campaign.messageFrequency == "Every 3 Days")
                            {

                                int daysSinceStart = (currentDateTime.Date - campaignStartDate.Date).Days;
                                int cycleNumber = daysSinceStart / 6;
                                int dayInCycle = daysSinceStart % 6;
                                if (dayInCycle <= 3)
                                {
                                    if (currentTime >= deliveryStartTime && currentTime <= deliveryEndTime)
                                    {
                                        RunCampaign((campaign.campaignId, campaign.listId, campaign.firstName, campaign.lastName, campaign.phoneNo, campaign.campaignName, campaign.templateName, campaign.channelType, campaign.startDate, campaign.serverId, campaign.smsNumber, campaign.endDate), messageCounter);
                                        Console.WriteLine($"Running campaign {campaign.campaignId} - Cycle {cycleNumber + 1}, Day {dayInCycle + 1} of active period");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Current time is outside the delivery window for campaign {campaign.campaignId}.");
                                    }
                                }



                            }
                            else if (campaign.messageFrequency == "Once a week")
                            {

                                int daysSinceStart = (currentDateTime.Date - campaignStartDate.Date).Days;

                                // Calculate the week number (0-based)
                                int weekNumber = daysSinceStart / 7;

                                // Calculate the day within the current week (0-6)
                                int dayInWeek = daysSinceStart % 7;

                                // Run campaign only on the first day of each week
                                if (dayInWeek == 0)
                                {
                                    if (currentTime >= deliveryStartTime && currentTime <= deliveryEndTime)
                                    {
                                        RunCampaign((campaign.campaignId, campaign.listId, campaign.firstName, campaign.lastName, campaign.phoneNo, campaign.campaignName, campaign.templateName, campaign.channelType, campaign.startDate, campaign.serverId, campaign.smsNumber, campaign.endDate), messageCounter);
                                        //Console.WriteLine($"Running campaign {campaign.campaignId} - Cycle {cycleNumber + 1}, Day {dayInCycle + 1} of active period");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Current time is outside the delivery window for campaign {campaign.campaignId}.");
                                    }
                                }



                            }

                        }
                    }


                    foreach (var countEntry in messageCounter.GetAllCounts())
                    {
                        Console.WriteLine($"Campaign ID: {countEntry.Key}, Messages Sent: {countEntry.Value}");
                        var channel = _dbHandler.ExecuteDataTable($"EXEC GetChannelTypeByCampaignId {(countEntry.Key.ToString())}");

                        // Check if channel.Rows is not null and has at least one row before accessing
                        if (channel != null && channel.Rows.Count > 0)
                        {
                            string channelType = channel.Rows[0]["channel_name"]?.ToString(); // Use null conditional operator

                            string procedure = "InsertCampaignSent";
                            string sentDate = DateTime.Now.ToString("yyyy-MM-dd");

                            // Check if channelType is not null and if it's either "WhatsApp" or "SMS"
                            if (!string.IsNullOrEmpty(channelType))
                            {
                                var parameters = new Dictionary<string, object>
            {
                { "@CampaignId", countEntry.Key },
                { "@SentDate", sentDate }
            };

                                if (channelType == "WhatsApp")
                                {
                                    parameters.Add("@WhatsApp", countEntry.Value);
                                }
                                else if (channelType == "SMS")
                                {
                                    parameters.Add("@Sms", countEntry.Value);
                                }

                                // Only call the stored procedure if the channelType was valid
                                if (parameters.ContainsKey("@WhatsApp") || parameters.ContainsKey("@Sms"))
                                {
                                    object result = _dbHandler.ExecuteScalar(procedure, parameters, CommandType.StoredProcedure);

                                    if (result != null)
                                    {
                                        Console.WriteLine($"Stored {countEntry.Value} messages for campaign {countEntry.Key} in database.");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Error storing messages for campaign {countEntry.Key} in database.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Error: Channel information for campaign {countEntry.Key} not found.");
                        }
                    }


                }
                else
                {
                    Console.WriteLine("No campaigns with status 'Open' found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessCampaignsAsync method: {ex.Message}");
            }

            await Task.Delay(2000);
        }
    }


    private async void RunCampaign(
      (string campaignId, string listId, string firstName, string lastName, string phoneNo,
      string campaignName, string templateName, string channelType, string startDate, int serverId, string smsNumber, string endDate) campaign,
      MessageCounter messageCounter)
    {
        var parameters = new Dictionary<string, object>
    {
        { "@campaign_id", int.Parse(campaign.campaignId) },
        { "@percentage", 25.0m } // Adjust percentage as needed
    };

        // Get the percentage of contacts to fetch
        var dtPercentage = _dbHandler.ExecuteDataTable("EXEC GetPercentageOfCampaignContacts @campaign_id, @percentage", parameters);

        if (dtPercentage.Rows.Count > 0)
        {
            foreach (DataRow row in dtPercentage.Rows)
            {
                string phoneNo = row["phoneno"].ToString();
                Console.WriteLine($"Processing phone number: {phoneNo}");

                // Run campaign processing asynchronously
                await Task.Run(() =>
                {
                    Console.WriteLine($"Processing Campaign: {campaign.campaignId}");
                    ProcessCampaign(campaign, messageCounter);
                });
            }
        }
        else
        {
            Console.WriteLine($"No data returned for campaign {campaign.campaignId}.");
        }
    }


    private async Task ProcessCampaign((string campaignId, string listId, string firstName, string lastName, string phoneNo, string campaignName, string templateName, string channelType, string startDate,int serverId, string smsNumber, string endDate) campaign, MessageCounter messageCounter)
    {
        try
        {
            Console.WriteLine($"Campaign {campaign.campaignId} Processing Started.");
            Console.WriteLine($"Processing contact {campaign.firstName} {campaign.lastName} with phone number: {campaign.phoneNo}");

            if (campaign.channelType == "WhatsApp")
            {
                Console.WriteLine($"Sending WhatsApp message to {campaign.phoneNo}");

                // Call WhatsApp_contacts_dialAsync
                await WhatsApp_contacts_dialAsync(
                    _dbHandler,
                    campaign.listId,
                    campaign.campaignId,
                    campaign.campaignName,
                    campaign.channelType,
                    campaign.phoneNo,
                    campaign.startDate,
                    campaign.endDate,
                    campaign.templateName,
                    messageCounter
                );
            }
            if (campaign.channelType == "SMS")
            {
                Console.WriteLine($"Sending SMS message to {campaign.phoneNo}");

                // Call WhatsApp_contacts_dialAsync
                await SMS_contacts_dialAsync(
                    _dbHandler,
                    campaign.listId,
                    campaign.campaignId,
                    campaign.campaignName,
                    campaign.channelType,
                    campaign.phoneNo,
                    campaign.startDate,
                    campaign.endDate,
                    campaign.templateName,
                    campaign.serverId,
                    campaign.smsNumber,
                    messageCounter
                );
            }
            else
            {
                Console.WriteLine("Unknown channel type.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing campaign {campaign.campaignId}: {ex.Message}");
        }
    }

    public static string ExtractBodyComponent(string inputJson, string templateId)
    {
        JArray originalJson = JArray.Parse(inputJson);
        var transformedJson = new JArray();

        foreach (var item in originalJson)
        {
            string type = item.Value<string>("type")?.ToLower();

            if (type == "header")
            {
                string format = item.Value<string>("format").ToLower();
                Console.WriteLine("Format : " + format);

                if (format == "text")
                {
                    // Skip adding headerObj if the format is "text"
                    continue;
                }

                JObject parameterObj = null;

                if (format == "document")
                {
                    parameterObj = new JObject
                    {
                        ["type"] = "document",
                        ["document"] = new JObject
                        {
                            ["link"] = "" // Add your document link here
                        }
                    };
                }
                else if (format == "image")
                {
                    parameterObj = new JObject
                    {
                        ["type"] = "image",
                        ["image"] = new JObject
                        {
                            ["link"] = $"https://travelad.agnointel.ai/AdvertiserAccount/api/GetFile?templateId={templateId}" // Provide the image link
                        }
                    };
                }
                else if (format == "video")
                {
                    parameterObj = new JObject
                    {
                        ["type"] = "video",
                        ["video"] = new JObject
                        {
                            ["link"] = "" // Provide the video link
                        }
                    };
                }
                else
                {
                    throw new ArgumentException("Unsupported format: " + format);
                }

                var headerObj = new JObject
                {
                    ["type"] = "header",
                    ["parameters"] = new JArray
        {
            parameterObj
        }
                };

                transformedJson.Add(headerObj);
            }


            if (type == "body")
            {
                string text = item.Value<string>("text");

                // Check for placeholders like {{1}} or {{2}}
                if (!string.IsNullOrEmpty(text) && Regex.IsMatch(text, @"\{\{\d+\}\}"))
                {
                    // Extract placeholders such as {{1}}, {{2}}, etc.
                    var matches = Regex.Matches(text, @"\{\{\d+\}\}");

                    var parameters = new JArray();
                    foreach (Match match in matches)
                    {
                        parameters.Add(new JObject
                        {
                            ["type"] = "text",
                            ["text"] = match.Value
                        });
                    }

                    var bodyObj = new JObject
                    {
                        ["type"] = "body",
                        ["parameters"] = parameters
                    };

                    transformedJson.Add(bodyObj);
                }
            }
        }

        string jsonOutput = JsonConvert.SerializeObject(transformedJson);
        //string finalOutput = jsonOutput.Replace("\"", "\\\"");
        // Console.WriteLine(jsonOutput);
        // Console.WriteLine(finalOutput);
        return jsonOutput;
    }



    public static string ConvertJson(string inputJson, string name, string imageLink, string pdfLink, string templateId)
    {
        JArray originalJson = JArray.Parse(inputJson);
        var transformedJson = new JArray();

        foreach (var item in originalJson)
        {
            string type = item.Value<string>("type")?.ToLower();

            if (type == "header")
            {
                string format = item.Value<string>("format").ToLower();
                Console.WriteLine("Format : " + format);

                if (format == "text")
                {
                    // Skip adding headerObj if the format is "text"
                    continue;
                }

                JObject parameterObj = null;

                if (format == "document")
                {
                    parameterObj = new JObject
                    {
                        ["type"] = "document",
                        ["document"] = new JObject
                        {
                            ["link"] = $"https://travelad.agnointel.ai/AdvertiserAccount/api/GetFile?templateId={templateId}" // Add your document link here
                        }
                    };
                }
                else if (format == "image")
                {
                    parameterObj = new JObject
                    {
                        ["type"] = "image",
                        ["image"] = new JObject
                        {
                            ["link"] = $"https://travelad.agnointel.ai/AdvertiserAccount/api/GetFile?templateId={templateId}" // Provide the image link
                        }
                    };
                }
                else if (format == "video")
                {
                    parameterObj = new JObject
                    {
                        ["type"] = "video",
                        ["video"] = new JObject
                        {
                            ["link"] = $"https://travelad.agnointel.ai/AdvertiserAccount/api/GetFile?templateId={templateId}" // Provide the video link
                        }
                    };
                }
                else
                {
                    throw new ArgumentException("Unsupported format: " + format);
                }

                var headerObj = new JObject
                {
                    ["type"] = "header",
                    ["parameters"] = new JArray
        {
            parameterObj
        }
                };

                transformedJson.Add(headerObj);
            }
            if (type == "body")
            {
                // Ensure the text property is properly accessed
                string text = item.SelectToken("parameters[0].text")?.ToString() ?? item.Value<string>("text");

                if (!string.IsNullOrEmpty(text))
                {
                    // Check for placeholders like {{1}}, {{2}}, etc.
                    var matches = Regex.Matches(text, @"\{\{\d+\}\}");

                    if (matches.Count > 0)
                    {
                        var parameters = new JArray();

                        // Extract placeholders such as {{1}}, {{2}}, etc.
                        foreach (Match match in matches)
                        {
                            parameters.Add(new JObject
                            {
                                ["type"] = "text",
                                ["text"] = match.Value
                            });
                        }

                        // Create the transformed JSON object
                        var bodyObj = new JObject
                        {
                            ["type"] = "body",
                            ["parameters"] = parameters
                        };

                        // Parse and transform the original JSON
                        JArray originalJson2 = new JArray { bodyObj }; // Assuming bodyObj is the JSON object you want to process

                        foreach (var originalItem in originalJson2)
                        {
                            string itemType = originalItem.Value<string>("type")?.ToLower();

                            if (itemType == "body")
                            {
                                // Access the text property of the first parameter
                                string innerText = originalItem.SelectToken("parameters[0].text")?.ToString();

                                if (!string.IsNullOrEmpty(innerText))
                                {
                                    // Replace placeholders
                                    innerText = ReplacePlaceholders(innerText, name, imageLink, pdfLink);
                                }

                                var transformedBodyObj = new JObject
                                {
                                    ["type"] = "body",
                                    ["parameters"] = new JArray
                        {
                            new JObject
                            {
                                ["type"] = "text",
                                ["text"] = innerText ?? "" // Assign an empty string if text is null
                            }
                        }
                                };

                                transformedJson.Add(transformedBodyObj);
                            }
                        }
                    }
                }
            }


            else if (type == "buttons")
            {
                var buttonType = item["buttons"][0]["type"].ToString(); // Get button type

                if (buttonType == "COPY_CODE") // Check if type is COPY_CODE
                {
                    var buttonObj = new JObject
                    {
                        ["type"] = "button",
                        ["sub_type"] = "copy_code", // Set sub_type as copy_code
                        ["index"] = "0", // Set index as 0
                        ["parameters"] = new JArray
            {
                new JObject
                {
                    ["type"] = "coupon_code", // Set type as coupon_code
                    ["coupon_code"] = item["buttons"][0]["example"][0].ToString() // Use example value as coupon code
                }
            }
                    };
                    transformedJson.Add(buttonObj);
                }
                else // Handle other button types
                {
                    var buttonObj = new JObject
                    {
                        ["type"] = "button",
                        ["sub_type"] = "URL",
                        ["index"] = "1",
                        ["parameters"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = item["buttons"][0]["url"].ToString()
                }
            }
                    };
                    transformedJson.Add(buttonObj);
                }
            }



        }
        return JsonConvert.SerializeObject(transformedJson, Formatting.None);
    }

    private static string ReplacePlaceholders(string text, string name, string imageLink, string pdfLink)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace("{{1}}", name ?? "")
            .Replace("{{2}}", imageLink ?? "")
            .Replace("{{3}}", pdfLink ?? "");
    }

    private static async Task WhatsApp_contacts_dialAsync([FromServices] IDbHandler dbHandler,
       string listid,
       string campaignid,
       string campaign_name,
       string channelType,
       string phoneNo,
       string start_time,
       string end_time,
       string templateName,
       MessageCounter messageCounter)
    {
        int msg_count = 0;
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        IConfiguration config = builder.Build();
        DataTable dtmain1 = new DataTable();
        // Fetch template details
        dtmain1 = dbHandler.ExecuteDataTable("select * from dbo.ta_meta_templates where template_name='" + templateName + "'");
        string template_language = dtmain1.Rows[0]["language"].ToString();
        string components = dtmain1.Rows[0]["components"].ToString();
        string templateId = dtmain1.Rows[0]["template_id"].ToString();



        // Fetch campaign contacts
        DataTable dtmain = new DataTable();
        dtmain = dbHandler.ExecuteDataTable("SELECT list_id,contact_id,firstname,lastname,phoneno,location,filename1,created_date,createdby,campaign_id,status FROM dbo.ta_campaign_contacts where campaign_id=" + campaignid + " and status='Open'");
        var dt = dbHandler.ExecuteDataTable("EXEC GetDistinctWorkspaceIDs @ListID=" + listid);

        // Check if the DataTable contains any rows (i.e., there is at least one workspace_id)
        if (dt.Rows.Count > 0)
        {
            // Retrieve the workspace_id from the first row and first column
            int workspaceid = Convert.ToInt32(dt.Rows[0]["workspace_id"]);

            // Now you can use workspaceid as needed
            Console.WriteLine("Workspace ID: " + workspaceid);

            Dialler dialler = new Dialler(dbHandler);



            var whatsappDetails = dialler.GetWhatsappAccountDetailsByWId(workspaceid);
            if (whatsappDetails == null)
            {
                Console.WriteLine("No WhatsApp account details found for workspace ID: " + workspaceid);
                return;
            }


            // Loop through all campaign contacts
            for (int i = 0; i < dtmain.Rows.Count; i++)
            {
                string callernumber = dtmain.Rows[i]["phoneno"].ToString();
                string contact_id = dtmain.Rows[i]["contact_id"].ToString();
                string firstname = dtmain.Rows[i]["firstname"].ToString();
                string lastname = dtmain.Rows[i]["lastname"].ToString();
                string location = dtmain.Rows[i]["location"].ToString();
                string extractedBodyComponent = ExtractBodyComponent(components, templateId);

                string data1 = ConvertJson(components, firstname, lastname, location, templateId);
                var msgText = Encoding.ASCII.GetBytes("{ \"messaging_product\": \"whatsapp\", \"recipient_type\": \"individual\", \"to\": \"" + callernumber + "\", \"type\": \"template\", \"template\": { \"name\": \"" + templateName + "\", \"language\": { \"code\": \"" + template_language + "\" }, \"components\": " + data1 + " } }");

                if (whatsappDetails == null)
                {
                    return;
                }
                // Create HTTP request to send message
                var request = (HttpWebRequest)WebRequest.Create(config["facebookApiUrl"].TrimEnd('/') + "/" + whatsappDetails.PhoneId + "/messages");
                Console.WriteLine("URL : " + (config["facebookApiUrl"].TrimEnd('/') + "/" + whatsappDetails.PhoneId + "/messages"));
                request.Method = "POST";
                request.Headers.Add("Authorization", "Bearer " + whatsappDetails.AccessToken);
                request.ContentType = "application/json";
                request.ContentLength = msgText.Length;

                try
                {
                    // Write data to the request stream
                    await using (var stream = request.GetRequestStream())
                    {
                        stream.Write(msgText, 0, msgText.Length);
                    }
                    Console.WriteLine(Encoding.ASCII.GetString(msgText));
                    // Get the response
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    Console.WriteLine("Response : " + response);

                    messageCounter.Increment(campaignid);
                    Console.WriteLine("Message Sent");
                    msg_count++;
                    // Call the stored procedure to update contact status and payment details
                    try
                    {
                        var result = dbHandler.ExecuteScalar("EXEC UpdateCampaignContactAndPaymentDetails @campaign_id, @contact_id, @phoneno", new Dictionary<string, object>
                    {
                        { "@campaign_id", campaignid },
                        { "@contact_id", contact_id },
                        { "@phoneno", callernumber }
                    });

                        if (result != null)
                        {
                            Console.WriteLine("Campaign contact and payment details updated successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Error in updating campaign contact and payment details.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error calling stored procedure: " + ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }

        }

    }

    private static async Task SMS_contacts_dialAsync([FromServices] IDbHandler dbHandler,
        string listid,
        string campaignid,
        string campaign_name,
        string channelType,
        string phoneNo,
        string start_time,
        string end_time,
        string templateName,
        int serverId,
        string smsNumber,
        MessageCounter messageCounter)
    {
        int msg_count = 0;
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        IConfiguration config = builder.Build();

        // Fetch template details
        DataTable dtmain1 = dbHandler.ExecuteDataTable("SELECT * FROM dbo.ta_meta_templates WHERE template_name='" + templateName + "'");
        string template_language = dtmain1.Rows[0]["language"].ToString();
        string components = dtmain1.Rows[0]["components"].ToString();
        string templateId = dtmain1.Rows[0]["template_id"].ToString();

        List<SMSComponent> componentList = JsonConvert.DeserializeObject<List<SMSComponent>>(components);

        // Access the 'text' field of the first component
        if (componentList != null && componentList.Count > 0)
        {
            string text = componentList[0].text;
            Console.WriteLine($"Extracted text: {text}");
        }
        else
        {
            Console.WriteLine("No components found in the JSON.");
        }

        // Fetch campaign contacts
        DataTable dtmain = dbHandler.ExecuteDataTable("SELECT list_id, contact_id, firstname, lastname, phoneno, location, filename1, created_date, createdby, campaign_id, status FROM dbo.ta_campaign_contacts WHERE campaign_id=" + campaignid + " AND status='Open'");
        var dt = dbHandler.ExecuteDataTable("EXEC GetDistinctWorkspaceIDs @ListID=" + listid);

        // Check if the DataTable contains any rows (i.e., there is at least one workspace_id)
        if (dt.Rows.Count > 0)
        {
            // Retrieve the workspace_id from the first row and first column
            int workspaceid = Convert.ToInt32(dt.Rows[0]["workspace_id"]);
            Console.WriteLine("Workspace ID: " + workspaceid);

            Dialler dialler = new Dialler(dbHandler);

            // Ensure serverId is not null
            if (serverId == 0)
            {
                Console.WriteLine("No WhatsApp account details found for workspace ID: " + workspaceid);
                return;
            }

            // Loop through all campaign contacts
            for (int i = 0; i < dtmain.Rows.Count; i++)
            {
                string callernumber = dtmain.Rows[i]["phoneno"].ToString();
                string contact_id = dtmain.Rows[i]["contact_id"].ToString();
                string firstname = dtmain.Rows[i]["firstname"].ToString();
                string lastname = dtmain.Rows[i]["lastname"].ToString();
                string location = dtmain.Rows[i]["location"].ToString();

                // Construct the URL
                var otherProjectBaseUrl = config["OtherProject:BaseUrl"];
                var url = $"{otherProjectBaseUrl}/Message/api/send";

                try
                {
                    // Prepare the message payload for sending the request
                    var messagePayload = new
                    {
                        Sender = smsNumber,  // Replace with the actual sender ID
                        Receiver = callernumber,
                        Message = componentList[0].text,  // Extracted text from the components JSON
                        ChannelId = serverId
                    };

                    // Serialize the payload into JSON format
                    var jsonMessage = JsonConvert.SerializeObject(messagePayload);

                    // Create the HTTP WebRequest
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";  // Set method to POST
                    request.ContentType = "application/json";  // Content type is JSON
                    request.ContentLength = jsonMessage.Length;

                    // Write the JSON message to the request stream
                    using (var stream = request.GetRequestStream())
                    {
                        byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
                        stream.Write(data, 0, data.Length);
                    }

                    // Get the response from the API
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        // Log the response status code
                        Console.WriteLine("Response Status: " + response.StatusCode);

                        // Read and log the response content
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            string responseContent = reader.ReadToEnd();
                            Console.WriteLine("Response Content: " + responseContent);
                        }
                    }

                    // Increment message counter and log the result
                    messageCounter.Increment(campaignid);
                    Console.WriteLine("Message Sent");
                    msg_count++;

                    // Call the stored procedure to update contact status and payment details
                    var result = dbHandler.ExecuteScalar("EXEC UpdateCampaignContactAndPaymentDetails @campaign_id, @contact_id, @phoneno", new Dictionary<string, object>
                {
                    { "@campaign_id", campaignid },
                    { "@contact_id", contact_id },
                    { "@phoneno", callernumber }
                });

                    if (result != null)
                    {
                        Console.WriteLine("Campaign contact and payment details updated successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Error in updating campaign contact and payment details.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
        else
        {
            Console.WriteLine("No workspace found for the given ListID.");
        }
    }

}


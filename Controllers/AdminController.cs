﻿using DBAccess;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using TravelAd_Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using static TravelAd_Api.Models.AdminModel;

namespace TravelAd_Api.Controllers
{
    [Route("[controller]/api/[action]")]
    [ApiController]
    [EnableCors("AllowSpecificOrigin")]
    public class AdminController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly IDbHandler _dbHandler;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IConfiguration configuration, IDbHandler dbHandler, ILogger<AdminController> logger)
        {
            _configuration = configuration;
            _dbHandler = dbHandler;
            _logger = logger;
        }


        private string DtToJSON(DataTable table)
        {
            string jsonString = JsonConvert.SerializeObject(table);
            return jsonString;
        }

        //__________________________________Admin Module----------------------------------------//


        [HttpGet]
        public IActionResult GetAccountList([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string getAccountList = "GetAccountDetails";

                _logger.LogInformation("Executing stored procedure: {ProcedureName}", getAccountList);

                DataTable accountList = dbHandler.ExecuteDataTable(getAccountList);

                if (accountList == null || accountList.Rows.Count == 0)
                {
                    _logger.LogInformation("No accounts found");

                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No accounts found"
                    });
                }

                var accountListData = accountList.AsEnumerable().Select(row => new
                {
                    Name = row.Field<string>("Name"),
                    Email = row.Field<string>("Email"),
                    Type = row.Field<string>("Type"),
                    CreatedAt = row.Field<DateTime>("CreatedAt"),
                    UpdatedAt = row.Field<DateTime>("UpdatedAt"),
                    AccountId = row.Field<int>("Account_id")
                }).ToList();

                _logger.LogInformation("Accounts retrieved successfully");
                _logger.LogInformation("Requests: {@AccountListData}", accountListData);


                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Accounts retrieved successfully",
                    accountList = accountListData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the account list: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the account list: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult GetWorkspaceDetailsByPersonalId([FromServices] IDbHandler dbHandler, int accountId, string mode)
        {
            try
            {
                string procedure = "GetWorkspaceDetailsByAccountId";
                _logger.LogInformation("Executing stored procedure: {procedure}", procedure);
                var parameters = new Dictionary<string, object>
              {
                  { "@AccountId", accountId },
                  { "@Mode",mode}
              };

                _logger.LogInformation("Stored procedure parameters: {Parameters}", parameters);

                DataTable UsersWorkspaceList = dbHandler.ExecuteDataTable(procedure, parameters, CommandType.StoredProcedure);
                if (UsersWorkspaceList == null || UsersWorkspaceList.Rows.Count == 0)
                {
                    _logger.LogWarning("No Users found for Workspace");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No details found for user"
                    });
                }

                var UsersWorkspaceListData = UsersWorkspaceList.AsEnumerable().Select(row => new
                {
                    workspaceId = row.Field<int>("workspace_id"),
                    workspaceName = row.Field<string>("workspace_name"),
                    roleId = row.Field<int>("role_id"),
                    accountId = row.Field<int>("account_id"),

                }).ToList();

                _logger.LogInformation("Users workspace details retrieved successfully , Response: {Response}", UsersWorkspaceListData);
                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Users workspace details retrieved successfully",
                    WorkspaceData = UsersWorkspaceListData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the users workspace details: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the users workspace details: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public IActionResult GetAudienceList([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string procedure = "GetAudienceList";
                _logger.LogInformation("Executing stored procedure: {ProcedureName}", procedure);

                DataTable audienceList = dbHandler.ExecuteDataTable(procedure);

                if (audienceList.Rows.Count == 0)
                {
                    _logger.LogInformation("No Telco details found");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No Telco details found"
                    });
                }

                var TelcoData = audienceList.AsEnumerable().Select(row => new
                {
                    telco_id = row.Field<int>("telco_id"),
                    telco_name = row.Field<string>("telco_name"),
                    status = row.Field<string>("status"),
                    recipients = row.Field<int>("recipients"),
                    updatedAt = row.Field<DateTime?>("update_date") 
                }).ToList();

                _logger.LogInformation("Request received: {@TelcoData}", TelcoData);
                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Telco details retrieved successfully",
                    audienceList = TelcoData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving telco details: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving telco details: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult GetUserCount([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string procedure = "GetTotalUserCount";
                _logger.LogInformation("Executing stored procedure: {ProcedureName}", procedure);

                DataTable userCountResult = dbHandler.ExecuteDataTable(procedure);

                if (userCountResult.Rows.Count == 0)
                {
                    _logger.LogWarning("No user data found");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No user data found"
                    });
                }

                int totalUserCount = userCountResult.Rows[0].Field<int>("TotalUserCount");
                _logger.LogInformation("Total user count retrieved successfully,totalUserCount: {Parameters}", totalUserCount);
                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Total user count retrieved successfully",
                    totalUserCount = totalUserCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the user count: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the user count: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult GetUserCountByDateRange([FromServices] IDbHandler dbHandler, DateTime from_date, DateTime to_date)
        {
            try
            {
                string procedure = "GetTotalUserCountByDateRange";
                _logger.LogInformation("Executing stored procedure: {ProcedureName}", procedure);

                DateTime fromDate = from_date.Date;
                DateTime toDate = to_date.Date;

                // Prepare parameters for the stored procedure
                var parameters = new Dictionary<string, object>
                {
                    { "@from_date", fromDate },
                    { "@to_date", toDate }
                };

                DataTable userCountResult = dbHandler.ExecuteDataTable(procedure, parameters, CommandType.StoredProcedure);

                if (userCountResult.Rows.Count == 0)
                {
                    _logger.LogWarning("No user data found");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No user data found"
                    });
                }

                int totalUserCount = userCountResult.Rows[0].Field<int>("TotalUserCount");
                _logger.LogInformation("Total user count retrieved successfully,totalUserCount: {Parameters}", totalUserCount);
                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Total user count retrieved successfully",
                    totalUserCount = totalUserCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the user count: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the user count: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public IActionResult GetWorkspaceCount([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string procedure = "GetTotalWorkspaceCount";
                _logger.LogInformation("Executing stored procedure: {ProcedureName}", procedure);

                DataTable CountResult = dbHandler.ExecuteDataTable(procedure);

                if (CountResult.Rows.Count == 0)
                {
                    _logger.LogWarning("No user data found");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No user data found"
                    });
                }

                int totalCount = CountResult.Rows[0].Field<int>("TotalUserCount");
                _logger.LogInformation("Total user count retrieved successfully, totalCount: {totalCount} ", totalCount);
                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Total user count retrieved successfully",
                    totalWorkspaceCount = totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the user count: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the user count: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult GetWorkspaceCountByDateRange([FromServices] IDbHandler dbHandler, DateTime from_date, DateTime to_date)
        {
            try
            {
                string procedure = "GetTotalWorkspaceCountByDateRange";
                _logger.LogInformation("Executing stored procedure: {ProcedureName}", procedure);

                DateTime fromDate = from_date.Date;
                DateTime toDate = to_date.Date;

                // Prepare parameters for the stored procedure
                var parameters = new Dictionary<string, object>
                {
                    { "@from_date", fromDate },
                    { "@to_date", toDate }
                };

                DataTable CountResult = dbHandler.ExecuteDataTable(procedure, parameters, CommandType.StoredProcedure);

                if (CountResult.Rows.Count == 0)
                {
                    _logger.LogWarning("No user data found");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No user data found"
                    });
                }

                int totalCount = CountResult.Rows[0].Field<int>("TotalUserCount");
                _logger.LogInformation("Total user count retrieved successfully, totalCount: {totalCount} ", totalCount);
                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Total user count retrieved successfully",
                    totalWorkspaceCount = totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the user count: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the user count: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public IActionResult GetCampaignsCount([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string procedure = "GetTotalCampaignsCount";
                _logger.LogInformation("Executing stored procedure: {ProcedureName}", procedure);

                DataTable CountResult = dbHandler.ExecuteDataTable(procedure);

                if (CountResult.Rows.Count == 0)
                {
                    _logger.LogWarning("No user data found");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No user data found"
                    });
                }

                int totalCount = CountResult.Rows[0].Field<int>("TotalUserCount");
                _logger.LogInformation("Total user count retrieved successfully, totalCount: {totalCount}", totalCount);

                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Total user count retrieved successfully",
                    totalCampaignsCount = totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the user count: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the user count: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult GetCampaignsCountByDateRange([FromServices] IDbHandler dbHandler, DateTime from_date, DateTime to_date)
        {
            try
            {
                string procedure = "GetTotalCampaignsCountByDateRange";
                _logger.LogInformation("Executing stored procedure: {ProcedureName}", procedure);

                DateTime fromDate = from_date.Date;
                DateTime toDate = to_date.Date;

                // Prepare parameters for the stored procedure
                var parameters = new Dictionary<string, object>
                {
                    { "@from_date", fromDate },
                    { "@to_date", toDate }
                };

                DataTable CountResult = dbHandler.ExecuteDataTable(procedure, parameters, CommandType.StoredProcedure);

                if (CountResult.Rows.Count == 0)
                {
                    _logger.LogWarning("No user data found");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No user data found"
                    });
                }

                int totalCount = CountResult.Rows[0].Field<int>("TotalUserCount");
                _logger.LogInformation("Total user count retrieved successfully, totalCount: {totalCount}", totalCount);

                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Total user count retrieved successfully",
                    totalCampaignsCount = totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the user count: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the user count: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public IActionResult GetAdvertiserinfo([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string getAccountList = "GetAdvertiserinfo";
                DataTable accountList = dbHandler.ExecuteDataTable(getAccountList);
                if (accountList == null || accountList.Rows.Count == 0)
                {
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No accounts found"
                    });
                }
                var accountListData = accountList.AsEnumerable().Select(row => new
                {
                    Name = row.Field<string>("workspace_name"),
                    country = row.Field<int>("billing_country"),
                    Industry = row.Field<string>("industry_name"),
                    Type = row.Field<string>("workspace_type"),
                    workspaceid = row.Field<int>("workspace_info_id"),
                    createdate = row.Field<DateTime>("created_date"),
                    updateddate = row.Field<DateTime>("updated_date")
                }).ToList();
                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Accounts retrieved successfully",
                    accountList = accountListData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the account list: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult deleteworkspce([FromServices] IDbHandler dbHandler, int workspaceid)
        {
            try
            {
                string deleteworkspaces = "deleteworkspce";

                var Iparameters = new Dictionary<string, object>
         {
          { "@workspaceinfoid", workspaceid }
         };
                DataTable deleteworkspaceids = dbHandler.ExecuteDataTable(deleteworkspaces, Iparameters, CommandType.StoredProcedure);
                Console.WriteLine(deleteworkspaceids);
                if (deleteworkspaceids.Rows.Count == 0)
                {
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "Campaign Not found"
                    });
                }

                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Campaign retrieved successfully",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the campaign list by id: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult GetCampaignListAdmin([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string procedure = "GetCampainListAdmin"; // Stored procedure name
                _logger.LogInformation($"Executing stored procedure: {procedure}");

                // Execute the stored procedure without parameters
                DataTable campaignList = dbHandler.ExecuteDataTable(procedure, null, CommandType.StoredProcedure);

                if (campaignList == null || campaignList.Rows.Count == 0)
                {
                    _logger.LogWarning("No campaigns found.");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No Campaigns found",
                        CampaignCount = 0 // Return zero count if no rows found
                    });
                }

                // Transform the DataTable to a list of objects
                var campaignListData = campaignList.AsEnumerable().Select(row => new
                {
                    workspace_id = row.Field<int>("workspace_id"),
                    workspace_name = row.Field<string>("workspace_name"),
                    campaign_id = row.Field<int>("campaign_id"),
                    campaign_name = row.Field<string>("campaign_name"),
                    channel_type = row.Field<string>("channel_type"),
                    campaign_budget = row.Field<string>("campaign_budget"),
                    start_date_time = row.Field<DateTime>("start_date_time"),
                    end_date_time = row.Field<DateTime>("end_date_time"),
                    status = row.Field<string>("status"),
                    sent = row.Field<int?>("sent")
                }).ToList();

                _logger.LogInformation("Campaigns retrieved successfully. Data: {@CampaignListData}", campaignListData);

                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Campaigns retrieved successfully",
                    CampaignCount = campaignList.Rows.Count, // Add the row count here
                    CampaignList = campaignListData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the campaign list.");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the campaign list: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult GetCampaignListAdminbyDateRange(DateTime from_date, DateTime to_date, [FromServices] IDbHandler dbHandler)
        {
            try
            {

                string procedure = "GetCampainListAdminbyDateRange";
                _logger.LogInformation($"Executing stored procedure: {procedure}");

                DateTime fromDate = from_date.Date;
                DateTime toDate = to_date.Date;

                // Prepare parameters for the stored procedure
                var parameters = new Dictionary<string, object>
                {

                    { "@from_date", fromDate },
                    { "@to_date", toDate }
                };

                // Execute the stored procedure
                DataTable campaignList = dbHandler.ExecuteDataTable(procedure, parameters, CommandType.StoredProcedure);

                if (campaignList == null || campaignList.Rows.Count == 0)
                {
                    _logger.LogWarning("No campaigns found");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No Campaigns found ",
                        CampaignCount = 0 // Return zero count if no rows found
                    });
                }

                // Transform the DataTable to a list of objects
                var campaignListData = campaignList.AsEnumerable().Select(row => new
                {
                    campaign_id = row.Field<int>("campaign_id"),
                    campaign_name = row.Field<string>("campaign_name"),
                    channel_type = row.Field<string>("channel_type"),
                    campaign_budget = row.Field<string>("campaign_budget"),
                    start_date_time = row.Field<DateTime>("start_date_time"),
                    end_date_time = row.Field<DateTime>("end_date_time"),
                    status = row.Field<string>("status"),
                    sent = row.Field<int?>("sent")
                }).ToList();

                _logger.LogInformation("Campaigns retrieved successfully. Data: {@CampaignListData}", campaignListData);

                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Campaigns retrieved successfully",
                    CampaignCount = campaignList.Rows.Count, // Add the row count here
                    CampaignList = campaignListData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the campaign list.");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the campaign list: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public IActionResult GetInReviewCount([FromServices] IDbHandler dbHandler, string Mode, int? WorkspaceId)
        {
            try
            {
                // Validate parameters based on mode
                if (Mode.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    if (WorkspaceId.HasValue)
                    {
                        return BadRequest(new
                        {
                            Status = "Error",
                            Status_Description = "For admin mode, WorkspaceId must be null."
                        });
                    }
                }
                else if (Mode.Equals("operator", StringComparison.OrdinalIgnoreCase))
                {
                    if (!WorkspaceId.HasValue)
                    {
                        return BadRequest(new
                        {
                            Status = "Error",
                            Status_Description = "WorkspaceId is required for operator mode."
                        });
                    }
                }
                else
                {
                    return BadRequest(new
                    {
                        Status = "Error",
                        Status_Description = "Invalid mode. Use 'admin' or 'operator'."
                    });
                }

                string procedure = "GetInReviewCount";
                _logger.LogInformation($"Executing stored procedure: {procedure}");

                // Build the parameters dictionary.
                // For admin mode, pass DBNull.Value for WorkspaceId.
                var parameters = new Dictionary<string, object>
        {
            {"@Mode", Mode },
            { "@WorkspaceId", Mode.Equals("admin", StringComparison.OrdinalIgnoreCase) ? (object)DBNull.Value : WorkspaceId.Value }
        };

                // Execute the stored procedure.
                int Count = (int)dbHandler.ExecuteScalar(procedure, parameters, CommandType.StoredProcedure);

                if (Count <= 0)
                {
                    _logger.LogWarning("No campaigns found.");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No Campaigns found",
                        CampaignCount = 0
                    });
                }

                _logger.LogInformation("Campaigns retrieved successfully. Data: {@CampaignListData}", Count);

                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Campaigns retrieved successfully",
                    CampaignCount = Count,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the campaign list.");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the campaign list: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public IActionResult GetAdminDashboardChartDetails([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string getChartDetailsList = "GetAdminDashboardChartDetails";

                _logger.LogInformation("Executing stored procedure: {ProcedureName}", getChartDetailsList);

                DataTable DataList = dbHandler.ExecuteDataTable(getChartDetailsList);

                if (DataList == null || DataList.Rows.Count == 0)
                {
                    _logger.LogInformation("No details for chart found");

                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No details for chart found"
                    });
                }

                var ChartListData = DataList.AsEnumerable().Select(row => new
                {
                    date = row.Field<DateTime>("date"),
                    Email = row.Field<int?>("Email"),
                    SMS = row.Field<int?>("SMS"),
                    PushNotifications = row.Field<int?>("PushNotification"),
                    RCSmessages = row.Field<int?>("RCSMessages"),
                    WhatsApp = row.Field<int?>("WhatsApp")
                }).ToList();

                _logger.LogInformation("Chart Details retrieved successfully");
                _logger.LogInformation("Requests: {@ChartListData}", ChartListData);


                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Accounts retrieved successfully",
                    ChartData = ChartListData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the Admin Dashboard chart details: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the Admin Dashboard chart details: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public IActionResult GetAdminDashboardChartDetailsByDateRange([FromServices] IDbHandler dbHandler, DateTime from_date, DateTime to_date)
        {
            try
            {
                string getChartDetailsList = "GetAdminDashboardChartDetailsByDateRange";

                _logger.LogInformation("Executing stored procedure: {ProcedureName}", getChartDetailsList);

                DateTime fromDate = from_date.Date;
                DateTime toDate = to_date.Date;

                var parameters = new Dictionary<string, object>
        {
            { "@from_date", fromDate },
            { "@to_date", toDate }
        };

                DataTable DataList = dbHandler.ExecuteDataTable(getChartDetailsList, parameters, CommandType.StoredProcedure);

                //if (DataList == null || DataList.Rows.Count == 0)
                //{
                //    _logger.LogInformation("No details for chart found");

                //    return Ok(new
                //    {
                //        Status = "Failure",
                //        Status_Description = "No details for chart found"
                //    });
                //}

                var ChartListData = DataList != null && DataList.Rows.Count > 0
                ? DataList.AsEnumerable().Select(row => new Dictionary<string, object>
                {
            { "date", row.Field<DateTime>("date") }, // Keep date unchanged
            { "email", row.Field<int?>("Email") ?? 0 }, // Convert Email → email (lowercase)
            { "sms", row.Field<int?>("SMS") ?? 0 }, // Convert SMS → sms
            { "pushNotifications", row.Field<int?>("PushNotification") ?? 0 }, // Convert PushNotifications → pushNotifications
            { "rcSmessages", row.Field<int?>("RCSMessages") ?? 0 }, // Convert RCSmessages → rcSmessages
            { "whatsApp", row.Field<int?>("WhatsApp") ?? 0 } // Convert WhatsApp → whatsApp
                }).ToList()
                : new List<Dictionary<string, object>> {
            new Dictionary<string, object> {
                { "date", DateTime.Now },
                { "email", 0 },
                { "sms", 0 },
                { "pushNotifications", 0 },
                { "rcSmessages", 0 },
                { "whatsApp", 0 }
            }
                };


                _logger.LogInformation("Chart Details retrieved successfully");
                _logger.LogInformation("Requests: {@ChartListData}", ChartListData);


                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Accounts retrieved successfully",
                    ChartData = ChartListData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the Admin Dashboard chart details: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the Admin Dashboard chart details: {ex.Message}"
                });
            }
        }


        [HttpPut]
        public IActionResult UpdateCampaignStatus(TravelAd_Api.Models.AdminModel.UpdateCampaign uc, [FromServices] IDbHandler dbHandler)
        {
            var response = new
            {
                Status = "Error",
                Status_Description = "An error occurred."
            };

            try
            {
                string updateQuery = "UpdateCampaignApprovalStatus";
                _logger.LogInformation("Executing stored procedure: {Query}", updateQuery);

                var parameters = new Dictionary<string, object>
               {
                   {"@campaignId", uc.campaignId },
                   {"@serverId", uc.serverId },
                   {"@connectionId",uc.connectionId},
                   {"@approverType","Admin" },
                   {"@workspaceId",null },
                   {"@approvalStatus", uc.status }
               };

                // Execute the stored procedure
                int result = Convert.ToInt32(dbHandler.ExecuteScalar(updateQuery, parameters, CommandType.StoredProcedure));


                if (result > 0)
                {
                    _logger.LogInformation($"Campaign with ID {uc.campaignId} was updated successfully.");
                    response = new
                    {
                        Status = "Success",
                        Status_Description = "Campaign status updated successfully."
                    };
                }
                else
                {
                    _logger.LogWarning($"Campaign ID {uc.campaignId} not found or update failed.");
                    response = new
                    {
                        Status = "Error",
                        Status_Description = "Campaign ID not found or update failed."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred while processing the request. Error: {ex.Message}");
                response = new
                {
                    Status = "Error",
                    Status_Description = "An exception occurred while processing the request."
                };
            }

            return Ok(response);
        }


        [HttpGet]
        public IActionResult GetAdminApprovalStatus([FromServices] IDbHandler dbHandler, int CampaignId)
        {
            try
            {
                string procedure = "GetCampaignApprovalStatus"; // Stored procedure name

                _logger.LogInformation($"Executing stored procedure: {procedure}");

                var parameters = new Dictionary<string, object>
        {
            { "@WorkspaceId", null },
            { "@CampaignId", CampaignId },
            { "@mode","Admin"}
        };

                _logger.LogInformation("Stored procedure parameters: {Parameters}", parameters);

                // Execute the stored procedure
                DataTable approvalResult = dbHandler.ExecuteDataTable(procedure, parameters, CommandType.StoredProcedure);

                // If no data is returned, assume the operator is not assigned or campaign doesn't exist
                if (approvalResult == null || approvalResult.Rows.Count == 0)
                {
                    _logger.LogWarning("Operator is not assigned to this campaign or no data found.");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "Operator is not assigned to this campaign or no data found.",
                        IsApproved = false
                    });
                }

                // Extract the approval status from the first row
                string approvalStatus = approvalResult.Rows[0]["ApprovalStatus"].ToString();

                // Determine if the campaign is approved
                bool isApproved = approvalStatus == "Approved";

                _logger.LogInformation($"Operator approval status: {approvalStatus}, IsApproved: {isApproved}");

                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Approval status retrieved successfully.",
                    IsApproved = isApproved // Return true if "Approved", otherwise false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the operator approval status.");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the approval status: {ex.Message}",
                    IsApproved = false
                });
            }
        }

        [HttpGet]
        public IActionResult GetAdminsList([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string getAccountList = "GetAdminsList";

                _logger.LogInformation("Executing stored procedure: {ProcedureName}", getAccountList);

                DataTable adminList = dbHandler.ExecuteDataTable(getAccountList);

                if (adminList == null || adminList.Rows.Count == 0)
                {
                    _logger.LogInformation("No admins found");

                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No admins found"
                    });
                }

                var adminsListData = adminList.AsEnumerable().Select(row => new
                {
                    id = row.Field<int>("id"),
                    email = row.Field<string>("Email"),
                    CreatedAt = row.Field<DateTime?>("created_at"),
                    UpdatedAt = row.Field<DateTime?>("updated_at"),
                    first_name = row.Field<string>("first_name"),
                    last_name = row.Field<string>("last_name"),
                }).ToList();

                _logger.LogInformation("Admins retrieved successfully");
                _logger.LogInformation("Requests: {@AccountListData}", adminsListData);


                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Accounts retrieved successfully",
                    adminsList = adminsListData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the account list: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the account list: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public IActionResult GetOperatorContactsSummary([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string storedProcedureName = "dbo.GetOperatorContactsSummary";
                DataTable resultTable = dbHandler.ExecuteDataTable(storedProcedureName);

                if (resultTable == null || resultTable.Rows.Count == 0)
                {
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No data found"
                    });
                }

                var resultData = resultTable.AsEnumerable().Select(row => new
                {
                    WorkspaceName = row.Field<string>("workspace_name"),
                    Status = row["status"] == DBNull.Value ? "No Contacts" : row.Field<string>("status"),
                    UpdatedAt = row["updated_date"] == DBNull.Value ? (DateTime?)null : row.Field<DateTime?>("updated_date"),
                    RecipientCount = row["recipient"] == DBNull.Value ? 0 : row.Field<int>("recipient"),
                    WorkspaceId = row.Field<int>("workspace_id")
                }).ToList();

                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Data retrieved successfully",
                    AudienceList = resultData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = "An error occurred while retrieving data",
                    ErrorMessage = ex.Message
                });
            }
        }

        [HttpDelete]
        public IActionResult DeleteAdminAccess(int AdminId, [FromServices] IDbHandler dbHandler)
        {
            try
            {
                string deleteAdminAccess = "DeleteAdminAccess";

                _logger.LogInformation("Executing stored procedure: {ProcedureName}", deleteAdminAccess);
                var parameters = new Dictionary<string, object>
        {
            { "@AdminId", AdminId }
        };

                int rowsAffected = dbHandler.ExecuteNonQuery(deleteAdminAccess, parameters, CommandType.StoredProcedure);

                if (rowsAffected == 0)
                {
                    _logger.LogInformation("Admin not found or could not be deleted");
                    return Ok(new
                    {

                        Status = "Failure",
                        Status_Description = "Admin not found or could not be deleted"
                    });
                }


                _logger.LogInformation("Admin Access deleted successfully");
                return Ok(new

                {
                    Status = "Success",
                    Status_Description = "Admin Access deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while deleting the admin Access: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while deleting the admin Access: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult GetWhatsappNumbers([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string procedure = "GetWhatsappPhoneNumbers";

                var parameters = new Dictionary<string, object>
                {
                };

                _logger.LogInformation("Executing stored procedure: {ProcedureName}", procedure);

                DataTable PhoneNumberList = dbHandler.ExecuteDataTable(procedure,parameters,CommandType.StoredProcedure);

                if (PhoneNumberList == null || PhoneNumberList.Rows.Count == 0)
                {
                    _logger.LogInformation("No phone numbers found");

                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No whatsapp phone number found"
                    });
                }

                var PhoneNumberListData = PhoneNumberList.AsEnumerable().Select(row => new
                {
                    id = row.Field<int>("id"),
                    workspaceName = row.Field<string>("workspace_name"),
                    wabaId = row.Field<string>("wabaId"),
                    phoneId = row.Field<string>("phoneId"),
                    created_date = row.Field<DateTime?>("createdDate"),
                    last_updated = row.Field<DateTime?>("lastUpdated"),
                }).ToList();

                _logger.LogInformation("Phone number list data retrieved successfully");
                _logger.LogInformation("Requests: {@AccountListData}", PhoneNumberListData);


                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "phone number list retrieved successfully",
                    PhoneNumberData = PhoneNumberListData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving the account list: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the whatsapp number list: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult GetBillingCountryandSymbol([FromServices] IDbHandler dbHandler, int accountId)
        {
            try
            {
                string procedure = "GetBillingCountryandSymbol";
                _logger.LogInformation("Executing stored procedure: {procedure}", procedure);
                var parameters = new Dictionary<string, object>
                 {
                     { "@accountid", accountId }
                 };
                _logger.LogInformation("Stored procedure parameters: {Parameters}", parameters);
                DataTable billingInfo = dbHandler.ExecuteDataTable(procedure, parameters, CommandType.StoredProcedure);

                if (billingInfo == null || billingInfo.Rows.Count == 0)
                {
                    _logger.LogWarning("No billing information found for account");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No billing information found for account"
                    });
                }

                // Check if there's an error message returned by the stored procedure
                if (billingInfo.Columns.Contains("Message"))
                {
                    string errorMessage = billingInfo.Rows[0].Field<string>("Message");
                    _logger.LogWarning("Stored procedure returned an error: {ErrorMessage}", errorMessage);
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = errorMessage
                    });
                }

                var billingData = billingInfo.Rows[0];
                var billingInfoData = new
                {
                    currencyName = billingData.Field<string>("CurrencyName"),
                    currencySymbol = billingData.Field<string>("CurrencySymbol")
                };

                _logger.LogInformation("Billing information retrieved successfully, Response: {Response}", billingInfoData);
                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Billing information retrieved successfully",
                    BillingInfo = billingInfoData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while retrieving billing information: {ex.Message}");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving billing information: {ex.Message}"
                });
            }
        }


        [HttpGet]
        public IActionResult GetAllCountriesWithCurrencyName([FromServices] IDbHandler dbHandler)
        {
            try
            {
                string getCountryListWithCurrencyName = "GetAllCountriesWithCurrencyName";
                _logger.LogInformation("Executing stored procedure: {StoredProcedureName}", getCountryListWithCurrencyName);

                DataTable countryListWithCurrency = dbHandler.ExecuteDataTable(getCountryListWithCurrencyName);

                if (countryListWithCurrency == null || countryListWithCurrency.Rows.Count == 0)
                {
                    _logger.LogInformation("No countries found in the database.");
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No countries found"
                    });
                }
                var countryListWithCurrencyData = countryListWithCurrency.AsEnumerable().Select(row => new
                {
                    country_id = row.Field<int>("country_id"),
                    country_name = row.Field<string>("country_name"),
                    currency_name = row.Field<string>("currency_name")
                }).ToList();

                _logger.LogInformation("Countries retrieved successfully .{countryListWithCurrencyData}", countryListWithCurrencyData);

                return Ok(new
                {
                    Status = "Success",
                    Status_Description = "Countries retrieved successfully",
                    CountryList = countryListWithCurrencyData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the country list.");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving the country list: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult GetAmountSpentonAdCampaigns([FromServices] IDbHandler dbHandler, string currencyName)
        {
            try
            {
                string storedProcedure = "GetAmountSpentonAdCampaigns";
                _logger.LogInformation("Executing stored procedure: {ProcedureName}", storedProcedure);

                var parameters = new Dictionary<string, object>
{
    { "@currencyname", currencyName }
};

                DataTable spResult = dbHandler.ExecuteDataTable(storedProcedure, parameters, CommandType.StoredProcedure);

                if (spResult == null || spResult.Rows.Count == 0)
                {
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = "No ad campaign spending data found",
                        campaign_spending = new List<object>()
                    });
                }

                // Check if this is an error message from the stored procedure
                if (spResult.Columns.Contains("Message"))
                {
                    string message = spResult.Rows[0]["Message"].ToString();
                    return Ok(new
                    {
                        Status = "Failure",
                        Status_Description = message,
                        campaign_spending = new List<object>()
                    });
                }

                // If we get here, we should have the normal result set
                try
                {
                    var adCampaignSpending = new
                    {
                        currency_name = spResult.Rows[0]["Currency_Name"].ToString(),
                        currency_symbol = spResult.Rows[0]["Currency_Symbol"].ToString(),
                        total_amount_spent = Convert.ToDecimal(spResult.Rows[0]["Total_Amount_Spent"])
                    };

                    return Ok(new
                    {
                        Status = "Success",
                        Status_Description = "Ad campaign spending retrieved successfully",
                        campaign_spending = adCampaignSpending
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing result set. Columns available: {Columns}",
                        string.Join(", ", spResult.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving ad campaign spending data");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Status_Description = $"An error occurred while retrieving ad campaign spending data: {ex.Message}"
                });
            }
        }


    }
}

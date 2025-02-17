using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3;
using Microsoft.Data.SqlClient;
using System.Data;
using DBAccess;
using Amazon;
using CsvHelper;
using System.Globalization;
using OfficeOpenXml;
using System.Dynamic;
using Org.BouncyCastle.Crypto.Tls;

public class S3Service
{
    private readonly string _bucketName = "operator-bucket-test"; // Replace with your actual bucket name.
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;
    private readonly IDbHandler _dbHandler;

    // Constructor to inject the DbHandler and IConfiguration dependencies
    public S3Service(IConfiguration configuration, IDbHandler dbHandler)
    {
        _configuration = configuration;
        _dbHandler = dbHandler;

        //var awsCredentials = new SessionAWSCredentials("ASIA4SDNVWWK6YBJVLL5", "esOAxR7EFLq5Scojq9q/zVeN10rLF9xGWS9/UPFU", "IQoJb3JpZ2luX2VjEJ///////////wEaCXVzLWVhc3QtMSJIMEYCIQDg6dHR2CyO30Lqztd1FDo6SKWRu1jNWuRs23lTT07cFgIhAJT3ZqEHdkaflhxUIHl22OmflPpbMhsEGWdXk+5CwlJjKvQCCLj//////////wEQABoMODYzNTE4NDM4ODA1IgxwfjGKRusLapZyGWcqyAILNfv5fKkCW19dYN+sbpW7KODF7JZswtUl7HRV1qHzHh0VjHaeRjlEerGRqw68n5vO9cl0QUTbL672tdVYSAI8hIrgdIctdrmC1yNQ9gA+EGRlMLyLAjHLB1a0lMeco6/nJ8ttScM+JrIaywHQDTvIEHNv4YyFbdpj6w/UYiWOwn2Quy7iewANcGOLXXC/6sEGkx2emyPfolLbUBZU3lo2Iek/e2/Ov2svNRD9zEv5b6v5nSHz00p+0yv6uZx94Nop5Qm783k9OCMcXPmRz4UrUaaIHpf9WAaQMJvCmLevDPfruFARVvpfg1gErnj5VvHarL/sKnJYph56BAJbQ0AvOB/ZXvRmnWBnR1Mo7iKN5i1hCsZ3wFlo/TUTQlEkJDZWEMXb0mIaX2354O6t8PvdjmfLil9m9fSBStYRbEXSVGiHG7TCR8wRMOm4pr0GOqYBuz5zvUrvkb1riAcTNwg04gJNPf+Ai/yynb1/ChPqJcFgpvVYmUrmvxcePUH6ifdMFcYdeOJ1MgiKxrgViVhvbhEVk1YqdQV8OojgcHHGCaER8JvbsiVPGojjLtLYaIzyEQx2jueWgVHcjrMGVpKdeOpye9taoURMgIWQkoynTBZydnjLvgxnR/iVSPt3iIZmTJSb8rpDLHuY1OOW5YiXXu9g++Mn9g==");
        //_s3Client = new AmazonS3Client(awsCredentials, RegionEndpoint.USEast1);
        _s3Client = new AmazonS3Client(RegionEndpoint.USEast1);// Correct initialization
    }

    private IDbHandler DbHandler => _dbHandler;

    public async Task<string?> DownloadFileAsync(string fileName, string localFilePath)
    {
        try
        {
            var response = await _s3Client.GetObjectAsync(_bucketName, fileName);
            using (var fileStream = File.Create(localFilePath))
            {
                await response.ResponseStream.CopyToAsync(fileStream);
            }
            return localFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file '{fileName}': {ex.Message}");
            return null;
        }
    }

    public async Task MoveFileToBackupAsync(string fileName)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss"); // Generates a unique timestamp
        string backupKey = $"backup_contacts/{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}";

        await _s3Client.CopyObjectAsync(_bucketName, fileName, _bucketName, backupKey);
        await _s3Client.DeleteObjectAsync(_bucketName, fileName);
    }

    public async Task<string?> GetLatestFileNameAsync()
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
        };

        var response = await _s3Client.ListObjectsV2Async(request);
        var latestFile = response.S3Objects
            .Where(o => !o.Key.StartsWith("backup_contacts/"))
            .OrderByDescending(o => o.LastModified)
            .FirstOrDefault();

        return latestFile?.Key;
    }
}

public class FileDownloadAndBackupService : BackgroundService
{
    private readonly ILogger<FileDownloadAndBackupService> _logger;
    private readonly S3Service _s3Service;
    private readonly IConfiguration _configuration;  // Injected IConfiguration
    private readonly IDbHandler _dbHandler;  // Injected IDbHandler
    private readonly string _localFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "S3 Files");

    public FileDownloadAndBackupService(ILogger<FileDownloadAndBackupService> logger, S3Service s3Service, IConfiguration configuration, IDbHandler dbHandler)
    {
        _logger = logger;
        _s3Service = s3Service;
        _configuration = configuration;  // Initialize _configuration
        _dbHandler = dbHandler;  // Initialize _dbHandler
    }


    private bool ValidateRow(dynamic row, out string errorMessage)
    {
        errorMessage = string.Empty;

        // Validate eventTimeStamp (check if it is a valid DateTime)
        if (!DateTime.TryParse(row.eventTimeStamp.ToString(), out DateTime eventTimeStamp))
        {
            errorMessage = "Invalid eventTimeStamp.";
            return false;
        }

        // Validate msisdn (check if it's a valid phone number format)
        if (string.IsNullOrWhiteSpace(row.msisdn.ToString()) || row.msisdn.ToString().Length != 10) // Assuming msisdn should be a 10-digit number
        {
            errorMessage = "Invalid msisdn.";
            return false;
        }

        // Validate mcc (check if it's a valid integer)
        if (!int.TryParse(row.mcc.ToString(), out int mcc))
        {
            errorMessage = "Invalid mcc.";
            return false;
        }

        // Validate mnc (check if it's a valid integer)
        if (!int.TryParse(row.mnc.ToString(), out int mnc))
        {
            errorMessage = "Invalid mnc.";
            return false;
        }

        // Validate countryName (check if it's a non-empty string)
        if (string.IsNullOrWhiteSpace(row.countryName.ToString()))
        {
            errorMessage = "Invalid countryName.";
            return false;
        }

        return true;
    }


    private async Task InsertFailureAsync(DataRow failedRow, string errorMessage, int workspaceId)
    {
        string procedure = "InsertFailureOperatorContacts"; // Define the stored procedure name
        var parameters = new Dictionary<string, object>
    {
        { "@eventTimeStamp", failedRow["eventTimeStamp"] },
        { "@msisdn", failedRow["msisdn"] },
        { "@mcc", failedRow["mcc"] },
        { "@mnc", failedRow["mnc"] },
        { "@countryName", failedRow["countryName"] },
        { "@errorMessage", errorMessage },
        { "@fileName", failedRow["fileName"] },
        { "@workspaceId", workspaceId }  // Add workspaceId
    };

        await _dbHandler.ExecuteNonQueryAsync(procedure, parameters, CommandType.StoredProcedure);
    }


    private List<dynamic> ReadCsvFile(string filePath)
    {
        var records = new List<dynamic>();
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            records = csv.GetRecords<dynamic>().ToList();
        }
        return records;
    }

    // Helper method to read Excel files
    private List<dynamic> ReadExcelFile(string filePath)
    {
        var records = new List<dynamic>();

        // Ensure EPPlus works with .xlsx files
        using (var package = new ExcelPackage(new FileInfo(filePath)))
        {
            var worksheet = package.Workbook.Worksheets[0]; // Assuming data is in the first worksheet

            for (int row = worksheet.Dimension.Start.Row + 1; row <= worksheet.Dimension.End.Row; row++)
            {
                var rowObj = new ExpandoObject() as IDictionary<string, object>;

                for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
                {
                    rowObj.Add(worksheet.Cells[1, col].Text, worksheet.Cells[row, col].Text);
                }
                records.Add(rowObj);
            }
        }

        return records;
    }




    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("File download and backup service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var latestFileName = await _s3Service.GetLatestFileNameAsync();

                if (!string.IsNullOrEmpty(latestFileName))
                {
                    _logger.LogInformation("Latest file found: {FileName}", latestFileName);
                    var filePath = await _s3Service.DownloadFileAsync(latestFileName, _localFilePath);

                    if (filePath != null)
                    {
                        _logger.LogInformation("File downloaded successfully to {FilePath}", filePath);
                        await _s3Service.MoveFileToBackupAsync(latestFileName);
                        _logger.LogInformation("File {FileName} moved to backup successfully.", latestFileName);

                        string workspaceName = Path.GetFileNameWithoutExtension(latestFileName)
                            .Split('_')[0]
                            .Trim();

                        string procedure = "GetWorkspaceIdbyName";
                        var parameters = new Dictionary<string, object>
                        {
                            { "@WorkspaceName", workspaceName }
                        };
                        DataTable workspaceNameById = _dbHandler.ExecuteDataTable(procedure, parameters, CommandType.StoredProcedure);
                        int workspaceId = Convert.ToInt32(workspaceNameById.Rows[0]["workspace_info_id"]);

                        string currentDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        string fileNameWithDate = $"{latestFileName}_{currentDate}";// Get the current date (without time)

                        // Read the file based on extension type (CSV or Excel)
                        List<dynamic> records = new List<dynamic>();
                        var fileExtension = Path.GetExtension(latestFileName).ToLower();

                        if (fileExtension == ".csv")
                        {
                            records = ReadCsvFile(filePath); // Read CSV
                        }
                        else if (fileExtension == ".xlsx" || fileExtension == ".xls")
                        {
                            records = ReadExcelFile(filePath); // Read Excel
                        }
                        else
                        {
                            _logger.LogWarning("Unsupported file type: {FileExtension}", fileExtension);
                            continue;
                        }

                        DataTable bulkData = new DataTable();
                        bulkData.Columns.Add("list_id", typeof(int));
                        bulkData.Columns.Add("contact_id", typeof(int));
                        bulkData.Columns.Add("eventTimeStamp", typeof(string));
                        bulkData.Columns.Add("msisdn", typeof(string));
                        bulkData.Columns.Add("mcc", typeof(int));
                        bulkData.Columns.Add("mnc", typeof(int));
                        bulkData.Columns.Add("countryName", typeof(string));
                        bulkData.Columns.Add("created_date", typeof(DateTime));
                        bulkData.Columns.Add("createdby", typeof(string));
                        bulkData.Columns.Add("status", typeof(string));
                        bulkData.Columns.Add("workspace_id", typeof(int));
                        bulkData.Columns.Add("updated_date", typeof(DateTime));
                        bulkData.Columns.Add("fileName", typeof(string)); // New column for fileName

                        int listIdCounter = Convert.ToInt32(_dbHandler.ExecuteScalar("SELECT ISNULL(MAX(list_id), 0) + 1 FROM ta_operator_contacts"));
                        int contactIdCounter = 1;

                        foreach (var row in records)
                        {
                            // Validate row data before adding it to the bulkData table
                            if (!ValidateRow(row, out string errorMessage))
                            {
                                _logger.LogInformation("Validation failed for row with msisdn {Msisdn}: {ErrorMessage}",errorMessage);


                                // Create a temporary DataRow for failure insertion
                                DataRow failureRow = bulkData.NewRow();
                                failureRow["list_id"] = listIdCounter;
                                failureRow["contact_id"] = contactIdCounter++;
                                failureRow["eventTimeStamp"] = row.eventTimeStamp;
                                failureRow["msisdn"] = row.msisdn;
                                failureRow["mcc"] = row.mcc;
                                failureRow["mnc"] = row.mnc;
                                failureRow["countryName"] = row.countryName;
                                failureRow["created_date"] = DateTime.Now;
                                failureRow["createdby"] = "S3";
                                failureRow["status"] = "Updated";
                                failureRow["workspace_id"] = workspaceId;
                                failureRow["updated_date"] = DateTime.Now;
                                failureRow["fileName"] = fileNameWithDate;

                                // Insert the failed row into the failure table
                                await InsertFailureAsync(failureRow, errorMessage, workspaceId);
                                continue; // Skip adding invalid row to bulkData
                            }

                            // If the row is valid, add it to bulkData
                            DataRow newRow = bulkData.NewRow();
                            newRow["list_id"] = listIdCounter;
                            newRow["contact_id"] = contactIdCounter++;
                            newRow["eventTimeStamp"] = row.eventTimeStamp;
                            newRow["msisdn"] = row.msisdn;
                            newRow["mcc"] = row.mcc;
                            newRow["mnc"] = row.mnc;
                            newRow["countryName"] = row.countryName;
                            newRow["created_date"] = DateTime.Now;
                            newRow["createdby"] = "S3";
                            newRow["status"] = "Updated";
                            newRow["workspace_id"] = workspaceId;
                            newRow["updated_date"] = DateTime.Now;
                            newRow["fileName"] = fileNameWithDate;
                            bulkData.Rows.Add(newRow);
                        }

                        // Bulk copy the valid rows into the ta_operator_contacts table
                        using (SqlConnection cnn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                        {
                            cnn.Open();
                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(cnn))
                            {
                                bulkCopy.DestinationTableName = "ta_operator_contacts";
                                bulkCopy.ColumnMappings.Add("list_id", "list_id");
                                bulkCopy.ColumnMappings.Add("contact_id", "contact_id");
                                bulkCopy.ColumnMappings.Add("eventTimeStamp", "eventTimeStamp");
                                bulkCopy.ColumnMappings.Add("msisdn", "msisdn");
                                bulkCopy.ColumnMappings.Add("mcc", "mcc");
                                bulkCopy.ColumnMappings.Add("mnc", "mnc");
                                bulkCopy.ColumnMappings.Add("countryName", "countryName");
                                bulkCopy.ColumnMappings.Add("created_date", "created_date");
                                bulkCopy.ColumnMappings.Add("createdby", "createdby");
                                bulkCopy.ColumnMappings.Add("status", "status");
                                bulkCopy.ColumnMappings.Add("workspace_id", "workspace_id");
                                bulkCopy.ColumnMappings.Add("updated_date", "updated_date");
                                bulkCopy.ColumnMappings.Add("fileName", "fileName");
                                bulkCopy.WriteToServer(bulkData);
                            }
                        }


                        _logger.LogInformation("File data inserted successfully into the database.");
                    }
                }
                else
                {
                    _logger.LogInformation("No files found in the S3 bucket.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during file download and backup.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("File download and backup service is stopping.");
    }

}

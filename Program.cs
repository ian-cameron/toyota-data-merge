
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
class Program
{
    static string repoPath = @"C:\source\flatdata-vehicle-inventory"; // Set your repo path here
    // Columns:
    // name,state,vin,year,vehicle,model,engine,transmission,drivetrain,cab,bed,color,interior,base_msrp,total_msrp,availability_date,total_packages,packages,dealerId,url,regionId,lat,long
    static string existingData = @"combined-allocation-sheet.csv";
    static string inventoryCsvFilePath = "data/toyota-inventory.csv"; // Path to the inventory CSV file
    static void Main()
    {
        Dictionary<string, (DateTime createdAt, int daysAvailable, List<string> data)> vinData = new Dictionary<string, (DateTime, int, List<string>)>();

        LoadExistingData(vinData);
        Console.WriteLine($"Loaded existing data from the old allocation spreadsheet. {vinData.Count} VINs");
        // Get the commit history for the files
        List<string> commitHashes = GetAllCommitHashes(repoPath, inventoryCsvFilePath);
        commitHashes.Reverse();
        foreach (string commitHash in commitHashes)
        {
            Console.Write($"Processing: {commitHash} ({GetCommitDateByHash(commitHash, repoPath)})");
            string inventoryContent = GetFileContentAtCommit(repoPath, inventoryCsvFilePath, commitHash);
            ProcessCsvContent(inventoryContent, vinData, commitHash);
            Console.Write($": {vinData.Count} VINs collected so far.\n");
        }

        // Create combined inventory with days_available
        CreateCombinedInventory(repoPath, vinData);
    }

    static void LoadExistingData(Dictionary<string, (DateTime createdAt, int daysAvailable, List<string> data)> vinData) {
        using (var csv = new CsvReader(new StreamReader(existingData), new CsvConfiguration() { HasHeaderRecord = true }))
        {
            var records = csv.GetRecords<dynamic>();
            foreach (var record in records)
            {
                var values = (IDictionary<string, object>)record;
                string vin = values["vin"].ToString(); // Get VIN by header name

                DateTime createdAt;

                if (!vinData.ContainsKey(vin)) // If VIN is not in the data structure
                {
                    createdAt = DateTime.Parse(values["created_at"].ToString().Trim(), CultureInfo.InvariantCulture);
                    List<string> row = new List<string>() 
                    {
                        values["name"].ToString(),
                        values["state"].ToString(),
                        values["vin"].ToString(),
                        values["year"].ToString(),
                        values["vehicle"].ToString(),
                        values["model"].ToString(),
                        "i-FORCE 2.4L Turbocharged Engine".ToString(),
                        values["transmission"].ToString(),
                        values["drivetrain"].ToString(),
                        values["cab"].ToString(),
                        values["bed"].ToString(),
                        values["color"].ToString(),
                        values["interior"].ToString(),
                        values["base_msrp"].ToString(),
                        values["total_msrp"].ToString(),
                        "".ToString(),
                        $"{values["packages"].ToString().Count(c => c == ',') +1}".ToString(),
                        values["packages"].ToString(),
                        00000.ToString(),
                        values["url"].ToString(),
                        "0".ToString(),
                        "".ToString(),
                        ""
                    };
                    var days = (DateTime.Parse(values["Last Updated"].ToString()) - createdAt).Days;
                    vinData[vin] = (createdAt, days, new List<string>(row));
                }
                else // If VIN is already in the data structure
                {
                    Console.WriteLine($"Duplicate VIN {vin}");
                }
            }
        }
    }
    static List<string> GetAllCommitHashes(string repoPath, string csvFilePath)
    {
        List<string> commitHashes = new List<string>();
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"log --pretty=format:%H {csvFilePath}",
            RedirectStandardOutput = true,
            WorkingDirectory = repoPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    commitHashes.Add(line.Trim());
                }
            }
        }

        return commitHashes;
    }

    static string GetFileContentAtCommit(string repoPath, string filePath, string commitHash)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"show {commitHash}:{filePath}",
            RedirectStandardOutput = true,
            WorkingDirectory = repoPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                return reader.ReadToEnd();
            }
        }
    }

    static void ProcessCsvContent(string csvContent, Dictionary<string, (DateTime createdAt, int daysAvailable, List<string> data)> vinData, string commitHash)
    {
        using (var reader = new StringReader(csvContent))
        using (var csv = new CsvReader(reader, new CsvConfiguration() { HasHeaderRecord = true }))
        {
            var records = csv.GetRecords<dynamic>();
            foreach (var record in records)
            {
                var values = (IDictionary<string, object>)record;
                string vin = values["vin"].ToString(); // Get VIN by header name

                // Check if the vehicle is "tacoma"
                if (values["vehicle"].ToString() != "tacoma")
                {
                    continue; // Skip rows that are not Tacoma
                }

                DateTime createdAt;

                if (!vinData.ContainsKey(vin)) // If VIN is not in the data structure
                {
                    if (values.ContainsKey("created_at")) // If created_at exists
                    {
                        createdAt = DateTime.Parse(values["created_at"].ToString().Trim(), CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        createdAt = GetCommitDateByHash(commitHash, repoPath); // Use commit date if created_at is missing
                    }

                    vinData[vin] = (createdAt, 0, new List<string>(values.Values.Select(v => v.ToString()))); // Initialize daysAvailable to 0
                }
                else // If VIN is already in the data structure
                {
                    var (existingCreatedAt, existingDaysAvailable, existingData) = vinData[vin];
                    vinData[vin] = (existingCreatedAt, existingDaysAvailable + 1, existingData); // Increment daysAvailable
                }
            }
        }
    }
    static Dictionary<string, DateTime> commitDates = [];
    static DateTime GetCommitDateByHash(string commitHash, string repoPath)
    {
        // see if its already cached
        if (commitDates.ContainsKey(commitHash))
            return commitDates[commitHash];
        
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"show -s --format=%ci {commitHash}",
            RedirectStandardOutput = true,
            WorkingDirectory = repoPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string dateStr = reader.ReadLine();
                if (DateTime.TryParse(dateStr, out DateTime commitDate))
                {
                    commitDates.Add(commitHash, commitDate);
                    return commitDate;
                }
                else
                {
                    throw new Exception($"Failed to parse commit date for hash {commitHash}");
                }
            }
        }
    }

   static void CreateCombinedInventory(string repoPath, Dictionary<string, (DateTime createdAt, int daysAvailable, List<string> data)> vinData)
{
    List<string> outputLines = new List<string>();
    
    // Define the expected headers for both inventory and dealer data
    List<string> headers = new List<string>
    {
        "created_at",
        "days_available",
        // Add dealer CSV headers here
        "name", "state", "vin", "year", "vehicle", "model", "engine", 
        "transmission", "drivetrain", "cab", "bed", "color", 
        "interior", "base_msrp", "total_msrp", "availability_date", 
        "total_packages", "packages", "dealerId", "url", "regionId", 
        "lat", "long"
    };

    outputLines.Add(string.Join(",", headers)); // Use defined headers for the output

    foreach (var kvp in vinData)
    {
        var (createdAt, daysAvailable, data) = kvp.Value;

        // Prepare the output line with all original data plus days_available
        outputLines.Add($"{createdAt.ToString("d")},{daysAvailable}," + string.Join(",", data.Select(d => QuoteIfNecessary(d))));
        
    }

    // Write combined inventory to CSV
    File.WriteAllLines(Path.Combine(repoPath, "combined-inventory.csv"), outputLines);
}
// Helper method to quote fields if they contain commas
static string QuoteIfNecessary(string value)
{
    if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
    {
        // Escape double quotes by replacing " with ""
        value = value.Replace("\"", "\"\"");
        return $"\"{value}\""; // Wrap in quotes
    }
    return value; // Return as is if no special characters
}

}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
class Program
{
    static DateTime startingFrom = new DateTime(2024,1,23); // Set the date of earliest commit to parse
    static string repoPath = @"C:\source\flatdata-vehicle-inventory"; // Set your repo path here
    static string inventoryFilePath = "inventory.json"; // Path to the inventory json file
    static void Main()
    {
        Dictionary<string, (int daysAvailable, List<string> data)> vinData = new Dictionary<string, (int, List<string>)>();

        // Get the commit history for the files
        List<string> commitHashes = GetAllCommitHashes(repoPath, inventoryFilePath, startingFrom);
        commitHashes.Reverse();
        foreach (string commitHash in commitHashes)
        {
            if (GetCommitDateByHash(commitHash, repoPath) < startingFrom)
                continue;
            Console.Write($"Processing: {commitHash} ({GetCommitDateByHash(commitHash, repoPath)})");
            string inventoryContent = GetFileContentAtCommit(repoPath, inventoryFilePath, commitHash);
            ProcessCsvContent(inventoryContent, vinData, commitHash);
            Console.Write($": {vinData.Count} VINs collected so far.\n");
        }

        // Create combined inventory with days_available
        CreateCombinedInventory(repoPath, vinData);
    }

    static List<string> GetAllCommitHashes(string repoPath, string filePath, DateTime sinceDate)
    {
        List<string> commitHashes = new List<string>();
        // Format the date for the git command
        string sinceArgument = $"--since=\"{sinceDate:yyyy-MM-dd}\"";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"log {sinceArgument} --pretty=format:%H {filePath}",
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

     static void ProcessCsvContent(string jsonContent, Dictionary<string, (int daysAvailable, List<string> data)> vinData, string commitHash)
    {
        using (var reader = new StringReader(jsonContent))
        {
            
            var json = reader.ReadToEnd();
            var records = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
           
            foreach (var record in records)
            {
                // Check if the vehicle is "tacoma" and the right year
                if (record["vehicle"].ToString() != "tacoma" || record["year"].ToString() != "2024")
                {
                    continue; // Skip rows that are not 2024 Tacomas
                }
                string vin = record["vin"].ToString(); // Get VIN by header name

                DateTime createdAt;
                if (!vinData.ContainsKey(vin)) // If VIN is not in the data structure
                {
                    if (record.ContainsKey("created_at")) // If created_at exists
                    {
                        createdAt = DateTime.Parse(record["created_at"].ToString().Trim(), CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        Console.WriteLine("Missing created at");
                        createdAt = GetCommitDateByHash(commitHash, repoPath); // Use commit date if created_at is missing
                    }

                    vinData[vin] = ( 0, new List<string>(record.Values.Select(v => v.ToString()))); // Initialize daysAvailable to 0
                }
                else // If VIN is already in the data structure
                {
                    var (existingDaysAvailable, existingData) = vinData[vin];
                    vinData[vin] = (existingDaysAvailable + 1, existingData); // Increment daysAvailable
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

   static void CreateCombinedInventory(string repoPath, Dictionary<string, (int daysAvailable, List<string> data)> vinData)
{
    List<string> outputLines = new List<string>();
    
    // Define the expected headers for both inventory and dealer data
    List<string> headers = new List<string>
    {
        // Add CSV headers here
        "dealer", "vin", "year", "vehicle", "model", "engine", 
        "transmission", "drivetrain", "cab", "bed", "color", 
        "interior", "base_msrp", "total_msrp", "availability_date", 
        "total_packages", "packages", "created_at", "days_available"
    };

    outputLines.Add(string.Join(",", headers)); // Use defined headers for the output

    foreach (var kvp in vinData)
    {
        var (daysAvailable, data) = kvp.Value;

        // Prepare the output line with all original data plus days_available
        outputLines.Add($"{string.Join(",", data.Select(d => QuoteIfNecessary(d)))},{daysAvailable}");
        
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